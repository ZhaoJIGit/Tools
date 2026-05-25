using DeployService.Services;
using PublishWeb.Services;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace PublishWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton<ChunkUploadService>();
            builder.Services.AddSingleton<DeployBackgroundService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<DeployBackgroundService>());
            builder.Services.AddHostedService<UploadCleanupService>();

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 500 * 1024 * 1024;
            });

            builder.Host.UseWindowsService();
            builder.WebHost.UseUrls("http://*:5555");

            var app = builder.Build();

            SiteConfigService.SetLogger(app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SiteConfigService"));

            app.UseAuthorization();
            app.MapControllers();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseStaticFiles();
            app.Run();
        }
    }
}
