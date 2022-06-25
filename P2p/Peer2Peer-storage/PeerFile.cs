using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Net.Sockets;
using ZipFile = Ionic.Zip.ZipFile;
using System.Text.RegularExpressions;

namespace Peer2Peer_storage
{
    class PeerFile
    {
        private Peers nodedInNetwork;
        private List<FileInfo> shards = new List<FileInfo>();
        public PeerFile(Peers nodedInNetwork)
        {
            NodeStatic.DirCreate("files");
            NodeStatic.DirCreate("shards");
            NodeStatic.DirCreate("temp");
            NodeStatic.DirCreate("manifests");

            this.nodedInNetwork = nodedInNetwork;
            DirectoryInfo dir = new DirectoryInfo("shards");
            var files = dir.GetFiles(@"*.shard");
            foreach(var shard in files)
            {
                shards.Add(new FileInfo(shard.Name,(int)shard.Length));
            }
        }
        public void UploadFile(string path)
        {
            string[] addreses;
            if (File.Exists(path))
            {
                Console.WriteLine("Uploading process started");

                string name = path;
                bool absolute = false;

                Regex pattern = new Regex(@"^[a-zA-Z]:\\[\\\S|*\S]?.*$");
                Match match = pattern.Match(path);
                if (match.Success)
                {
                    absolute = true;
                    var parts = path.Split("\\");
                    string nameOfFile = parts[parts.Length - 1];
                    name = nameOfFile;
                }

                Console.WriteLine("Compressing file");
                Random r = new Random();
                int fileId = r.Next(1000);
                string compresedFilePath = "temp/" + fileId + ".zip";

                ZipFile zip = new ZipFile();
                if (absolute)
                {
                    zip.AddFile(path, "");
                }
                else
                {
                    zip.AddFile(path);
                }

                zip.Save(compresedFilePath);

                var file = File.ReadAllBytes(compresedFilePath);

                Console.WriteLine("Compressing done");

                int actualShards = nodedInNetwork.Nodes.Count;
                int numberOfShards = 2;
                if (actualShards >= 2)
                {
                    if(actualShards == 2)
                    {
                        numberOfShards = 2;
                    }else
                    {
                        if(file.Length <= 1000000)
                        {
                            numberOfShards = 2;
                        }else
                        {
                            if(file.Length <= 10000000)
                            {
                                numberOfShards = 4;
                            }
                            else
                            {
                                numberOfShards = NodeInfo.maxNumberOfShards;
                            }
                        }
                            
                    }
                }

                if(numberOfShards > actualShards)
                {
                    numberOfShards = actualShards;
                }
                
                string hashOfContent = NodeStatic.GetContentHash(path);

                int pingCount = 0;
                ClientConnection[] clients = new ClientConnection[numberOfShards];
                addreses = new string[numberOfShards];
                if(numberOfShards == actualShards)
                {
                    for (int x = 0; x < numberOfShards; x++)
                    {
                        ClientConnection c = new ClientConnection(nodedInNetwork.Nodes[x]);
                        if (c.Client.Connected)
                        {
                            clients[x] = c;
                            addreses[x] = nodedInNetwork.Nodes[x];
                            pingCount++;
                        }
                    }
                }
                else
                {
                        var list = nodedInNetwork.Nodes.OrderBy(x => r.Next()).Take(numberOfShards);
                        int index = 0;
                        foreach(var node in list)
                        {
                        ClientConnection c = new ClientConnection(node);
                        if (c.Client.Connected)
                        {
                            clients[index] = c;
                            addreses[index] = node;
                            pingCount++;
                        }
                        index++;
                    }                                        
                }               
                if(pingCount != numberOfShards)
                {
                    Console.WriteLine("Nodes not responding");
                    return;
                }              
              
                Console.WriteLine("Sharding file");              

                int lengthOfShard = file.Length / numberOfShards;
              
                int overflow = file.Length - lengthOfShard * numberOfShards;
                int additionalBytes = 0;

                if(overflow > 0)
                {
                    Console.WriteLine("First shard will be " + overflow + " bytes bigger");
                    additionalBytes = overflow;
                }
               
                List<byte[]> shards = new List<byte[]>();
                int pointer = 0;

                byte[] shard = new byte[lengthOfShard + additionalBytes];
                for (int i = 0; i < lengthOfShard + additionalBytes; i++)
                {
                    shard[i] = file[pointer];
                    pointer++;
                }
                shards.Add(shard);

                for (int x = 2;x <= numberOfShards; x++)
                {
                    shard = new byte[lengthOfShard];
                    for (int i = 0; i < lengthOfShard; i++)
                    {
                        shard[i] = file[pointer];
                        pointer++;
                    } 
                    shards.Add(shard);
                }

                ShardManifest manifest = new ShardManifest(name, file.Length);        

                Console.WriteLine("Sharding file done");

                for (int x = 0;x < numberOfShards; x++)
                {
                    Console.WriteLine("Shard "+ (x+1)+ "/"+ numberOfShards+" sending to " + clients[x].Client.Client.RemoteEndPoint);
                    NodeStatic.SendToClient(clients[x].Client, clients[x].WriterClient, shards[x],name,hashOfContent);
                    manifest.AddShard(hashOfContent, addreses[x]);
                    Console.WriteLine("Shard " + (x + 1) + "/" + numberOfShards + " uploaded");
                    clients[x].Client.Close();
                }
                Console.WriteLine(numberOfShards + "/" + numberOfShards + " shards uploaded");


                string json = JsonSerializer.Serialize(manifest);
                File.WriteAllText("manifests/" + name.Split(".")[0] + ".json", json); ;

                Console.WriteLine("Manifest created");

                File.Delete(compresedFilePath);
            }
            else
            {
                Console.WriteLine("File not exists");
                return;
            }
        }             

        public bool ReceiveFromClient(TcpClient client, string name, int length)
        {          
            return NodeStatic.ReceiveFromClient(client, name, length, shards);
        }

        public void FetchShard(StreamWriter clientWriter,StreamReader clientReader, string shardName)
        {
            NodeStatic.FetchShard(clientWriter, clientReader, shardName); 
        }
        
        public void FileRequest(StreamWriter clientWriter,string name, TcpClient client)
        {
            NodeStatic.FileRequest(clientWriter, name, client, shards);
        }

        public void DownloadFile(string manifestFile)
        {
            if(File.Exists("manifests/"+ manifestFile))
            {
                string json = File.ReadAllText("manifests/"+ manifestFile);
                ShardManifest manifest = (ShardManifest)JsonSerializer.Deserialize<ShardManifest>(json);
                Console.WriteLine("Manifest loaded");
                Console.WriteLine("Numbers of shards:"+ manifest.Shards.Count);
                List<byte[]> shards = new List<byte[]>();
                for (int x = 0;x < manifest.Shards.Count; x++)
                {
                    ClientConnection client = new ClientConnection(manifest.Shards[x].Address);
                    Console.WriteLine("Fetching shard from " + manifest.Shards[x].Address);
                    if (client.Client.Client.Connected)
                    {
                        NodeStatic.WriteToClient(client.WriterClient,"p2p;requestShard;" + manifest.Shards[x].Hash);
                        string response = NodeStatic.ReadFromClient(client.ReaderClient);
                       
                        if (response.Contains("p2p;getOk;"))
                        {
                            Console.WriteLine("Downloading shard");
                            byte[] shard = new byte[long.Parse(response.Split(";")[2])];

                            int length = Int32.Parse(response.Split(";")[2]);
                            byte[] buffer = new byte[length];
                            int received = 0;
                            int read = 0;
                            int size = 1024;
                            int remaning = 0;

                            int view = 1;
                            int cycle = 0;

                            while(received < length)
                            {                                                          
                                remaning = length - received;
                                cycle++;
                                if (cycle == view * 5000)
                                {
                                    //double percent = 100 - ((100 * remaning) / length);
                                    //Console.WriteLine(percent + "%");
                                    view++;
                                }
                                if (remaning  < size)
                                {
                                    size = remaning;
                                }
                                try
                                {
                                    read = client.Client.GetStream().Read(buffer, received, size);
                                    received += read;
                                }
                                catch(Exception t)
                                {
                                    Console.WriteLine("Network error");
                                    return;
                                }
                                
                            }
                            Console.WriteLine("100%");
                            Console.WriteLine("Shard "+ (x+1)+ "/"+ manifest.Shards.Count+ " downloaded");
                            client.Client.Close();

                            shards.Add(buffer);
                        }
                        else
                        {
                            Console.WriteLine("Some shards missing or are offline");
                            return;
                        }
                        if(shards.Count == manifest.Shards.Count)
                        {                      
                        Console.WriteLine("Making final file");
                         
                        byte[] finalFile = new byte[manifest.DeCompressedSize]; 

                        int pointer = 0;
                        for(int i = 0;i < shards.Count; i++)
                        {
                                for (int b = 0;b < shards[i].Length; b++)
                                {
                                    finalFile[pointer] = shards[i][b];
                                    pointer++;                                  
                                }
                        }

                            Random r = new Random();
                            string compressedFilePath = "temp/d"+ r.Next(1000)+ ".zip";
                            
                            File.WriteAllBytes(compressedFilePath,finalFile);

                            Console.WriteLine("Decompressing file");

                            try {
                                using (ZipFile zip = new ZipFile(compressedFilePath))
                                {
                                    try
                                    {                                       
                                        zip.ExtractAll("files/");
                                    }
                                    catch (Exception t)
                                    {
                                        Console.WriteLine("Compression error or file already exists");
                                    }
                                }
                            }catch(Exception t)
                            {
                                Console.WriteLine("Compression error");
                                return;
                            }
                            
                                               
                            File.Delete(compressedFilePath);

                            Console.WriteLine("File succesfuly saved in "+ Environment.CurrentDirectory+ "\\files\\"+ manifest.Name);

                        }
                    }else
                    {
                        Console.WriteLine("Node is not avaible");
                        return;
                    }
                }             
            }else
            {
                Console.WriteLine("File not exists");
            }
        }

        public void DeleteFile(string manifestName)
        {
            NodeStatic.DeleteFile(manifestName);
        }

        public void DeleteShard(StreamWriter clientWriter, string hash)
        {
            NodeStatic.DeleteShard(clientWriter, hash, shards);
        }       
    }
}
