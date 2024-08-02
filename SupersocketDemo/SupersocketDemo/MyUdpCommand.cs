using SuperSocket.Command;
using SuperSocket.ProtoBase;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions.Session;
using System.Text;

namespace SupersocketDemo
{
    public class MyUdpCommand : IAsyncCommand<TextPackageInfo>
    {
        public ValueTask ExecuteAsync(IAppSession session, TextPackageInfo package, CancellationToken cancellationToken)
        {
            // 处理接收到的消息
            Console.WriteLine($"Received: {package.Text}");
            return session.SendAsync(Encoding.UTF8.GetBytes("Message received\n"));
        }
    }
    public class ADD : IAsyncCommand<StringPackageInfo>
    {
        public async ValueTask ExecuteAsync(IAppSession session, StringPackageInfo package, CancellationToken cancellationToken)
        {
            var result = package.Parameters
                .Select(p => int.Parse(p))
                .Sum();

            await session.SendAsync(Encoding.UTF8.GetBytes(result.ToString() + "\r\n"));
        }
    }
}
