using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Net.Sockets;
using System.Net;

namespace Peer2Peer_storage
{
    class Peers
    {
        private List<string> nodes = new List<string>();
        public List<string> Nodes { get => nodes; set => nodes = value; }
        public Peers()
        {
            if (File.Exists("peers.data"))
            {
                string dataFromFile = File.ReadAllText("peers.data");
                var peersData = dataFromFile.Split(";");
                foreach (var peer in peersData)
                {
                    if (peer != "")
                    {
                        lock (this)
                        {
                            nodes.Add(peer);
                        }
                    }
                }
            }
        }
        public void AddPeer(string address)
        {
            if(nodes.Contains(address) != true)
            {
                    lock (this)
                    {
                        if (NodeInfo.LocalIpList().Contains(address) == false)
                        {
                            nodes.Add(address);
                            File.AppendAllText("peers.data", address + ";");
                        }
                    }
           }
        }

        public bool CheckPeer(string address)
        {
            return nodes.Contains(address);
        }

        public string GetPeerTable()
        {
            string response = "p2p;";
            foreach(var peer in nodes)
            {
                if(peer != null || peer != "\n")
                response += peer + ";";
            }
            return response;
        }

        public void HandlePeerTable(string table)
        {
            string[] tableData = table.Split(";");
            List<string> localIps = NodeInfo.LocalIpList();
            foreach(var peer in tableData.Skip(1))
            {
                if(nodes.Contains(peer) != true && peer != "" && localIps.Contains(peer) == false)
                {
                    lock (this)
                    {
                        nodes.Add(peer);
                        File.AppendAllText("peers" + NodeInfo.portOfNode + ".data", peer + ";");
                    }
                }
            }           
        }
        public void ViewPeerTable()
        {
            Console.WriteLine("Peers count:" + nodes.Count);
            Console.WriteLine("Peers table:");
            foreach (var node in nodes)
            {
                Console.WriteLine(node);
            }
        }
    }
}
