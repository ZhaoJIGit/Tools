using Consul;
using Microsoft.OpenApi.Models;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;

namespace MiscoSoftware_ApiGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 指定额外的配置文件路径
            builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

            // 添加 Ocelot 配置
            builder.Services.AddOcelot(builder.Configuration).AddConsul();
            // 添加 Swagger 服务
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "My API",
                    Version = "v1",
                    Description = "API documentation for My Web API"
                });
            });
            builder.Services.AddControllers();
         
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            var app = builder.Build();
           

            // Configure the HTTP request pipeline.
            app.UseAuthorization();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
                c.RoutePrefix = string.Empty; // 使 Swagger UI 在根路径显示
            });
            app.UseOcelot();
            // 在 app.UseRouting() 之后启用 CORS
            app.UseCors("AllowAll");
            // 配置中间件
            app.UseRouting();
        
            app.MapControllers();
            app.Run();
        }

    }
}
