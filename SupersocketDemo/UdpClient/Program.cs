using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace UdpClientApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var message = "";
            while ((message = Console.ReadLine()) != null)
            {
                //using (var client = new UdpClient("localhost", 4040))
                using (var udpClient = new UdpClient())
                {
                    string server = "localhost";
                    int port = 4040;

                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    await udpClient.SendAsync(messageBytes, messageBytes.Length, server, port);
                    Console.WriteLine($"Message sent: {message}");

                    var serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
                    var receivedResult = await udpClient.ReceiveAsync();
                    string receivedMessage = Encoding.UTF8.GetString(receivedResult.Buffer);
                    Console.WriteLine($"Received: {receivedMessage} from {receivedResult.RemoteEndPoint}");
                }
            }
           
        }
    }
}
