using System.Net;

namespace Comms;
/// <summary>
/// PeerData ���ʾ����������ӵĿͻ�����Ϣ
/// </summary>
public class PeerData
{
    // ���һ�δӿͻ����յ� KeepAlive����������Ϣ��ʱ��
    internal double LastKeepAliveReceiveTime;
    // �´���Ҫ��ͻ��˷��� KeepAlive����������Ϣ��ʱ��
    internal double NextKeepAliveSendTime;
    /// <summary>
    ///������ Peer ���󣨷������˵� Peer ʵ����
    /// </summary>
    public Peer Owner { get; internal set; }
    /// <summary>
    /// �ͻ��˵� IP ��ַ�Ͷ˿�
    /// </summary>
    public IPEndPoint Address { get; internal set; }
    /// <summary>
    /// �ӿͻ��˽��յ��� Ping ֵ����ʾ�ӳ٣��Ժ���Ϊ��λ��
    /// </summary>
	public float Ping { get; internal set; }
    /// <summary>
    /// һ�����������洢����� Peer ������������󡣴��ֶο��������Զ������ݡ�
    /// </summary>
	public object Tag { get; set; }
    /// <summary>
    /// ���캯������ʼ�� PeerData ʵ��
    /// </summary>
    /// <param name="owner">��ǰ PeerData �����ķ������� Peer ����</param>
    /// <param name="address">�ͻ��˵� IP ��ַ�Ͷ˿�</param>
	internal PeerData(Peer owner, IPEndPoint address)
    { // �趨 PeerData ������ Peer ʵ������ʾ�������ˣ�
        Owner = owner;
        // �趨 PeerData ��Ӧ�Ŀͻ��˵�ַ��IP + �˿ڣ�
        Address = address;
        // ��ʼ�����һ���յ� KeepAlive ��Ϣ��ʱ��Ϊ��ǰʱ��
        LastKeepAliveReceiveTime = Comm.GetTime();
        // �����´η��� KeepAlive ��Ϣ��ʱ��Ϊ��ǰʱ����� KeepAlive �ļ��ʱ��
        NextKeepAliveSendTime = LastKeepAliveReceiveTime + (double)owner.Settings.KeepAlivePeriod;
	}
}
