namespace Comms;
/// <summary>
/// PeerPacket �ṹ���ڷ�װ��������ͻ���֮�����Ϣ����
/// </summary>
public struct PeerPacket
{
    /// <summary>
    /// ��ʾ�����Ϣ�����Ŀͻ��˵�������ݣ��� IP ��ַ���ӳٵȣ�
    /// </summary>
    public PeerData PeerData;
    /// <summary>
    /// ��Ϣ��ʵ�����ݣ����ֽ�������ʽ��ʾ
    /// </summary>
    public byte[] Bytes;
    /// <summary>
    /// ���캯�������ڳ�ʼ�� PeerPacket ʵ��
    /// </summary>
    /// <param name="peerData">���ͻ������Ϣ�Ŀͻ��������Ϣ</param>
    /// <param name="bytes">��Ϣ���ֽ�����</param>
	public PeerPacket(PeerData peerData, byte[] bytes)
    {// ��ʼ�� PeerData ���ԣ���ʾ��Ϣ��Դ��Ŀ�ĵصĿͻ�����Ϣ
        PeerData = peerData;
        // ��ʼ�� Bytes ���ԣ���ʾ�������Ϣ����
        Bytes = bytes;
	}
}
