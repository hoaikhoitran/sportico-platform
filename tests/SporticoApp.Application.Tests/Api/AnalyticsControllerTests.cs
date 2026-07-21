using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SporticoApp.Api.Controllers;
using SporticoApp.Api.Middlewares;
using SporticoApp.Application.DTOs.Analytics;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Validators.Analytics;
using Xunit;

namespace SporticoApp.Application.Tests.Api;

/// <summary>Covers: frontend page-view ingestion — the actual PageView-producing controller action.</summary>
public class AnalyticsControllerTests
{
    private static AnalyticsController Build(IVisitorTrackingQueue queue, Guid? visitorId)
    {
        var controller = new AnalyticsController(queue, new SubmitPageViewRequestValidator(), NullLogger<AnalyticsController>.Instance);

        var httpContext = new DefaultHttpContext();
        if (visitorId.HasValue)
        {
            httpContext.Items[VisitorTrackingMiddleware.VisitorIdItemKey] = visitorId.Value;
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task ValidPageView_WithResolvedVisitor_EnqueuesPageViewWorkItem()
    {
        var queue = new FakeQueue();
        var visitorId = Guid.NewGuid();
        var controller = Build(queue, visitorId);

        var result = await controller.SubmitPageView(new SubmitPageViewRequest
        {
            Path = "/coaches/123",
            Title = "Coach Profile",
            Referrer = "/search"
        });

        Assert.IsType<AcceptedResult>(result);
        var item = Assert.Single(queue.Enqueued);
        Assert.Equal(VisitorTrackingWorkItemKind.PageView, item.Kind);
        Assert.Equal("/coaches/123", item.Path); // the REAL frontend route the client submitted, verbatim
        Assert.Equal("Coach Profile", item.Title);
        Assert.Equal(visitorId, item.Context.VisitorId);
    }

    [Fact]
    public async Task NoResolvedVisitor_StillAccepts_ButEnqueuesNothing()
    {
        var queue = new FakeQueue();
        var controller = Build(queue, visitorId: null); // analytics disabled / bot-classified request

        var result = await controller.SubmitPageView(new SubmitPageViewRequest { Path = "/x" });

        Assert.IsType<AcceptedResult>(result); // never fails the caller
        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public async Task QueueThrows_SubmissionStillAccepted()
    {
        var queue = new ThrowingQueue();
        var controller = Build(queue, Guid.NewGuid());

        var exception = await Record.ExceptionAsync(() => controller.SubmitPageView(new SubmitPageViewRequest { Path = "/x" }));

        Assert.Null(exception);
    }

    // ── fakes ────────────────────────────────────────────────────────────────
    private sealed class FakeQueue : IVisitorTrackingQueue
    {
        public readonly List<VisitorTrackingWorkItem> Enqueued = new();

        public bool TryEnqueue(VisitorTrackingWorkItem item)
        {
            Enqueued.Add(item);
            return true;
        }

        public IAsyncEnumerable<VisitorTrackingWorkItem> ReadAllAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private sealed class ThrowingQueue : IVisitorTrackingQueue
    {
        public bool TryEnqueue(VisitorTrackingWorkItem item) => throw new InvalidOperationException("simulated");

        public IAsyncEnumerable<VisitorTrackingWorkItem> ReadAllAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
