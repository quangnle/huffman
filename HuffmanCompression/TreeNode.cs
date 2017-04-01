using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuffmanCompression
{
    class TreeNode
    {
        public byte Symbol { get; set; }
        public int Frequency { get; set; }
        public int BinaryCode { get; set; }
        public int Length { get; set; }
        public TreeNode Parent { get; set; }
        public TreeNode Left { get; set; }
        public TreeNode Right { get; set; }

        public byte[] ToByteArray()
        {
            byte[] arr = new byte[6];
            arr[0] = this.Symbol;
            var byteCode = BitConverter.GetBytes(this.BinaryCode);
            arr[1] = byteCode[0];
            arr[2] = byteCode[1];
            arr[3] = byteCode[2];
            arr[4] = byteCode[3];
            arr[5] = (byte)Length;
            return arr;
        }

        public void Load(byte[] arr)
        {
            this.Symbol = arr[0];
            this.BinaryCode = (arr[4] << 24) | (arr[3] << 16) | (arr[2] << 8) | arr[1];
            this.Length = arr[5];
        }
    }
}
