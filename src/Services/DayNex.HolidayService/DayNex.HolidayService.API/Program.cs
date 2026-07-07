
using DayNex.HolidayService.Application;
using DayNex.HolidayService.Infrastructure;
using DayNex.Shared.Http;

namespace DayNex.HolidayService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            InterfaceDependency.AddApplication(builder.Services);
            InfrastructureDependencies.AddInfrastructure(builder.Services,builder.Configuration);
            HTTPDependencies.AddHttpService(builder.Services);
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
