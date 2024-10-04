using System;
using System.Net;
using System.Text;

namespace Comms;
/// <summary>
/// Writer �����ڽ�������������д���ֽ����飬�Ա���������紫��
/// </summary>
public class Writer
{
	// ��ǰд��λ�ã���ʾ��һ���ֽ�д���λ������
    private int _Position;
    // ��ʾ�ֽ�����ĵ�ǰ����
    private int _Length;
    // �洢д����ֽ����ݵĻ���������ʼ����Ϊ 16
    private byte[] Bytes = new byte[16];
    /// <summary>
    /// ��ǰд��λ�õ�����
    /// </summary>
    public int Position
	{
		get
		{
			return _Position;
		}
		set
		{
            // ���µ�ǰ����
            _Length = Length;
            // ������õ�ֵ������Ч��Χ�����׳��쳣
            if (value < 0 || value > _Length)
			{
				throw new InvalidOperationException("Position out of bounds.");
			}
			_Position = value;
		}
	}
    /// <summary>
    /// �ֽ�����ĳ�������
    /// </summary>
    public int Length
	{
		get
        {
			// ����д��λ�ú͵�ǰ�����нϴ��ֵ
            return Math.Max(_Position, _Length);
		}
		set
		{
            // �������С�� 0���׳��쳣
            if (value < 0)
			{
				throw new InvalidOperationException("Length out of bounds.");
			}
            // ���³���ֵ
            _Length = Length;
            // ��չ������������������ֽ�����
            if (value > _Length)
			{
				EnsureCapacity(value);
				Array.Clear(Bytes, _Length, value - _Length);
				_Length = value;
			}
			else if (value < _Length)
			{
                // ������ٳ��ȣ����� _Position ���³���
                _Length = value;
				_Position = value;
			}
		}
	}
    /// <summary>
    /// ��ȡ��ǰ�ֽ�����ĸ���
    /// </summary>
    /// <returns>�ֽ����鸱��</returns>
    public byte[] GetBytes()
	{
		byte[] array = new byte[Length];
		Array.Copy(Bytes, array, array.Length);
		return array;
	}
    /// <summary>
    /// д�벼��ֵ������ת��Ϊ 0 �� 1 ��д���ֽ�����
    /// </summary>
    /// <param name="value">����</param>
    public void WriteBoolean(bool value)
	{
		WriteByte(value ? ((byte)1) : ((byte)0));
	}
    /// <summary>
    /// д�뵥�ֽڵ�ֵ
    /// </summary>
    /// <param name="value">���ֽ�</param>
    public void WriteByte(byte value)
	{
		EnsureCapacity(_Position + 1);// ȷ�������㹻
        Bytes[_Position++] = value;// д���ֽڲ�����λ��
    }
    /// <summary>
    /// д���ַ���ʹ�������ֽڴ洢 Unicode ֵ
    /// </summary>
    /// <param name="value">���ַ�</param>
    public void WriteChar(char value)
	{
		EnsureCapacity(_Position + 2);
		Bytes[_Position++] = (byte)value;
		Bytes[_Position++] = (byte)((int)value >> 8);
	}
    /// <summary>
    /// д�� 16 λ�з���������2 �ֽڣ�
    /// </summary>
    /// <param name="value">16 λ�з�������</param>
    public void WriteInt16(short value)
	{
		EnsureCapacity(_Position + 2);
		Bytes[_Position++] = (byte)value;
		Bytes[_Position++] = (byte)(value >> 8); // ���ַ���λд���ֽ�����
    }
    /// <summary>
    /// д�� 16 λ�޷���������ʵ�����ǵ��� WriteInt16 ����
    /// </summary>
    /// <param name="value">16 λ�޷�������</param>
    public void WriteUInt16(ushort value)
	{
		WriteInt16((short)value);
	}
    /// <summary>
    /// д�� 32 λ�з���������4 �ֽڣ�
    /// </summary>
    /// <param name="value">32 λ�з�������</param>
    public void WriteInt32(int value)
	{
		EnsureCapacity(_Position + 4);
		Bytes[_Position++] = (byte)value;
		Bytes[_Position++] = (byte)(value >> 8);
		Bytes[_Position++] = (byte)(value >> 16);
		Bytes[_Position++] = (byte)(value >> 24);
	}
    /// <summary>
    /// д�� 32 λ�޷���������ʵ�����ǵ��� WriteInt32 ����
    /// </summary>
    /// <param name="value">32 λ�޷�������</param>
    public void WriteUInt32(uint value)
	{
		WriteInt32((int)value);
	}
    /// <summary>
	/// д�� 64 λ�з���������8 �ֽڣ�
	/// </summary>
	/// <param name="value">64 λ�з�������</param>
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
    /// д�� 64 λ�޷�������
    /// </summary>
    /// <param name="value">64 λ�޷�������</param>
    public void WriteUInt64(ulong value)
	{
		WriteInt64((long)value);
	}
    /// <summary>
    /// д�� 32 λ������
    /// </summary>
    /// <param name="value">�����ȸ�����</param>
    public unsafe void WriteSingle(float value)
	{
		WriteInt32(*(int*)(&value));// ����������λת��Ϊ������д��
    }
    /// <summary>
    /// д�� 64 λ˫���ȸ�����
    /// </summary>
    /// <param name="value">˫���ȸ�����</param>
    public unsafe void WriteDouble(double value)
	{
		WriteInt64(*(long*)(&value));// ��˫���ȸ�������λת��Ϊ������д��
    }
    /// <summary>
    /// д��һ��ѹ���� 32 λ�������䳤���룬������С������
    /// </summary>
    /// <param name="value">ѹ����32 λ����</param>
    public void WritePackedInt32(int value)
	{
		EnsureCapacity(_Position + 5);
		uint num;
		for (num = (uint)value; num >= 128; num >>= 7)
		{
			Bytes[_Position++] = (byte)(num | 0x80u);// �������λ��ָʾ���и����ֽ�
        }
		Bytes[_Position++] = (byte)num;// ���һ���ֽڲ��������λ
    }
    /// <summary>
    /// д��̶����ȵ��ֽ�����
    /// </summary>
    /// <param name="bytes">�ֽ�����</param>
    public void WriteFixedBytes(byte[] bytes)
	{
		if (bytes != null)
		{
			WriteFixedBytes(bytes, 0, bytes.Length);
		}
	}
    /// <summary>
    /// д��̶����ȵ��ֽ����飬ָ����ʼ����������
    /// </summary>
    /// <param name="bytes">�ֽ�����</param>
    /// <param name="start">��ʼ����</param>
    /// <param name="count">����</param>
    public void WriteFixedBytes(byte[] bytes, int start, int count)
	{
		EnsureCapacity(_Position + count);
		Array.Copy(bytes, start, Bytes, _Position, count);
		_Position += count;
	}
    /// <summary>
    /// д��ɱ䳤�ȵ��ֽ�����
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
			WriteByte(0);// �������Ϊ�գ���д��һ�� 0 �ֽ�
        }
	}
    /// <summary>
    /// д��ָ����Χ�Ŀɱ䳤���ֽ�����
    /// </summary>
    /// <param name="bytes">�ֽ�����</param>
    /// <param name="start">��ʼ����</param>
    /// <param name="count">����</param>
    public void WriteBytes(byte[] bytes, int start, int count)
	{
		EnsureCapacity(_Position + 5 + count);
		WritePackedInt32(count);
		Array.Copy(bytes, start, Bytes, _Position, count);
		_Position += count;
	}
    /// <summary>
    /// д���ַ�������д���ַ������ֽڳ��ȣ�Ȼ��д�� UTF-8 ������ֽ�����
    /// </summary>
    /// <param name="value">�ַ���</param>
    public void WriteString(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			int byteCount = Encoding.UTF8.GetByteCount(value);
			EnsureCapacity(_Position + 5 + byteCount);
			WritePackedInt32(byteCount);// ��д���ֽڳ���
            _Position += Encoding.UTF8.GetBytes(value, 0, value.Length, Bytes, _Position);// д���ַ�������
        }
		else
		{
			WriteByte(0);// ����ַ���Ϊ�գ���д��һ�� 0 �ֽ�
        }
	}
    /// <summary>
    /// д�� IPEndPoint ���ͣ������ַ�Ͷ˿ںţ�
    /// </summary>
    /// <param name="address"></param>
    public void WriteIPEndPoint(IPEndPoint address)
	{
		WriteBytes(address.Address.GetAddressBytes());// д�� IP ��ַ
        WriteInt16((short)address.Port);// д��˿ں�
    }
    /// <summary>
    /// // ȷ�������������㹻�����������������չ������
    /// </summary>
    /// <param name="capacity">������</param>
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
			Bytes = array;// ��չ������
        }
	}
}
