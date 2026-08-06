
using SporticoApp.Api.Middlewares;
using SporticoApp.Application;
using SporticoApp.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;

using SporticoApp.Shared.Configuration;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Enums;
using SporticoApp.Shared.Responses;
using Microsoft.OpenApi.Models;
namespace SporticoApp.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LoadEnvIfPresent();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase;

                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            });
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var details = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();

                    var response = new Result<object>
                    {
                        IsSuccess = false,
                        Error = new Error
                        {
                            Code = ErrorCodes.ValidationError,
                            Message = "Invalid request data",
                            Type = ErrorType.Validation,
                            Details = details
                        }
                    };

                    return new BadRequestObjectResult(response);
                };
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Sportico API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token only"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

            builder.Services.AddApplicationDI();
            builder.Services.AddInfrastructureDI(builder.Configuration);

            // Background reconciliation of PayOS payout status for processing withdrawals.
            builder.Services.Configure<SporticoApp.Application.Options.WithdrawalPayoutReconciliationOptions>(
                builder.Configuration.GetSection(
                    SporticoApp.Application.Options.WithdrawalPayoutReconciliationOptions.SectionName));
            builder.Services.AddHostedService<SporticoApp.Api.BackgroundServices.WithdrawalPayoutReconciliationService>();

            // Consumes visitor-tracking work items off the in-process queue (IVisitorTrackingQueue)
            // so tracking writes happen off the HTTP request thread — see VisitorTrackingMiddleware.
            builder.Services.AddHostedService<SporticoApp.Api.BackgroundServices.VisitorTrackingBackgroundService>();

            // Community post lifecycle: flips published/closed posts to "expired" once their
            // activity time has passed.
            builder.Services.AddHostedService<SporticoApp.Api.BackgroundServices.CommunityPostExpiryBackgroundService>();

            // Safety net for PayOS payments/voucher reservations abandoned before the webhook or a
            // learner-triggered reconcile ever resolved them.
            builder.Services.AddHostedService<SporticoApp.Api.BackgroundServices.PaymentAndVoucherExpirySweepBackgroundService>();

            var jwtSecretKey = builder.Configuration["JWT:SecretKey"];
            var jwtIssuer = builder.Configuration["JWT:Issuer"];
            var jwtAudience = builder.Configuration["JWT:Audience"];

            if (string.IsNullOrWhiteSpace(jwtSecretKey) ||
                string.IsNullOrWhiteSpace(jwtIssuer) ||
                string.IsNullOrWhiteSpace(jwtAudience))
            {
                throw new InvalidOperationException(
                    "JWT configuration is missing required values.");
            }

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,

                        ValidateAudience = true,
                        ValidAudience = jwtAudience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSecretKey)),

                        ValidateLifetime = true,

                        ClockSkew = TimeSpan.Zero
                    };
                });

            // ── Google sign-in ──────────────────────────────────────────────────
            // Bound from GoogleAuth:* with the existing flat GOOGLE_*/FRONTEND_URL env vars as
            // fallback. Values are read via IConfiguration only and never logged.
            builder.Services.AddSingleton<
                Microsoft.Extensions.Options.IConfigureOptions<SporticoApp.Application.Options.GoogleAuthOptions>,
                SporticoApp.Api.Configuration.GoogleAuthOptionsSetup>();

            // Resolve once at startup so the Google handler gets the configured callback path.
            var googleAuthOptions = new SporticoApp.Application.Options.GoogleAuthOptions();
            new SporticoApp.Api.Configuration.GoogleAuthOptionsSetup(builder.Configuration)
                .Configure(googleAuthOptions);

            var googleCallbackPath =
                SporticoApp.Api.Configuration.GoogleCallbackUrlResolver.ResolveCallbackPath(googleAuthOptions);

            // Short-lived cookie that carries the external principal for the single hop between
            // Google's callback and /api/auth/google/complete. It is NOT a session cookie: JWT
            // Bearer remains the only thing that authorizes an API call.
            builder.Services
                .AddAuthentication()
                .AddCookie(SporticoApp.Api.Controllers.AuthenticationSchemeNames.ExternalCookie, options =>
                {
                    options.Cookie.Name = "sportico_external";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SameSite = SameSiteMode.Lax; // must survive Google's cross-site redirect back
                    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                    options.SlidingExpiration = false;
                });

            // Registered unconditionally so the app always starts; when credentials are absent the
            // Google endpoints answer 503 (see GoogleAuthController.EnsureRedirectFlowConfigured)
            // while every other endpoint keeps working.
            //
            // Registered ONLY when credentials exist: GoogleHandler validates ClientId/ClientSecret
            // when the scheme is initialised, which happens inside UseAuthentication() on EVERY
            // request. Registering it with blank credentials would therefore throw
            // ArgumentException("The 'ClientId' option must be provided") on every call to every
            // endpoint — taking the whole API down instead of just disabling Google sign-in.
            if (googleAuthOptions.IsRedirectFlowConfigured)
            {
            builder.Services
                .AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = googleAuthOptions.ClientId!;
                    options.ClientSecret = googleAuthOptions.ClientSecret!;

                    // Google redirects here; must match the registered redirect URI exactly.
                    options.CallbackPath = googleCallbackPath;

                    // Park the external identity in the temporary cookie, not in a real session.
                    options.SignInScheme = SporticoApp.Api.Controllers.AuthenticationSchemeNames.ExternalCookie;

                    // Sportico never calls Google APIs on the user's behalf, so no offline access
                    // and no Google refresh token is requested or stored.
                    options.SaveTokens = false;
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("email");
                    options.Scope.Add("profile");

                    // GoogleOptions maps sub/name/email by default but not picture or
                    // email_verified. Read them straight off the userinfo payload — an explicit
                    // event avoids depending on ClaimAction overload availability.
                    options.Events.OnCreatingTicket = context =>
                    {
                        if (context.Identity == null)
                        {
                            return Task.CompletedTask;
                        }

                        if (context.User.TryGetProperty("picture", out var picture) &&
                            picture.ValueKind == JsonValueKind.String)
                        {
                            var url = picture.GetString();
                            if (!string.IsNullOrWhiteSpace(url))
                            {
                                context.Identity.AddClaim(new System.Security.Claims.Claim("urn:google:picture", url));
                            }
                        }

                        if (context.User.TryGetProperty("email_verified", out var verified) &&
                            (verified.ValueKind == JsonValueKind.True || verified.ValueKind == JsonValueKind.False))
                        {
                            context.Identity.AddClaim(new System.Security.Claims.Claim(
                                "email_verified",
                                verified.GetBoolean() ? "true" : "false"));
                        }

                        return Task.CompletedTask;
                    };

                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                    options.CorrelationCookie.SecurePolicy = builder.Environment.IsDevelopment()
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;

                    // A rejected consent must not surface a raw exception page — send the user back
                    // to the frontend with a stable error code instead.
                    options.Events.OnRemoteFailure = context =>
                    {
                        context.Response.Redirect(
                            SporticoApp.Api.Configuration.GoogleCallbackUrlResolver
                                .BuildFrontendCallbackUrl(googleAuthOptions, null, ErrorCodes.GoogleLoginFailed));
                        context.HandleResponse();
                        return Task.CompletedTask;
                    };
                });
            }

            builder.Services.AddAuthorization();

            // Azure App Service / reverse proxy terminates TLS. Without this the Google redirect_uri
            // would be built with the internal scheme+host (http://<instance>) instead of the public
            // https://sportico.click, and Google would reject it as an unregistered redirect URI.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto |
                    ForwardedHeaders.XForwardedHost;

                // Azure's front end is not in a known-network range we can enumerate; clearing these
                // is the documented App Service configuration. Safe here because the platform
                // strips client-supplied X-Forwarded-* headers before they reach the app.
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var app = builder.Build();

            // Must run before UseHttpsRedirection/UseAuthentication so scheme+host are already
            // corrected when the Google challenge builds its absolute redirect_uri.
            app.UseForwardedHeaders();

            // Configure the HTTP request pipeline.
            // Đưa ra ngoài để Production (Azure) vẫn chạy được
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sportico API V1");
                c.RoutePrefix = string.Empty; 
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sportico API V1");
                    c.RoutePrefix = string.Empty; 
                });
            }
            app.UseHttpsRedirection();

            app.UseMiddleware<ExceptionMiddleware>();

            app.UseAuthentication();

            // After authentication (so HttpContext.User is populated for logged-in visitors) and
            // before authorization enforcement (so it runs for every request, including ones that
            // later get a 401/403). Never blocks or fails the real request — see the middleware.
            app.UseMiddleware<SporticoApp.Api.Middlewares.VisitorTrackingMiddleware>();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        /// <summary>
        /// Loads the single recognised environment file, <c>&lt;repository-root&gt;/.env</c>, via the
        /// shared <see cref="EnvironmentFileLoader"/> that the EF design-time factory also uses — so
        /// <c>dotnet run</c> and <c>dotnet ef</c> can never target different databases.
        /// <para>
        /// Only non-sensitive facts are written to the console: which file was loaded, and whether a
        /// stray legacy <c>.env</c> was ignored. No key name and no value is ever printed.
        /// </para>
        /// </summary>
        private static void LoadEnvIfPresent()
        {
            var result = EnvironmentFileLoader.Load();

            if (result.Loaded)
            {
                Console.WriteLine("[config] Environment file loaded from repository root.");
            }
            else if (result.RepositoryRoot == null)
            {
                Console.WriteLine(
                    "[config] No repository root found; relying on process environment variables.");
            }
            else
            {
                Console.WriteLine(
                    "[config] No .env at repository root; relying on process environment variables.");
            }

            foreach (var _ in result.IgnoredPaths)
            {
                Console.WriteLine(
                    "[config] WARNING: a legacy .env exists under src/SporticoApp.Api and was IGNORED. " +
                    "The repository-root .env is the only supported location.");
            }
        }
    }
}
