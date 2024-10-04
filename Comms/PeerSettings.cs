namespace Comms;
/// <summary>
///PeerSettings ����������������ͨ���нڵ㣨Peer����ص����á�
///�������ӺͶϿ�֪ͨ����ʱʱ�䡢�������ڵȲ�����
/// </summary>
public class PeerSettings
{
    /// <summary>
    /// �����Ƿ�������ڵ㣨Peer�����ӻ�Ͽ�ʱ����֪ͨ��
    /// Ĭ��Ϊ true����ʾ����֪ͨ��
    /// </summary>
    public bool SendPeerConnectDisconnectNotifications = true;
    /// <summary>
    /// �������ӳ�ʱʱ�䣨����Ϊ��λ����
    /// ����ڸ�ʱ����δ�ܽ������ӣ���ᴥ�����ӳ�ʱ�߼���
    /// Ĭ��Ϊ 8 �롣
    /// </summary>
    public float ConnectTimeOut = 8f;
    /// <summary>
    /// ���屣����Ϣ�ķ������ڣ�����Ϊ��λ����
    /// ��ʱ����������������ͻ��˶�÷���һ�α�����Ϣ��ȷ�����ӵĳ����ԡ�
    /// Ĭ��Ϊ 10 �롣
    /// </summary>
    public float KeepAlivePeriod = 10f;
    /// <summary>
    /// ���屣����Ϣ���ط����ڣ�����Ϊ��λ����
    /// ����ڸ�ʱ�����δ�յ�������Ϣ����Ӧ������ط�������Ϣ��
    /// Ĭ��Ϊ 1 �롣
    /// </summary>
    public float KeepAliveResendPeriod = 1f;
    /// <summary>
    /// �������Ӷ�ʧ�ļ�����ڣ�����Ϊ��λ����
    /// ����ڸ�ʱ����û���յ��κ���Ϣ�����ӽ�����Ϊ�Ѷ�ʧ��
    /// Ĭ��Ϊ 30 �롣
    /// </summary>
    public float ConnectionLostPeriod = 30f;
}
