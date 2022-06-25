using System;
using System.Collections.Generic;
using System.Text;

namespace Peer2Peer_storage
{
    public class FileInfo
    {
        private string name;
        private int length;

        public string Name { get => name; set => name = value; }
        public int Length { get => length; set => length = value; }

        public FileInfo(string name, int length)
        {
            Name = name;
            Length = length;
        }
    }
}
