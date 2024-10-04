using System;
using System.Net;
using System.Text;

namespace Comms;
/// <summary>
/// Reader �����ڴ��ֽ������ж�ȡ�����������ͣ�
/// ��ҪӦ�������л����ݵĽ����ͷ����л���
/// </summary>
public class Reader
{
    // ��ǰ��ȡ��λ�ã�ָ���ֽ������е�������
    private int _Position;
    // �洢��Ҫ�������ֽ����顣
    private byte[] Bytes;
    /// <summary>
    /// Position ���ԣ���ȡ�����õ�ǰ��ȡ������λ�á�
	/// ������õ�ֵ�����ֽ�����ķ�Χ�����׳� InvalidOperationException �쳣��
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
    /// Length ���ԣ������ֽ�������ܳ��ȡ�
    /// </summary>
    public int Length => Bytes.Length;
    /// <summary>
    ///  ���캯��������һ���ֽ��������ڳ�ʼ����ȡ����
    /// </summary>
    /// <param name="bytes"></param>
    public Reader(byte[] bytes)
	{
		Bytes = bytes;
	}
    /// <summary>
    /// ReadBoolean �������ӵ�ǰ������ȡһ���ֽڣ����������Ϊ����ֵ��
	/// �����ȡ�����ֽ�ֵΪ 0���򷵻� false�����򷵻� true��
    /// </summary>
    /// <returns>����</returns>
    public bool ReadBoolean()
	{
		return ReadByte() != 0;
	}
    /// <summary>
    /// ReadByte ��������ȡһ���ֽڲ����ء�
	/// �����ǰ���������ֽ�����ĳ��ȣ����׳��쳣��
    /// </summary>
    /// <returns>�ֽ�</returns>
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
    /// ReadChar ��������ȡ�����ֽڲ��������Ϊһ���ַ���
    /// </summary>
    /// <returns>�ַ�</returns>
    public char ReadChar()
	{
		return (char)ReadInt16();
	}
    /// <summary>
    /// ReadInt16 ��������ȡ�����ֽڲ��������Ϊ�з��� 16 λ������
    /// </summary>
    /// <returns>�з��� 16 λ����</returns>
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
    /// ReadUInt16 ��������ȡ�����ֽڲ��������Ϊ�޷��� 16 λ������
    /// </summary>
    /// <returns>�޷��� 16 λ����</returns>
    public ushort ReadUInt16()
	{
		return (ushort)ReadInt16();
	}
    /// <summary>
    /// ReadInt32 ��������ȡ�ĸ��ֽڲ��������Ϊ�з��� 32 λ������
    /// </summary>
    /// <returns>�з��� 32 λ����</returns>
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
    /// ReadUInt32 ��������ȡ�ĸ��ֽڲ��������Ϊ�޷��� 32 λ������
    /// </summary>
    /// <returns>�޷��� 32 λ����</returns>
    public uint ReadUInt32()
	{
		return (uint)ReadInt32();
	}
    /// <summary>
    /// ReadInt64 ��������ȡ�˸��ֽڲ��������Ϊ�з��� 64 λ������
    /// </summary>
    /// <returns>�з��� 64 λ����</returns>
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
    /// ReadUInt64 ��������ȡ�˸��ֽڲ��������Ϊ�޷��� 64 λ������
    /// </summary>
    /// <returns>�޷��� 64 λ����</returns>
    public ulong ReadUInt64()
	{
		return (ulong)ReadInt64();
	}
    /// <summary>
    /// ReadSingle ��������ȡ�ĸ��ֽڲ��������Ϊ�����ȸ����� (float)��
	/// ʹ�� unsafe �﷨ֱ�ӽ�����λת��Ϊ��������
    /// </summary>
    /// <returns>�����ȸ�����</returns>
    public unsafe float ReadSingle()
	{
		int num = ReadInt32();
		return *(float*)(&num);
	}
    /// <summary>
    /// ReadDouble ��������ȡ�˸��ֽڲ��������Ϊ˫���ȸ����� (double)��
	/// ʹ�� unsafe �﷨ֱ�ӽ�����λת��Ϊ˫��������
    /// </summary>
    /// <returns>˫���ȸ�����</returns>
    public unsafe double ReadDouble()
	{
		long num = ReadInt64();
		return *(double*)(&num);
	}
    /// <summary>
    /// ReadPackedInt32 ��������ȡ 7 λ��������������ڱ䳤���롣
	/// ���ڽ�ʡ�ռ������¶�ȡ��������
    /// </summary>
    /// <returns>��ȡ�������</returns>
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
    /// ReadPackedInt32 ��������չ�汾����ȡ��Χ�ڵĴ��������
	/// �����ȡ��ֵ����ָ����Χ�����׳��쳣��
    /// </summary>
    /// <param name="minValue">��Сֵ</param>
    /// <param name="maxValue">���ֵ</param>
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
    /// ReadFixedBytes ��������ȡ�̶����ȵ��ֽ����顣
	/// �����ȡ���ֽڳ����ֽ�����ĳ��ȷ�Χ�����׳��쳣��
    /// </summary>
    /// <param name="count">����</param>
    /// <returns>�ֽ�����</returns>
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
    /// ReadBytes ��������ȡ�䳤�ֽ����飬���ȶ�ȡ����ĳ��ȣ�Ȼ���ȡ�ó��ȵ��ֽڡ�
    /// </summary>
    /// <returns>�ֽ�����</returns>
    public byte[] ReadBytes()
	{
		int count = ReadPackedInt32();
		return ReadFixedBytes(count);
	}
    /// <summary>
    /// ReadString ��������ȡ UTF-8 ������ַ�����
	/// ͨ����ȡ�䳤�ֽ����鲢����ת��Ϊ�ַ������ء�
    /// </summary>
    /// <returns>�ַ���</returns>
    public string ReadString()
	{
		return Encoding.UTF8.GetString(ReadBytes());
	}
    /// <summary>
    /// ReadIPEndPoint ��������ȡ IP ��ַ�Ͷ˿ںŲ����� IPEndPoint ʵ����
	/// IP ��ַͨ���ֽ������ʾ���˿ں����޷��� 16 λ������
    /// </summary>
    /// <returns>IP ��ַ�Ͷ˿ں�</returns>
    public IPEndPoint ReadIPEndPoint()
	{
		byte[] address = ReadBytes();
		return new IPEndPoint(port: ReadUInt16(), address: new IPAddress(address));
	}
}
