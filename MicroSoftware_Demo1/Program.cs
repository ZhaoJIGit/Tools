using Consul;
using Microsoft.OpenApi.Models;

namespace MicroSoftware_Demo1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // 获取 Consul 配置
            var consulConfig = builder.Configuration.GetSection("Consul");

            // 注册 Consul 客户端
            builder.Services.AddSingleton<IConsulClient>(sp => new ConsulClient(config =>
            {
                config.Address = new Uri(consulConfig["Address"] ?? "http://127.0.0.1:8500");
            }));

            // 注册服务与 Consul 的集成
            builder.Services.AddSingleton<IHostedService, ConsulServiceRegistration>();
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

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

          
            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseAuthorization();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
                c.RoutePrefix = string.Empty; // 使 Swagger UI 在根路径显示
            });
            app.MapGet("/health", () => 
            Results.Ok("Healthy")
            );
            // 配置中间件
            app.UseRouting();
            // 在 app.UseRouting() 之后启用 CORS
            app.UseCors("AllowAll");
            app.MapControllers();

            app.Run();
        }
    }

    // 定义一个 Consul 服务注册类
    public class ConsulServiceRegistration : IHostedService
    {
        private readonly IConsulClient _consulClient;
        private readonly IWebHostEnvironment _env;

        public ConsulServiceRegistration(IConsulClient consulClient, IWebHostEnvironment env)
        {
            _consulClient = consulClient;
            _env = env;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var registration = new AgentServiceRegistration()
            {
                ID = $"demo1",
                Name = "demo1",
                Address = "127.0.0.1",  // 服务实例的地址
                Port = 5001,            // 服务实例的端口
                Tags = new[] { "api" }, // 标签
                Check = new AgentServiceCheck
                {
                    HTTP = "http://127.0.0.1:5001/health", // 健康检查的 URL
                    Interval = TimeSpan.FromSeconds(3)    // 检查频率
                }
            };

            await _consulClient.Agent.ServiceRegister(registration);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var serviceId = $"demo1";
            await _consulClient.Agent.ServiceDeregister(serviceId);
        }
    }
   
}
