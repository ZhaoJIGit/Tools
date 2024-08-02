using SuperSocket.Udp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SupersocketDemo
{
    public class MySessionIdentifierProvider : IUdpSessionIdentifierProvider
    {
        public string GetSessionIdentifier(IPEndPoint remoteEndPoint, ArraySegment<byte> data)
        {
            return "test";
        }
    }
}
