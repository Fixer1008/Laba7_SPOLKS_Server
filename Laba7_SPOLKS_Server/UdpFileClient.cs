using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Laba7_SPOLKS_Server
{
    public class UdpFileClient : UdpClient
    {
        public UdpFileClient()
        {

        }

        public UdpFileClient(int port)
            : base (port)
        {

        }

        public bool ActiveRemoteHost
        {
            get { return Active; }
        }

        public Socket FileClientSocket 
        {
            get { return Client; }
        }
    }
}
