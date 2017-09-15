#define Debug
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuffmanCompression
{
    public class Huffman
    {
        private Dictionary<byte, TreeNode> _dict = new Dictionary<byte, TreeNode>();
        private TreeNode _root = null;
        private int[] _masks = new int[] { 0x0, 0x1, 0x3, 0x7, 0xF, 0x1F, 0x3F, 0x7F, 0xFF, 0x1FF, 0x3FF, 0x7FF, 0xFFF, 0x1FFF, 0x3FFF, 0x7FFF, 0xFFFF, 0x1FFFF, 0x3FFFF, 0x7FFFF, 0xFFFFF, 0x1FFFFF, 0x3FFFFF, 0x7FFFFF, 0xFFFFFF, 0x1FFFFFF, 0x3FFFFFF, 0x7FFFFFF, 0xFFFFFFF, 0x1FFFFFFF, 0x3FFFFFFF, 0x7FFFFFFF };

        public void Compress(string inFile, string outFile)
        {
            var content = File.ReadAllBytes(inFile);
            var freqTable = BuildFrequencyTable(content);
            _root = BuildHuffmanTree(freqTable);
            UpdateTree(_root, "", 0, 0);

            var len = 0;
            var compress = Compress(content, out len);
            var header = GenerateHeader(len);
            var fileContent = new List<byte>();
            fileContent.AddRange(header);
            fileContent.AddRange(compress);

            File.WriteAllBytes(outFile, fileContent.ToArray());
        }

        public void Decompress(string inFile, string outFile)
        {
            var content = File.ReadAllBytes(inFile);
            var len = 0;
            var headerSize = 0;
            var dict = DecompressHeader(content, out len, out headerSize);

            var data = ByteUtils.GetBytes(content, headerSize, content.Length - headerSize);
            var origin = new List<byte>();
            var buffer = 0;
            var decprLen = 0;

            var curCode = 0;
            var curLen = 0;
            for (int i = 0; i < data.Length; i++)
            {
                buffer = (byte)data[i];
                for (int j = 0; j < 8; j++)
                {
                    var sb = buffer >> (7 - j) & 1;
                    buffer = buffer & _masks[7 - j];

                    curCode = curCode << 1 | sb;
                    curLen++;
                    var node = GetNode(dict, curCode, curLen);

                    if (node != null)
                    {
                        origin.Add(node.Symbol);
                        curCode = 0;
                        curLen = 0;
                    }

                    decprLen++;
                    if (decprLen == len)
                        break;
                }
            }

            File.WriteAllBytes(outFile, origin.ToArray());
        }

        #region Compression preparation methods
        private List<byte> Compress(byte[] content, out int len)
        {
            var compression = new List<byte>();
            var totalLength = 0;
            var runLen = 0;
            var buffer = 0;

            for (int i = 0; i < content.Length; i++)
            {
                var node = _dict[content[i]];
                buffer = buffer << node.Length | node.BinaryCode;
                runLen = runLen + node.Length;

                var nBytes = runLen / 8;
                var nRightBits = runLen % 8;

                for (int j = 0; j < nBytes; j++)
                {
                    var shftR = nRightBits + ((nBytes - j - 1) << 3);
                    var c = buffer >> shftR;
                    compression.Add((byte)c);
                    buffer = buffer & _masks[shftR];
                }

                totalLength += node.Length;
                runLen = nRightBits;
            }

            if (totalLength % 8 != 0)
            {
                var pad = 8 - totalLength % 8;
                buffer = buffer << pad;
                compression.Add((byte)buffer);
            }

            len = totalLength;
            return compression;
        }

        private List<byte> GenerateHeader(int length)
        {
            if (_dict != null || _dict.Count != 0)
            {
                var arr = new List<byte>();
                arr.Add((byte)'Q');
                arr.Add((byte)'P');
                arr.Add((byte)'K');

                var dataSize = BitConverter.GetBytes(length);
                arr.AddRange(dataSize);
                var nTuples = (byte)_dict.Count;
                arr.Add(nTuples);
                foreach (var node in _dict)
                {
                    var cArr = node.Value.ToByteArray();
                    arr.AddRange(cArr);
                }
                return arr;
            }

            return null;
        }

        private void UpdateTree(TreeNode node, string code, int bCode, int len)
        {
            if (node.Left == null && node.Right == null)
            {
                node.BinaryCode = bCode;
                node.Length = len;
                _dict.Add(node.Symbol, node);

#if Debug
                Console.WriteLine("{0} ({1}): Freq: {2}; Code: {3} ({4})", (char)node.Symbol, node.Symbol, node.Frequency, code, bCode);
#endif
            }
            else
            {
                if (node.Left != null)
                    UpdateTree(node.Left, code + "0", bCode << 1, len + 1);

                if (node.Right != null)
                    UpdateTree(node.Right, code + "1", bCode << 1 | 1, len + 1);
            }
        }

        private TreeNode BuildHuffmanTree(List<KeyValuePair<byte, int>> freqTable)
        {
            var mainStack = new Stack<TreeNode>();
            for (int i = 0; i < freqTable.Count; i++)
            {
                mainStack.Push(new TreeNode { Symbol = freqTable[i].Key, Frequency = freqTable[i].Value });
            }

            var temp = new Stack<TreeNode>();

            while (mainStack.Count() != 1)
            {
                var leftNode = mainStack.Pop();
                var rightNode = mainStack.Pop();
                var newNode = new TreeNode { Frequency = leftNode.Frequency + rightNode.Frequency, Left = leftNode, Right = rightNode };
                leftNode.Parent = newNode;
                rightNode.Parent = newNode;

                while (mainStack.Count() > 0 && mainStack.Peek().Frequency < newNode.Frequency)
                {
                    var item = mainStack.Pop();
                    temp.Push(item);
                }

                mainStack.Push(newNode);

                while (temp.Count() > 0)
                {
                    var item = temp.Pop();
                    mainStack.Push(item);
                }
            }

            var root = mainStack.Pop();
            return root;
        }

        private List<KeyValuePair<byte, int>> BuildFrequencyTable(byte[] content)
        {
            var arr = new int[256];
            for (int i = 0; i < content.Length; i++)
            {
                arr[content[i]]++;
            }

            var dict = new Dictionary<byte, int>();
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] > 0)
                    dict.Add((byte)i, arr[i]);
            }

            var result = dict.ToList().OrderByDescending(c => c.Value).ToList();

            return result;
        }
        #endregion

        #region Decompression preparation methods
        private Dictionary<byte, TreeNode> DecompressHeader(byte[] content, out int zipLen, out int headerSize)
        {
            if (content[0] != 'Q' || content[1] != 'P' || content[2] != 'K')
            {
                zipLen = 0;
                headerSize = 0;
                return null;
            }
            var dataIndex = 8;
            var blockSize = 6;

            var sizeData = ByteUtils.GetBytes(content, 3, 4);
            zipLen = ByteUtils.GetInt(sizeData);

            var nTuples = content[7];
            var dict = new Dictionary<byte, TreeNode>();
            for (int i = 0; i < nTuples; i++)
            {
                var chunk = ByteUtils.GetBytes(content, dataIndex + i * blockSize, blockSize);
                var node = new TreeNode();
                node.Load(chunk);
                dict.Add(node.Symbol, node);
            }

            headerSize = dataIndex + nTuples * blockSize;
            return dict;
        }

        private TreeNode GetNode(Dictionary<byte, TreeNode> dict, int code, int length)
        {
            foreach (var item in dict)
            {
                if (item.Value.BinaryCode == code && item.Value.Length == length)
                    return item.Value;
            }

            return null;
        }
        #endregion
        
        #region unused 
        /// <summary>
        /// demo algorithm to unpack using Huffman tree
        /// </summary>
        /// <param name="content"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        private string Decompress(string content, int len)
        {
            var origin = new StringBuilder();
            var buffer = 0;
            var decprLen = 0;

            TreeNode curNode = _root;
            for (int i = 0; i < content.Length; i++)
            {
                buffer = (byte)content[i];
                for (int j = 0; j < 8; j++)
                {
                    var sb = buffer >> (7 - j) & 1;
                    buffer = buffer & _masks[7 - j];
                    decprLen++;

                    if (sb == 0) curNode = curNode.Left;
                    else curNode = curNode.Right;

                    if (curNode.Left == null && curNode.Right == null)
                    {
                        origin.Append(curNode.Symbol);
                        curNode = _root;
                    }
                }

                if (decprLen == len) break;
            }
            return origin.ToString();
        }
        #endregion
    }
}
