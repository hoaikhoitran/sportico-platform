using SporticoApp.Application.DTOs.Analytics;
using SporticoApp.Application.Validators.Analytics;
using Xunit;

namespace SporticoApp.Application.Tests.Analytics;

public class SubmitPageViewRequestValidatorTests
{
    [Fact]
    public void EmptyPath_Invalid()
    {
        var result = new SubmitPageViewRequestValidator().Validate(new SubmitPageViewRequest { Path = "" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidPath_Valid()
    {
        var result = new SubmitPageViewRequestValidator().Validate(new SubmitPageViewRequest
        {
            Path = "/coaches/123",
            Title = "Coach Profile",
            Referrer = "/search"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void PathTooLong_Invalid()
    {
        var result = new SubmitPageViewRequestValidator().Validate(new SubmitPageViewRequest
        {
            Path = "/" + new string('a', 600)
        });

        Assert.False(result.IsValid);
    }
}
