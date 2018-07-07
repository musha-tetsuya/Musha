using System.IO;

namespace MushaEngine {

/// <summary>
/// BinaryReader拡張クラス
/// </summary>
public static class BinaryReaderExtension
{
	/// <summary>
	/// これ以上読み込めない場合true
	/// </summary>
	public static bool IsEnd(this BinaryReader reader)
	{
		return reader.BaseStream.Length <= reader.BaseStream.Position;
	}
	/// <summary>
	/// Boolean値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static bool ReadBoolean(this BinaryReader reader, bool defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadBoolean();
	}
	/// <summary>
	/// Byte値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static byte ReadByte(this BinaryReader reader, byte defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadByte();
	}
	/// <summary>
	/// Double値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static double ReadDouble(this BinaryReader reader, double defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadDouble();
	}
	/// <summary>
	/// Int16値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static short ReadInt16(this BinaryReader reader, short defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadInt16();
	}
	/// <summary>
	/// Int32値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static int ReadInt32(this BinaryReader reader, int defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadInt32();
	}
	/// <summary>
	/// Int64値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static long ReadInt64(this BinaryReader reader, long defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadInt64();
	}
	/// <summary>
	/// SByte値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static sbyte ReadSByte(this BinaryReader reader, sbyte defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadSByte();
	}
	/// <summary>
	/// Single値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static float ReadSingle(this BinaryReader reader, float defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadSingle();
	}
	/// <summary>
	/// String値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static string ReadString(this BinaryReader reader, string defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadString();
	}
	/// <summary>
	/// UInt16値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static ushort ReadUInt16(this BinaryReader reader, ushort defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadUInt16();
	}
	/// <summary>
	/// UInt32値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static uint ReadUInt32(this BinaryReader reader, uint defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadUInt32();
	}
	/// <summary>
	/// UInt64値を読み込む
	/// </summary>
	/// <param name="defaultValue">読み込めなかった場合に返す値</param>
	public static ulong ReadUInt64(this BinaryReader reader, ulong defaultValue)
	{
		return reader.IsEnd() ? defaultValue : reader.ReadUInt64();
	}
}

}//namespace MushaEngine
