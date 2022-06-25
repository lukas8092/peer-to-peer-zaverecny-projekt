using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Peer2Peer_storage
{
    public class ShardManifest
    {
        private string name;
        private List<ShardPlace> shards = new List<ShardPlace>();
        private int deCompressedSize;
        public ShardManifest(string name,int deCompressedSize)
        {
            this.Name = name;
            this.deCompressedSize = deCompressedSize;
        }
        public ShardManifest()
        {

        }
        public List<ShardPlace> Shards { get => shards; set => shards = value; }
        public string Name { get => name; set => name = value; }
        public int DeCompressedSize { get => deCompressedSize; set => deCompressedSize = value; }

        public void AddShard(string hash, string address)
        {
            Shards.Add(new ShardPlace(hash, address));
        }

        public void SaveToFile()
        {
            string json = JsonSerializer.Serialize(this);
            File.WriteAllText("/manifests/" + Name + ".json", json);
        }
    }
}
