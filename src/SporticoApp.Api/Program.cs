
using DotNetEnv;
using SporticoApp.Application;
using SporticoApp.Infrastructure;
using System.IO;

namespace SporticoApp.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LoadEnvIfPresent();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddApplicationDI();
            builder.Services.AddInfrastructureDI(builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

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
