using System.Net;

namespace Comms;
/// <summary>
/// Packet �ṹ�����ڷ�װ�������ݰ���Ϣ���������ݰ���Ŀ�����Դ��ַ�Լ��������ݡ�
/// </summary>
public struct Packet
{
    /// <summary>
    /// Address �ֶΣ���ʾ���ݰ���Դ��ַ��Ŀ�ĵ�ַ��
	/// ����һ�� IPEndPoint ���󣬷�װ�� IP ��ַ�Ͷ˿ںţ���������ͨ�š�
    /// </summary>
    public IPEndPoint Address;
    /// <summary>
    /// Bytes �ֶΣ��洢���ݰ���ʵ�����ݡ�
    /// ����һ���ֽ����飬�����˷��ͻ���յ�ԭʼ�������ݡ�
    /// </summary>
    public byte[] Bytes;
    /// <summary>
    /// Packet ���캯�������ڳ�ʼ�� Packet ʵ����
    /// </summary>
    /// <param name="address">���ݰ���Դ��ַ��Ŀ�ĵ�ַ��IPEndPoint ���ͣ���</param>
    /// <param name="bytes">�������ݰ����ݵ��ֽ����顣</param>
	public Packet(IPEndPoint address, byte[] bytes)
	{
        // ������� address ������ֵ�� Address �ֶΡ�
        Address = address;
        // ������� bytes ������ֵ�� Bytes �ֶΡ�
        Bytes = bytes;
	}
}
