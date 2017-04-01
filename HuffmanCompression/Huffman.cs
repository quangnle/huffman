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
        private Dictionary<char, TreeNode> _dict = new Dictionary<char, TreeNode>();
        private TreeNode _root = null;
        private int[] _masks = new int[] { 0, 1, 3, 7, 15, 31, 63, 127, 255, 511, 1023, 2047, 4095, 8191, 16383, 32767, 65535 };
        
        public void Compress(string inFile, string outFile)
        {
            var content = File.ReadAllText(inFile);
            var freqTable = BuildFrequencyTable(content);
            _root = BuildHuffmanTree(freqTable);
            UpdateTree(_root, "", 0, 0);
            
            var len = 0;
            var compress = Compress(content, out len);
            var header = GenerateHeader(len);
            var fileContent = header + compress;

            File.WriteAllText(outFile, fileContent
                );
        }

        public void Decompress(string inFile, string outFile)
        {
            var content = File.ReadAllText(inFile);
            var len = 0;
            var headerSize = 0;
            var dict = DecompressHeader(content, out len, out headerSize);

            var data = content.Substring(headerSize, content.Length - headerSize);
            var origin = new StringBuilder();
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
                        origin.Append(node.Symbol);
                        curCode = 0;
                        curLen = 0;
                    }

                    decprLen++;
                }

                if (decprLen == len)
                    break;
            }
            var fileContent = origin.ToString();
            origin.Clear();

            File.WriteAllText(outFile, fileContent);
        }

        #region Compression preparation methods
        private string Compress(string content, out int len)
        {
            var compressedStr = new StringBuilder();
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
                    compressedStr.Append((char)c);
                    buffer = buffer & _masks[shftR];
                }

                totalLength += node.Length;
                runLen = nRightBits;
            }

            if (totalLength % 8 != 0)
            {
                var pad = 8 - totalLength % 8;
                buffer = buffer << pad;
            }

            len = totalLength;
            return compressedStr.ToString();
        }

        private string GenerateHeader(int length)
        {
            if (_dict != null || _dict.Count != 0)
            {
                var sb = new StringBuilder();
                sb.Append("QPK");
                var dataSize = System.Text.Encoding.ASCII.GetString(BitConverter.GetBytes(length));
                sb.Append(dataSize);
                var nTuples = (char)_dict.Count;
                sb.Append(nTuples);
                foreach (var node in _dict)
                {
                    var cArr = node.Value.ToCharArray();
                    sb.Append(cArr);
                }
                return sb.ToString();
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
                Console.WriteLine("{0} ({1}): {2} - {3}", node.Symbol, (int)node.Symbol, node.Frequency, code);
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

        private TreeNode BuildHuffmanTree(List<KeyValuePair<char, int>> freqTable)
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

        private List<KeyValuePair<char, int>> BuildFrequencyTable(string content)
        {
            var arr = new int[256];
            for (int i = 0; i < content.Length; i++)
            {
                arr[content[i]]++;
            }

            var dict = new Dictionary<char, int>();
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] > 0)
                    dict.Add((char)i, arr[i]);
            }

            var result = dict.ToList().OrderByDescending(c => c.Value).ToList();

            return result;
        }
        #endregion

        #region Decompression preparation methods
        private Dictionary<char, TreeNode> DecompressHeader(string content, out int zipLen, out int headerSize)
        {
            if (content.Substring(0, 3) != "QPK")
            {
                zipLen = 0;
                headerSize = 0;
                return null;
            }
            var dataIndex = 8;
            var blockSize = 6;

            var sizeData = content.Substring(3, 4);
            var size = 0;
            var arBytes = sizeData.ToCharArray();
            for (int i = 0; i < 4; i++)
            {
                size = size << 8 | arBytes[3 - i];
            }
            zipLen = size;

            var nTuples = content[7];
            var dict = new Dictionary<char, TreeNode>();
            for (int i = 0; i < nTuples; i++)
            {
                var sub = content.Substring(dataIndex + i * blockSize, blockSize);
                var node = new TreeNode();
                node.LoadChars(sub.ToCharArray());
                dict.Add(node.Symbol, node);
            }

            headerSize = dataIndex + nTuples * blockSize;
            return dict;
        }

        private TreeNode GetNode(Dictionary<char, TreeNode> dict, int code, int length)
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
