using System;
using System.Net;

namespace Comms;
/// <summary>
/// ��ʾһ�����ݴ������Ľӿ�
/// 
/// �ýӿڶ����˷��ͺͽ������ݰ��Ļ������ܣ�����ʵ���ദ������ͨ�š�
/// </summary>
public interface ITransmitter : IDisposable
{
    /// <summary>
    /// ��ȡ�����͵�������ݰ���С�����ֽ�Ϊ��λ��
    /// 
    /// �����Ա�ʾ�������ϴ���ʱ���������ݰ�������ֽ�����
    /// ʵ����Ӧ����������Э������������ô�ֵ��
    /// </summary>
    int MaxPacketSize { get; }
    /// <summary>
    /// ��ȡ��ǰ�������������ַ
    /// 
    /// �����Ա�ʾ��ǰ�������� IP ��ַ�Ͷ˿ڣ�ͨ������ָ�����ݵķ��ͺͽ���Ŀ�ꡣ
    /// </summary>
    IPEndPoint Address { get; }
    /// <summary>
    /// �����¼�
    /// 
    /// ����������з����쳣ʱ�������¼����ṩ������Ϣ�Թ�����
    /// </summary>
    event Action<Exception> Error;
    /// <summary>
    /// �����¼�
    /// 
    /// �����ڴ���������ṩ������Ϣ�����㿪����Ա���������Ų顣
    /// </summary>
    event Action<string> Debug;
    /// <summary>
    /// ���ݰ������¼�
    /// 
    /// �����յ��µ����ݰ�ʱ�������¼����ṩ���յ��İ����ݣ��Թ�����
    /// </summary>
    event Action<Packet> PacketReceived;
    /// <summary>
    /// �������ݰ��ķ���
    /// 
    /// �÷������ڽ����ݰ����͵�ָ����Ŀ�ĵء�
    /// ʵ����Ӧ�������ݰ������л������紫�䡣
    /// </summary>
    /// <param name="packet">Ҫ���͵����ݰ�</param>
    void SendPacket(Packet packet);
}
