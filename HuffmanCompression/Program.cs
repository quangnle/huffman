#define Debug

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HuffmanCompression
{
    class Program
    {   
        static void Main(string[] args)
        {
            var fileName = "test2.txt";

            var h = new Huffman();
            h.Compress(fileName, "cmpr.qpk");
            h.Decompress("cmpr.qpk", "origin.txt");

            Console.Read();
        }
    }
}
