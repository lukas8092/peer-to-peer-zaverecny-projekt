using System;
using System.Collections.Generic;
using System.Text;

namespace Peer2Peer_storage
{
    public class ShardPlace
    {
        private string hash, address;

        public string Hash { get => hash; set => hash = value; }
        public string Address { get => address; set => address = value; }
        public ShardPlace(string hash, string address)
        {
            Hash = hash;
            Address = address;
        }
        public ShardPlace()
        {

        }
    }
}
