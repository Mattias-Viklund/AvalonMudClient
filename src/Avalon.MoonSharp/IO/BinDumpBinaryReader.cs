using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MoonSharp.Interpreter.IO
{
    /// <summary>
    /// "Optimized" BinaryReader which shares strings and use a dumb compression for integers
    /// </summary>
    public class BinDumpBinaryReader : BinaryReader
    {
        private List<string> _strings = new List<string>();

        public BinDumpBinaryReader(Stream s) : base(s)
        {
        }

        public BinDumpBinaryReader(Stream s, Encoding e) : base(s, e)
        {
        }

        public override int ReadInt32()
        {
            sbyte b = base.ReadSByte();

            if (b == 0x7F)
            {
                return base.ReadInt16();
            }

            if (b == 0x7E)
            {
                return base.ReadInt32();
            }

            return b;
        }

        public override uint ReadUInt32()
        {
            byte b = base.ReadByte();

            if (b == 0x7F)
            {
                return base.ReadUInt16();
            }

            if (b == 0x7E)
            {
                return base.ReadUInt32();
            }

            return b;
        }

        public override string ReadString()
        {
            int pos = this.ReadInt32();

            if (pos < _strings.Count)
            {
                return _strings[pos];
            }

            if (pos == _strings.Count)
            {
                string str = base.ReadString();
                _strings.Add(str);
                return str;
            }

            throw new IOException("string map failure");
        }
    }
}