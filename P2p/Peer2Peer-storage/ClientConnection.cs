using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Peer2Peer_storage
{
    class ClientConnection
    {
        private TcpClient client;
        private StreamWriter writerClient;
        private StreamReader readerClient;
        public TcpClient Client { get => client; set => client = value; }
        public StreamWriter WriterClient { get => writerClient; set => writerClient = value; }
        public StreamReader ReaderClient { get => readerClient; set => readerClient = value; }

        public string Address { get; set; }

        public ClientConnection(string address)
        {
           Address = address;
            string[] addressData = address.Split(":");
            Client = new TcpClient();
            try
            {
                Client.Connect(addressData[0], Int32.Parse(addressData[1]));
                NetworkStream networkStream = Client.GetStream();

                WriterClient = new StreamWriter(networkStream);
                ReaderClient = new StreamReader(networkStream, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Client " + Address+ " not responding");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}
