using System;
using System.Net;
using System.Text;

namespace Comms;
/// <summary>
/// Writer 类用于将各种数据类型写入字节数组，以便后续的网络传输
/// </summary>
public class Writer
{
	// 当前写入位置，表示下一个字节写入的位置索引
    private int _Position;
    // 表示字节数组的当前长度
    private int _Length;
    // 存储写入的字节数据的缓冲区，初始容量为 16
    private byte[] Bytes = new byte[16];
    /// <summary>
    /// 当前写入位置的属性
    /// </summary>
    public int Position
	{
		get
		{
			return _Position;
		}
		set
		{
            // 更新当前长度
            _Length = Length;
            // 如果设置的值超出有效范围，则抛出异常
            if (value < 0 || value > _Length)
			{
				throw new InvalidOperationException("Position out of bounds.");
			}
			_Position = value;
		}
	}
    /// <summary>
    /// 字节数组的长度属性
    /// </summary>
    public int Length
	{
		get
        {
			// 返回写入位置和当前长度中较大的值
            return Math.Max(_Position, _Length);
		}
		set
		{
            // 如果长度小于 0，抛出异常
            if (value < 0)
			{
				throw new InvalidOperationException("Length out of bounds.");
			}
            // 更新长度值
            _Length = Length;
            // 扩展缓冲区并清空新增的字节内容
            if (value > _Length)
			{
				EnsureCapacity(value);
				Array.Clear(Bytes, _Length, value - _Length);
				_Length = value;
			}
			else if (value < _Length)
			{
                // 如果减少长度，调整 _Position 到新长度
                _Length = value;
				_Position = value;
			}
		}
	}
    /// <summary>
    /// 获取当前字节数组的副本
    /// </summary>
    /// <returns>字节数组副本</returns>
    public byte[] GetBytes()
	{
		byte[] array = new byte[Length];
		Array.Copy(Bytes, array, array.Length);
		return array;
	}
    /// <summary>
    /// 写入布尔值，将其转换为 0 或 1 并写入字节数组
    /// </summary>
    /// <param name="value">布尔</param>
    public void WriteBoolean(bool value)
	{
		WriteByte(value ? ((byte)1) : ((byte)0));
	}
    /// <summary>
    /// 写入单字节的值
    /// </summary>
    /// <param name="value">单字节</param>
    public void WriteByte(byte value)
	{
		EnsureCapacity(_Position + 1);// 确保容量足够
        Bytes[_Position++] = value;// 写入字节并增加位置
    }
    /// <summary>
    /// 写入字符，使用两个字节存储 Unicode 值
    /// </summary>
    /// <param name="value">单字符</param>
    public void WriteChar(char value)
	{
		EnsureCapacity(_Position + 2);
		Bytes[_Position++] = (byte)value;
		Bytes[_Position++] = (byte)((int)value >> 8);
	}
    /// <summary>
    /// 写入 16 位有符号整数（2 字节）
    /// </summary>
    /// <param name="value">16 位有符号整数</param>
    public void WriteInt16(short value)
	{
		EnsureCapacity(_Position + 2);
		Bytes[_Position++] = (byte)value;
		Bytes[_Position++] = (byte)(value >> 8); // 将字符高位写入字节数组
    }
    /// <summary>
    /// 写入 16 位无符号整数，实际上是调用 WriteInt16 方法
    /// </summary>
    /// <param name="value">16 位无符号整数</param>
    public void WriteUInt16(ushort value)
	{
		WriteInt16((short)value);
	}
    /// <summary>
    /// 写入 32 位有符号整数（4 字节）
    /// </summary>
    /// <param name="value">32 位有符号整数</param>
    public void WriteInt32(int value)
	{
		EnsureCapacity(_Position + 4);
		Bytes[_Position++] = (byte)value;
		Bytes[_Position++] = (byte)(value >> 8);
		Bytes[_Position++] = (byte)(value >> 16);
		Bytes[_Position++] = (byte)(value >> 24);
	}
    /// <summary>
    /// 写入 32 位无符号整数，实际上是调用 WriteInt32 方法
    /// </summary>
    /// <param name="value">32 位无符号整数</param>
    public void WriteUInt32(uint value)
	{
		WriteInt32((int)value);
	}
    /// <summary>
	/// 写入 64 位有符号整数（8 字节）
	/// </summary>
	/// <param name="value">64 位有符号整数</param>
	public void WriteInt64(long value)
	{
		EnsureCapacity(_Position + 8);
		Bytes[_Position++] = (byte)value;
		Bytes[_Position++] = (byte)(value >> 8);
		Bytes[_Position++] = (byte)(value >> 16);
		Bytes[_Position++] = (byte)(value >> 24);
		Bytes[_Position++] = (byte)(value >> 32);
		Bytes[_Position++] = (byte)(value >> 40);
		Bytes[_Position++] = (byte)(value >> 48);
		Bytes[_Position++] = (byte)(value >> 56);
	}
    /// <summary>
    /// 写入 64 位无符号整数
    /// </summary>
    /// <param name="value">64 位无符号整数</param>
    public void WriteUInt64(ulong value)
	{
		WriteInt64((long)value);
	}
    /// <summary>
    /// 写入 32 位浮点数
    /// </summary>
    /// <param name="value">单精度浮点数</param>
    public unsafe void WriteSingle(float value)
	{
		WriteInt32(*(int*)(&value));// 将浮点数按位转换为整数后写入
    }
    /// <summary>
    /// 写入 64 位双精度浮点数
    /// </summary>
    /// <param name="value">双精度浮点数</param>
    public unsafe void WriteDouble(double value)
	{
		WriteInt64(*(long*)(&value));// 将双精度浮点数按位转换为整数后写入
    }
    /// <summary>
    /// 写入一个压缩的 32 位整数（变长编码，适用于小整数）
    /// </summary>
    /// <param name="value">压缩的32 位整数</param>
    public void WritePackedInt32(int value)
	{
		EnsureCapacity(_Position + 5);
		uint num;
		for (num = (uint)value; num >= 128; num >>= 7)
		{
			Bytes[_Position++] = (byte)(num | 0x80u);// 设置最高位以指示还有更多字节
        }
		Bytes[_Position++] = (byte)num;// 最后一个字节不设置最高位
    }
    /// <summary>
    /// 写入固定长度的字节数组
    /// </summary>
    /// <param name="bytes">字节数组</param>
    public void WriteFixedBytes(byte[] bytes)
	{
		if (bytes != null)
		{
			WriteFixedBytes(bytes, 0, bytes.Length);
		}
	}
    /// <summary>
    /// 写入固定长度的字节数组，指定开始索引和数量
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="start">开始索引</param>
    /// <param name="count">数量</param>
    public void WriteFixedBytes(byte[] bytes, int start, int count)
	{
		EnsureCapacity(_Position + count);
		Array.Copy(bytes, start, Bytes, _Position, count);
		_Position += count;
	}
    /// <summary>
    /// 写入可变长度的字节数组
    /// </summary>
    /// <param name="bytes"></param>
    public void WriteBytes(byte[] bytes)
	{
		if (bytes != null)
		{
			WriteBytes(bytes, 0, bytes.Length);
		}
		else
		{
			WriteByte(0);// 如果数组为空，则写入一个 0 字节
        }
	}
    /// <summary>
    /// 写入指定范围的可变长度字节数组
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="start">开始索引</param>
    /// <param name="count">数量</param>
    public void WriteBytes(byte[] bytes, int start, int count)
	{
		EnsureCapacity(_Position + 5 + count);
		WritePackedInt32(count);
		Array.Copy(bytes, start, Bytes, _Position, count);
		_Position += count;
	}
    /// <summary>
    /// 写入字符串，先写入字符串的字节长度，然后写入 UTF-8 编码的字节数据
    /// </summary>
    /// <param name="value">字符串</param>
    public void WriteString(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			int byteCount = Encoding.UTF8.GetByteCount(value);
			EnsureCapacity(_Position + 5 + byteCount);
			WritePackedInt32(byteCount);// 先写入字节长度
            _Position += Encoding.UTF8.GetBytes(value, 0, value.Length, Bytes, _Position);// 写入字符串内容
        }
		else
		{
			WriteByte(0);// 如果字符串为空，则写入一个 0 字节
        }
	}
    /// <summary>
    /// 写入 IPEndPoint 类型（网络地址和端口号）
    /// </summary>
    /// <param name="address"></param>
    public void WriteIPEndPoint(IPEndPoint address)
	{
		WriteBytes(address.Address.GetAddressBytes());// 写入 IP 地址
        WriteInt16((short)address.Port);// 写入端口号
    }
    /// <summary>
    /// // 确保缓冲区容量足够，如果容量不足则扩展缓冲区
    /// </summary>
    /// <param name="capacity">缓冲区</param>
    private void EnsureCapacity(int capacity)
	{
		if (capacity > Bytes.Length)
		{
			int num;
			for (num = Bytes.Length; num < capacity; num *= 2)
			{
			}
			byte[] array = new byte[num];
			Array.Copy(Bytes, array, _Position);
			Bytes = array;// 扩展缓冲区
        }
	}
}
