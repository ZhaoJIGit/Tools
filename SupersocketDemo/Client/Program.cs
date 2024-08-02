using Common.Filters;
using SuperSocket.Client;
using SuperSocket.ProtoBase;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                string message;
                var client2 = new EasyClient<Common.Models.TextPackageInfo>(new SimplePipelineFilter()).AsClient();
                var connected = await client2.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 4040));

                while ((message = Console.ReadLine()) != null)
                {
                    // 发送消息到服务器
                    var data = Encoding.UTF8.GetBytes(message + "\r\n");
                    await client2.SendAsync(data);
                }

                using (var client = new TcpClient("localhost", 4040))
                using (var stream = client.GetStream())
                {
                    Console.WriteLine("Connected to chat server. Type your messages below (type 'exit' to quit):");

                    var receiveThread = new Thread(() => ReceiveMessages(stream));
                    receiveThread.Start();


                    while ((message = Console.ReadLine()) != null)
                    {
                        if (message.Trim().ToLower() == "exit")
                        {
                            var exitMessage = Encoding.UTF8.GetBytes("EXIT\r\n");
                            stream.Write(exitMessage, 0, exitMessage.Length);
                            break;
                        }

                        // 发送消息到服务器
                        var data = Encoding.UTF8.GetBytes(message + "\r\n");
                        stream.Write(data, 0, data.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        static void ReceiveMessages(NetworkStream stream)
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytes;
                while (true)
                {
                    bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes > 0)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, bytes);
                        Console.WriteLine(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive Exception: {ex.Message}");
            }
        }

    }
}