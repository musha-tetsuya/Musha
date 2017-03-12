using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Musha
{
	public class BinaryReaderEx : BinaryReader
	{
		public BinaryReaderEx(Stream input) : base(input) { }
		public BinaryReaderEx(Stream input, Encoding encoding) : base(input, encoding) { }
		public bool IsEnd { get { return BaseStream.Length <= BaseStream.Position; } }
		public bool ReadBoolean(bool defaultValue) { return IsEnd ? defaultValue : ReadBoolean(); }
		public byte ReadByte(byte defaultValue) { return IsEnd ? defaultValue : ReadByte(); }
		public double ReadDouble(double defaultValue) { return IsEnd ? defaultValue : ReadDouble(); }
		public short ReadInt16(short defaultValue) { return IsEnd ? defaultValue : ReadInt16(); }
		public int ReadInt32(int defaultValue) { return IsEnd ? defaultValue : ReadInt32(); }
		public long ReadInt64(long defaultValue) { return IsEnd ? defaultValue : ReadInt64(); }
		public sbyte ReadSByte(sbyte defaultValue) { return IsEnd ? defaultValue : ReadSByte(); }
		public float ReadSingle(float defaultValue) { return IsEnd ? defaultValue : ReadSingle(); }
		public string ReadString(string defaultValue) { return IsEnd ? defaultValue : ReadString(); }
		public ushort ReadUInt16(ushort defaultValue) { return IsEnd ? defaultValue : ReadUInt16(); }
		public uint ReadUInt32(uint defaultValue) { return IsEnd ? defaultValue : ReadUInt32(); }
		public ulong ReadUInt64(ulong defaultValue) { return IsEnd ? defaultValue : ReadUInt64(); }
	}
}
