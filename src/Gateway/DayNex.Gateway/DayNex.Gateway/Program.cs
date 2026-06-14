using Serilog;

namespace DayNex.Gateway
{
    public class Program
    {

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
            Log.Information("DayNex Gateway starting up...");
            try
            {

                var builder = WebApplication.CreateBuilder(args);
                var app = builder.Build();

                app.MapGet("/", () => "Hello World!");

                app.Run();
            }
            catch (Exception ex)
            {
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }
    }
}
