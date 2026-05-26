
using DotNetEnv;
using SporticoApp.Api.Middlewares;
using SporticoApp.Application;
using SporticoApp.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
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

            builder.Services.AddAuthorization();

            var app = builder.Build();

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

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        private static void LoadEnvIfPresent()
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());

            for (var i = 0; i < 5 && current != null; i++)
            {
                var envPath = Path.Combine(current.FullName, ".env");
                if (File.Exists(envPath))
                {
                    Env.Load(envPath);
                    break;
                }

                current = current.Parent;
            }
        }
    }
}
