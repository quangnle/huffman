using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuffmanCompression
{
    class ByteUtils
    {
        public static byte[] GetBytes(List<byte> lst, int startIndex, int length)
        {
            var arr = new byte[length];
            for (int i = 0; i < length; i++)
            {
                arr[i] = lst[startIndex + i];
            }
            return arr;
        }

        public static byte[] GetBytes(byte[] lst, int startIndex, int length)
        {
            var arr = new byte[length];
            for (int i = 0; i < length; i++)
            {
                arr[i] = lst[startIndex + i];
            }
            return arr;
        }

        public static int GetInt(byte[] arr)
        {
            var result = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                result = result << 8 | arr[arr.Length - i - 1];
            }
            return result;
        }
    }
}
