using System;
using System.Net;
using System.Text;

namespace Comms;
/// <summary>
/// Reader 类用于从字节数组中读取各种数据类型，
/// 主要应用于序列化数据的解析和反序列化。
/// </summary>
public class Reader
{
    // 当前读取的位置，指向字节数组中的索引。
    private int _Position;
    // 存储需要解析的字节数组。
    private byte[] Bytes;
    /// <summary>
    /// Position 属性：获取或设置当前读取的索引位置。
	/// 如果设置的值超出字节数组的范围，则抛出 InvalidOperationException 异常。
    /// </summary>
    public int Position
	{
		get
		{
			return _Position;
		}
		set
		{
			if (value < 0 || value > Length)
			{
				throw new InvalidOperationException("Position out of bounds.");
			}
			_Position = value;
		}
	}
    /// <summary>
    /// Length 属性：返回字节数组的总长度。
    /// </summary>
    public int Length => Bytes.Length;
    /// <summary>
    ///  构造函数，接受一个字节数组用于初始化读取器。
    /// </summary>
    /// <param name="bytes"></param>
    public Reader(byte[] bytes)
	{
		Bytes = bytes;
	}
    /// <summary>
    /// ReadBoolean 方法：从当前索引读取一个字节，并将其解释为布尔值。
	/// 如果读取到的字节值为 0，则返回 false；否则返回 true。
    /// </summary>
    /// <returns>布尔</returns>
    public bool ReadBoolean()
	{
		return ReadByte() != 0;
	}
    /// <summary>
    /// ReadByte 方法：读取一个字节并返回。
	/// 如果当前索引超出字节数组的长度，则抛出异常。
    /// </summary>
    /// <returns>字节</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public byte ReadByte()
	{
		if (_Position + 1 > Length)
		{
			throw new InvalidOperationException("Reading beyond end of data.");
		}
		return Bytes[_Position++];
	}
    /// <summary>
    /// ReadChar 方法：读取两个字节并将其解释为一个字符。
    /// </summary>
    /// <returns>字符</returns>
    public char ReadChar()
	{
		return (char)ReadInt16();
	}
    /// <summary>
    /// ReadInt16 方法：读取两个字节并将其解释为有符号 16 位整数。
    /// </summary>
    /// <returns>有符号 16 位整数</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public short ReadInt16()
	{
		if (_Position + 2 > Length)
		{
			throw new InvalidOperationException("Reading beyond end of data.");
		}
		return (short)(Bytes[_Position++] | (Bytes[_Position++] << 8));
	}
    /// <summary>
    /// ReadUInt16 方法：读取两个字节并将其解释为无符号 16 位整数。
    /// </summary>
    /// <returns>无符号 16 位整数</returns>
    public ushort ReadUInt16()
	{
		return (ushort)ReadInt16();
	}
    /// <summary>
    /// ReadInt32 方法：读取四个字节并将其解释为有符号 32 位整数。
    /// </summary>
    /// <returns>有符号 32 位整数</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public int ReadInt32()
	{
		if (_Position + 4 > Length)
		{
			throw new InvalidOperationException("Reading beyond end of data.");
		}
		return Bytes[_Position++] | (Bytes[_Position++] << 8) | (Bytes[_Position++] << 16) | (Bytes[_Position++] << 24);
	}
    /// <summary>
    /// ReadUInt32 方法：读取四个字节并将其解释为无符号 32 位整数。
    /// </summary>
    /// <returns>无符号 32 位整数</returns>
    public uint ReadUInt32()
	{
		return (uint)ReadInt32();
	}
    /// <summary>
    /// ReadInt64 方法：读取八个字节并将其解释为有符号 64 位整数。
    /// </summary>
    /// <returns>有符号 64 位整数</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public long ReadInt64()
	{
		if (_Position + 8 > Length)
		{
			throw new InvalidOperationException("Reading beyond end of data.");
		}
		int num = Bytes[_Position++] | (Bytes[_Position++] << 8) | (Bytes[_Position++] << 16) | (Bytes[_Position++] << 24);
		uint num2 = (uint)(Bytes[_Position++] | (Bytes[_Position++] << 8) | (Bytes[_Position++] << 16) | (Bytes[_Position++] << 24));
		return (long)(((ulong)(uint)num << 32) | num2);
	}
    /// <summary>
    /// ReadUInt64 方法：读取八个字节并将其解释为无符号 64 位整数。
    /// </summary>
    /// <returns>无符号 64 位整数</returns>
    public ulong ReadUInt64()
	{
		return (ulong)ReadInt64();
	}
    /// <summary>
    /// ReadSingle 方法：读取四个字节并将其解释为单精度浮点数 (float)。
	/// 使用 unsafe 语法直接将整数位转换为浮点数。
    /// </summary>
    /// <returns>单精度浮点数</returns>
    public unsafe float ReadSingle()
	{
		int num = ReadInt32();
		return *(float*)(&num);
	}
    /// <summary>
    /// ReadDouble 方法：读取八个字节并将其解释为双精度浮点数 (double)。
	/// 使用 unsafe 语法直接将整数位转换为双精度数。
    /// </summary>
    /// <returns>双精度浮点数</returns>
    public unsafe double ReadDouble()
	{
		long num = ReadInt64();
		return *(double*)(&num);
	}
    /// <summary>
    /// ReadPackedInt32 方法：读取 7 位打包整数，适用于变长编码。
	/// 用于节省空间的情况下读取大整数。
    /// </summary>
    /// <returns>读取打包整数</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public int ReadPackedInt32()
	{
		int num = 0;
		int num2 = 0;
		byte b;
		do
		{
			if (num2 == 35)
			{
				throw new InvalidOperationException("Corrupt 7-bit packed int.");
			}
			b = ReadByte();
			num |= (b & 0x7F) << num2;
			num2 += 7;
		}
		while ((b & 0x80u) != 0);
		return num;
	}
    /// <summary>
    /// ReadPackedInt32 方法的扩展版本：读取范围内的打包整数。
	/// 如果读取的值超出指定范围，则抛出异常。
    /// </summary>
    /// <param name="minValue">最小值</param>
    /// <param name="maxValue">最大值</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public int ReadPackedInt32(int minValue, int maxValue)
	{
		int num = ReadPackedInt32();
		if (num < minValue)
		{
			throw new InvalidOperationException("Value too small.");
		}
		if (num > maxValue)
		{
			throw new InvalidOperationException("Value too large.");
		}
		return num;
	}
    /// <summary>
    /// ReadFixedBytes 方法：读取固定长度的字节数组。
	/// 如果读取的字节超出字节数组的长度范围，则抛出异常。
    /// </summary>
    /// <param name="count">数量</param>
    /// <returns>字节数组</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public byte[] ReadFixedBytes(int count)
	{
		if (_Position + count > Bytes.Length)
		{
			throw new InvalidOperationException("Reading beyond end of data.");
		}
		byte[] array = new byte[count];
		Array.Copy(Bytes, _Position, array, 0, count);
		_Position += count;
		return array;
	}
    /// <summary>
    /// ReadBytes 方法：读取变长字节数组，首先读取数组的长度，然后读取该长度的字节。
    /// </summary>
    /// <returns>字节数组</returns>
    public byte[] ReadBytes()
	{
		int count = ReadPackedInt32();
		return ReadFixedBytes(count);
	}
    /// <summary>
    /// ReadString 方法：读取 UTF-8 编码的字符串。
	/// 通过读取变长字节数组并将其转换为字符串返回。
    /// </summary>
    /// <returns>字符串</returns>
    public string ReadString()
	{
		return Encoding.UTF8.GetString(ReadBytes());
	}
    /// <summary>
    /// ReadIPEndPoint 方法：读取 IP 地址和端口号并返回 IPEndPoint 实例。
	/// IP 地址通过字节数组表示，端口号是无符号 16 位整数。
    /// </summary>
    /// <returns>IP 地址和端口号</returns>
    public IPEndPoint ReadIPEndPoint()
	{
		byte[] address = ReadBytes();
		return new IPEndPoint(port: ReadUInt16(), address: new IPAddress(address));
	}
}
