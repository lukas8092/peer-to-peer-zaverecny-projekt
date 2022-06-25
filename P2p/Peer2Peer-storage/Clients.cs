using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Peer2Peer_storage
{
    class Clients
    {
        private List<TcpClient> nodeClients = new List<TcpClient>();
        public List<TcpClient> NodeClients { get => nodeClients; set => nodeClients = value; }
    }
}
