using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Peer2Peer_storage
{
    class Node
    {
        private TcpListener nodeServer;
        private Clients nodeClients = new Clients();
        private Peers nodesInNetwork;
        private PeerFile files;
        private bool nodeServerRunning = true;
        private int portOfNode;

        public Node()
        {
            NodeInfo.LoadSetting();       
            nodesInNetwork = new Peers();
            files = new PeerFile(nodesInNetwork);
            this.portOfNode = NodeInfo.portOfNode;

            Console.WriteLine("Peer to peer cloud");
            Console.WriteLine("By Lukáš Štěp");
            Console.WriteLine("Port:" + NodeInfo.portOfNode);
            Console.WriteLine("Max number of shards:" + NodeInfo.maxNumberOfShards);
            Console.WriteLine("Peers count:" + nodesInNetwork.Nodes.Count);
            Console.WriteLine("Initial join all:");
            var appSettings = ConfigurationManager.AppSettings;
            string bootNode = appSettings["bootNode"] ?? "Not Found";
            Join(bootNode);
            JoinAll();
            Console.WriteLine();

            nodeServer = new TcpListener(IPAddress.Any, NodeInfo.portOfNode);
            nodeServer.Start();
            Thread th = new Thread(AcceptsClients);
            th.Start();

            bool running = true;
            while (running)
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("p2p>");
                Console.ForegroundColor = ConsoleColor.White;
                string input = Console.ReadLine();                
                switch (input)
                {
                    case "join":
                        string address = NodeInfo.CheckInput("Ip address + port (192.168.0.1:5222):", @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}):(\d{1,5})");
                        Join(address); 
                        break;
                    case "joinAll":
                        JoinAll();
                        break;
                    case "ping":
                        address = NodeInfo.CheckInput("Ip address + port (192.168.0.1:5222):", @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}):(\d{1,5})");
                        Console.WriteLine("Ping result to "+ address+": "+ Ping(address));
                        break;
                    case "upload":
                        string pathU = NodeInfo.CheckInput("File for upload absolute or relative path:", ".");
                        files.UploadFile(pathU);
                        break;
                    case "download":
                        NodeInfo.viewSavedManifests();
                        pathU = NodeInfo.CheckInput("Manifest name:", ".");
                        files.DownloadFile(pathU + ".json");
                        break;
                    case "delete":
                        NodeInfo.viewSavedManifests();
                        pathU = NodeInfo.CheckInput("Manifest name:", ".");
                        files.DeleteFile(pathU + ".json");
                        break;
                    case "manifests":
                        NodeInfo.viewSavedManifests();
                        break;
                    case "settings":
                        NodeInfo.Settings();
                        break;
                    case "help":
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("join - add node with ip address\n" +
                            "joinAll - fetch nodes from all saved noded\n" +
                            "upload - upload file to other nodes\n" +
                            "download - download file with manifest\n" +
                            "delete - delete shards from all nodes\n" +
                            "ping - pings specific node\n" +
                            "peersTable - view whole table of saved nodes\n" +
                            "manifests - view all saved manifests\n" +
                            "setting - settings menu\n" +
                            "help - help\n" +
                            "exit - stop app");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case "peersTable":
                        nodesInNetwork.ViewPeerTable();
                        break;
                    case "exit":
                        running = false;
                        nodeServerRunning = false;
                        Console.WriteLine("App stoped");
                        nodeServer.Stop();
                        break;
                    case "":
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Not exists, type \"help\" to commands");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }
            }
        }
        private void AcceptsClients()
        {
            while (nodeServerRunning)
            {
                try
                {
                    TcpClient client = nodeServer.AcceptTcpClient();
                    Thread th = new Thread(new ParameterizedThreadStart(HandleClient));
                    nodeClients.NodeClients.Add(client);
                    th.Start(client);
                }catch(Exception t)
                {

                }
                
            }
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient) obj;
            StreamWriter writerClient = new StreamWriter(client.GetStream(), Encoding.ASCII);
            StreamReader readerClient = new StreamReader(client.GetStream(), Encoding.ASCII);

            while (client.Connected)
            {
                string clientInput = String.Empty;
                try
                {
                    clientInput = readerClient.ReadLine();
                }catch(Exception t)
                {
                    Console.WriteLine("Network error or input error");
                }

                switch (clientInput)
                {
                    case string input when input.Contains("p2p;join;"):
                        HandleJoin(client, writerClient, readerClient, clientInput);
                        break;
                    case string input when input.Contains("p2p;ping;"):
                        HandlePing(writerClient);
                        break;
                    case string input when input.Contains("p2p;upload;"):
                        var parts = input.Split(";");                      
                        files.ReceiveFromClient(client, parts[3], Int32.Parse(parts[2]));
                        break;
                    case string input when input.Contains("p2p;requestShard;"):
                        parts = input.Split(";");
                        files.FileRequest(writerClient, parts[2], client);
                        break;
                    case string input when input.Contains("p2p;delete;"):
                        parts = input.Split(";");
                        files.DeleteShard(writerClient, parts[2]);
                        break;
                    default:
                        writerClient.WriteLine("unknown"); writerClient.Flush();
                        break;
                }
                client.Close();
            }
            nodeClients.NodeClients.Remove(client);              
        }

        private bool SendToClient(StreamWriter writerClient,string text)
        {
            try
            {
                writerClient.WriteLine(text);
                writerClient.Flush();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("SendedToClient:" + text);
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch(Exception t)
            {
                Console.WriteLine("Network error");
                return false;
            }
            return true;           
        }

        private string ReadFromClient(StreamReader readerClient)
        {
            string response = String.Empty;
            try
            {
                response = readerClient.ReadLine();
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Response:" + response);
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch(Exception t)
            {
                Console.WriteLine("Network error");
            }
            return response;
        }

        private void HandleJoin(TcpClient client, StreamWriter writerClient, StreamReader readerClient, string clientInput)
        {
            string clientPort = clientInput.Split(";")[2];
            string address = IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()) + ":" + clientPort;
            if (SendToClient(writerClient, nodesInNetwork.GetPeerTable()) == false)
            {
                Console.WriteLine("Network error");
                return;
            }
            nodesInNetwork.AddPeer(address);

        }      
        
        private void HandlePing(StreamWriter writerClient)
        {
            SendToClient(writerClient, "p2p;alive;");
        }

        private void Join(string address)
        {
            ClientConnection client = new ClientConnection(address);
            if (client.Client.Client.Connected)
            {
                if (SendToClient(client.WriterClient, "p2p;join;" + portOfNode) != true)
                    return;
                string response = String.Empty;
                try
                {
                    response = ReadFromClient(client.ReaderClient);
                }
                catch(Exception t)
                {
                    Console.WriteLine("Network error");
                }
                
                if (response.Contains("p2p;"))
                {
                    nodesInNetwork.AddPeer(address);
                    nodesInNetwork.HandlePeerTable(response);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.WriteLine("unknown join attemp");
                }
            }
            
        }
        private void JoinAll()
        {
            foreach(var peer in nodesInNetwork.Nodes.ToArray())
            {
                Join(peer);
            }
        }
        private bool Ping(string address)
        {
            ClientConnection client = new ClientConnection(address);
            if (client.Client.Connected)
            {

                if (SendToClient(client.WriterClient, "p2p;ping;") == false)
                    return false;
                if (ReadFromClient(client.ReaderClient) == "p2p;alive;")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
