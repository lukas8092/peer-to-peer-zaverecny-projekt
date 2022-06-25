using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Peer2Peer_storage
{
    public class NodeStatic
    {
        public static string GetContentHash(string path)
        {
            MD5 md5 = MD5.Create();
            FileStream stream = File.OpenRead(path);
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static void DirCreate(string name)
        {
            if (!Directory.Exists(name))
            {
                Directory.CreateDirectory(name);
            }
        }
        public static void SendToClient(StreamWriter writer, string text)
        {
            try
            {
                writer.Write(text);
                writer.Flush();
            }
            catch (Exception t)
            {
                Console.WriteLine(t);
                return;
            }

        }
        public static string ReadFromClient(StreamReader reader)
        {
            try
            {
                return reader.ReadLine();
            }
            catch (Exception t)
            {
                Console.WriteLine(t);
                return "";
            }

        }
        public static void SendToClient(TcpClient client, StreamWriter clientWriter, byte[] bytes, string path, string hashOfContent)
        {
            var parts = path.Split("");
            try
            {
                clientWriter.WriteLine("p2p;upload;" + bytes.Length + ";" + hashOfContent + ".shard");
                clientWriter.Flush();
            }
            catch (Exception t)
            {
                Console.WriteLine(t);
                return;
            }

            Random random = new Random();
            string tempFile = "temp/" + random.Next(99999) + ".shard";
            File.WriteAllBytes(tempFile, bytes);
            client.Client.SendFile(tempFile);
            File.Delete(tempFile);
        }

        public static bool WriteToClient(StreamWriter clientWriter, string text)
        {
            try
            {
                clientWriter.WriteLine(text);
                clientWriter.Flush();
            }
            catch (Exception t)
            {
                Console.WriteLine(t);
                return false;
            }
            return true;
        }
        public static bool ReceiveFromClient(TcpClient client, string name, int length, List<FileInfo> shards)
        {
            byte[] buffer = new byte[length];
            int received = 0;
            int read = 0;
            int size = 1024;
            int remaning = 0;

            while (received < length)
            {
                remaning = length - received;
                if (remaning < size)
                {
                    size = remaning;
                }
                try
                {
                    read = client.GetStream().Read(buffer, received, size);
                    received += read;
                }
                catch (Exception t)
                {
                    Console.WriteLine("Network error");
                    return false;
                }
            }
            Console.WriteLine("Shard received");
            File.WriteAllBytes("shards/" + name, buffer);
            shards.Add(new FileInfo(name, length));
            client.Close();
            return true;
        }

        public static void FetchShard(StreamWriter clientWriter, StreamReader clientReader, string shardName)
        {
            try
            {
                clientWriter.WriteLine("p2p;requestShards;" + shardName);
                clientWriter.Flush();
            }
            catch (Exception t)
            {
                Console.WriteLine("Network error");
                return;
            }
            string response = NodeStatic.ReadFromClient(clientReader);
            if (response.Contains("p2p;getOk;"))
            {
                byte[] shard = new byte[Int32.Parse(response.Split(";")[2])];
                for (int x = 0; x < shard.Length; x++)
                {
                    try
                    {
                        shard[x] = (byte)clientReader.Read();
                    }
                    catch (Exception t)
                    {
                        Console.WriteLine("Network error");
                        return;
                    }

                }
                File.WriteAllBytes("temp/" + shardName, shard);
            }
            else
            {
                Console.WriteLine("Shard not found on that node");
            }
        }

        public static void FileRequest(StreamWriter clientWriter, string name, TcpClient client, List<FileInfo> shards)
        {
            name += ".shard";
            FileInfo file = shards.Where(x => x.Name == name).FirstOrDefault();
            if (shards.Where(x => x.Name == name).Count() == 1)
            {
                try
                {
                    clientWriter.WriteLine("p2p;getOk;" + file.Length);
                    clientWriter.Flush();
                }
                catch (Exception t)
                {
                    Console.WriteLine(t);
                    return;
                }
                byte[] shard = File.ReadAllBytes("shards/" + name);

                try
                {
                    client.Client.SendFile("shards/" + name);
                    clientWriter.Flush();
                }
                catch (Exception t)
                {
                    Console.WriteLine("Network error");
                    return;
                }
                Console.WriteLine("Shard sended");
            }
            else
            {
                NodeStatic.WriteToClient(clientWriter, "p2p;notFound;");
            }
        }

        public static void DeleteFile(string manifestName)
        {
            if (File.Exists("manifests/" + manifestName))
            {
                string json = File.ReadAllText("manifests/" + manifestName);
                ShardManifest manifest = (ShardManifest)JsonSerializer.Deserialize<ShardManifest>(json);

                foreach (var shard in manifest.Shards)
                {
                    ClientConnection client = new ClientConnection(shard.Address);
                    if (client.Client.Client.Connected)
                    {
                        NodeStatic.WriteToClient(client.WriterClient, "p2p;delete;" + shard.Hash);
                        string responce = NodeStatic.ReadFromClient(client.ReaderClient);
                        if (responce == "p2p;deleted;")
                        {
                            Console.WriteLine("Shard deleted");
                        }
                        else
                        {
                            Console.WriteLine("Shard cannot be deleted or its not there");
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                File.Delete("manifests/" + manifestName);
                Console.WriteLine("File is completly deleted");
            }
            else
            {
                Console.WriteLine("File not exists");
            }

        }

        public static void DeleteShard(StreamWriter clientWriter, string hash, List<FileInfo> shards)
        {
            hash += ".shard";
            FileInfo file = shards.Where(x => x.Name == hash).FirstOrDefault();
            if (shards.Where(x => x.Name == hash).Count() == 1)
            {
                File.Delete("shards/" + hash);
                shards.Remove(file);
                NodeStatic.SendToClient(clientWriter, "p2p;deleted;");
                Console.WriteLine("Shard deleted");
            }
            else
            {
                NodeStatic.SendToClient(clientWriter, "p2p;notFound");
            }
        }
    }
}
