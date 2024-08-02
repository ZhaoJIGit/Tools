using Common.Filters;
using Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuperSocket.Command;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.Server.Host;
using SuperSocket.Udp;
using System.Text;

namespace SupersocketDemo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //var host = SuperSocketHostBuilder.Create<TextPackageInfo, UdpPipelineFilter>()
            //.UseUdp()
            ////.UseCommand((commandOptions) =>
            ////{
            ////     // 一个一个的注册命令
            ////     commandOptions.AddCommand<ADD>();
            ////     // 注册程序集重的所有命令
            ////     //commandOptions.AddCommandAssembly(typeof(SUB).GetTypeInfo().Assembly);
            ////})

            // .UseSessionHandler(
            //    onConnected: async (s) =>
            //    {
            //        Console.WriteLine($"Session connected: {s.SessionID}");
            //        await Task.CompletedTask;
            //    },
            //    onClosed: async (s, args) =>
            //    {
            //        await Task.CompletedTask;
            //    })

            //.ConfigureSuperSocket(options =>
            //{
            //    options.Name = "SuperSocketServer";
            //    options.AddListener(new ListenOptions
            //    {
            //        Ip = "Any",
            //        Port = 4040
            //    });
            //})
            // .UsePackageHandler(async (session, package) =>
            // {
            //     // 处理接收到的UDP消息
            //     Console.WriteLine($"Received: {package.Text}");
            //     await session.SendAsync(Encoding.UTF8.GetBytes($"已收到:{package.Text}"));
            // })
            //.UsePipelineFilter<UdpPipelineFilter>()
            //.UseSessionHandler(async (session) =>
            //{
            //    Console.WriteLine("New session connected: " + session.SessionID);
            //    await Task.CompletedTask;
            //}, async (session, reason) =>
            //{
            //    Console.WriteLine("Session closed: " + session.SessionID);
            //    await Task.CompletedTask;
            //})
            //.UseInProcSessionContainer()
            ////.ConfigureServices((context, services) =>
            ////{
            ////    services.AddSingleton<IUdpSessionIdentifierProvider, MySessionIdentifierProvider>();
            ////})
            //.ConfigureLogging(loggingBuilder =>
            //{
            //    loggingBuilder.AddConsole();
            //})

            //.Build();

            //await host.RunAsync();



            var host = SuperSocketHostBuilder.Create<TextPackageInfo, SimplePipelineFilter>()
                .UseSessionHandler(
                onConnected: async (s) =>
                {
                    Console.WriteLine($"Session connected: {s.SessionID}");
                    await Task.CompletedTask;
                },
                onClosed: async (s, args) =>
                {
                    await Task.CompletedTask;
                })
               .UsePackageHandler(async (session, package) =>
               {
                   if (package.Key.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
                   {
                       await session.SendAsync(Encoding.UTF8.GetBytes("Goodbye!\r\n"));
                       await session.CloseAsync(SuperSocket.Connection.CloseReason.LocalClosing);
                   }
                   else
                   {
                       var response = $"User {session.SessionID} says: {package.Text}\r\n";
                       var sessions = session.Server.GetSessionContainer().GetSessions();
                       foreach (var s in sessions)
                       {
                           if (s.SessionID != session.SessionID)
                           {
                               await s.SendAsync(Encoding.UTF8.GetBytes(response));
                           }
                       }
                   }
                   await session.SendAsync(Encoding.UTF8.GetBytes("服务端已收到"));
               })
                .ConfigureSuperSocket(options =>
                {
                    options.Name = "Chat Server";
                    options.Listeners = new List<ListenOptions>
                    {
                        new ListenOptions
                        {
                            Ip = "Any",
                            Port = 4040,
                        }
                    };
                })
                .UseInProcSessionContainer()
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConsole();
                })
                .Build();
            await host.RunAsync();

        }
    }
}
