using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Peer2Peer_storage
{
    class NodeInfo
    {
        public static int portOfNode;
        public static int maxNumberOfShards;
        public static List<string> LocalIpList()
        {
            List<string> list = new List<string>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    list.Add(ip.ToString()+":"+ NodeInfo.portOfNode);
                }
            }
            return list;
        }
        public static void LoadSetting()
        {
            if (File.Exists("port.data") == false)
            {
                Random r = new Random();
                NodeInfo.portOfNode = r.Next(5000, 5999);
                NodeInfo.maxNumberOfShards = 8;
                File.WriteAllText("port.data", NodeInfo.portOfNode.ToString() + "\n" + 8);
            }
            else
            {
                string data = File.ReadAllText("port.data");
                var values = data.Split("\n");
                NodeInfo.portOfNode = Int32.Parse(values[0]);
                NodeInfo.maxNumberOfShards = Int32.Parse(values[1]);
            }
        }

        public static void Settings()
        {
            bool settingActive = true;
            while (settingActive)
            {

                Console.WriteLine("Settings:");
                Console.WriteLine("1. Port\n2. Max number of shards");
                Console.Write("Choise:");
                int input = 0;
                try
                {
                    input = Int32.Parse(Console.ReadLine());
                }
                catch (Exception t)
                {
                    continue;
                }
                if (input > 0 && input < 3)
                {
                    switch (input)
                    {
                        case 1:
                            int newPort = Int32.Parse(CheckInput("New port(app must be restarted):", @"^[0-9]{1,10}$"));                           
                            portOfNode = newPort;
                            File.WriteAllText("port.data", NodeInfo.portOfNode.ToString() + "\n" + NodeInfo.maxNumberOfShards);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Port changed, please restart app, type \"exit\"");
                            Console.ForegroundColor = ConsoleColor.White;
                            settingActive = false;
                            break;
                        case 2:
                            int number = Int32.Parse(CheckInput("Max number of shards:", @"^[0-9]{1,10}$"));                           
                            maxNumberOfShards = number;
                            File.WriteAllText("port.data", NodeInfo.portOfNode.ToString() + "\n" + NodeInfo.maxNumberOfShards);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Number changed");
                            Console.ForegroundColor = ConsoleColor.White;
                            settingActive = false;
                            break;

                    }

                }
                else
                {
                    break;
                }
            }
        }

        public static string CheckInput(string text, string reg)
        {
            bool continuee = false;
            while (continuee != true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(text);
                Console.ForegroundColor = ConsoleColor.White;
                string userInput = Console.ReadLine();
                Regex pattern = new Regex(reg);
                Match match = pattern.Match(userInput);
                if (match.Success)
                {
                    continuee = true;
                    return userInput;
                }
            }
            return null;
        }
        public static void viewSavedManifests()
        {
            DirectoryInfo dir = new DirectoryInfo("manifests");
            var files = dir.GetFiles(@"*.json");
            Console.WriteLine("Manifests:");
            foreach (var manifest in files)
            {
                Console.WriteLine(manifest.Name.Split(".")[0]);
            }
        }
    }

}
