using System.Collections.Generic;
using System.Net;

namespace Comms.Drt;
/// <summary>
/// ServerClient ������������ͨ�ŵĿͻ��ˣ��洢�����ض��ͻ�����ص���Ϣ����ͻ��� ID�����ơ������ַ�ȡ�
/// </summary>
public class ServerClient
{
    //����ͻ��˷��͸����������������ݵ��ֽ������б���Щ����ͨ������Ϸ��Ӧ���е����루������������
    internal List<byte[]> InputsBytes = new List<byte[]>();
    // ��ÿͻ�����ص� PeerData ���󣬰����ͻ��˵�����������Ϣ�����״̬��
    internal PeerData PeerData { get; }
    /// <summary>
    /// �ͻ��������� ServerGame ���󣬱�ʾ�ÿͻ��˲������Ϸ��
    /// </summary>
    public ServerGame ServerGame { get; }
    /// <summary>
    /// �ͻ��˵�Ψһ��ʶ����ÿ���ͻ����ڷ������϶���һ��Ψһ�� ID��
    /// </summary>
    public int ClientID { get; }
    /// <summary>
    /// �ͻ��˵����ƣ����ڱ�ʶ�ͻ��˵���ʾ���ƻ�������ơ�
    /// </summary>
    public string ClientName { get; }
    /// <summary>
    /// �� PeerData ��ȡ�ͻ��˵������ַ��IP ��ַ�Ͷ˿ڣ���
    /// </summary>
    public IPEndPoint Address => PeerData.Address;
    // ���캯������ʼ�� ServerClient ����Ϊ�ͻ��˷���Ψһ ID �����ƣ����� PeerData �� ServerClient ������
    internal ServerClient(ServerGame serverGame, PeerData peerData, int clientID, string clientName)
	{
        // ��� PeerData �� Tag �����Ƿ��Ѿ��� ServerClient ʵ��������У��׳�Э��Υ���쳣��
        if (peerData.Tag != null)
		{
			throw new ProtocolViolationException("PeerData already has a ServerClient assigned.");
		}
        // ��ʼ�� ServerGame��PeerData �Ϳͻ��������Ϣ���� PeerData �� Tag ��������Ϊ��ǰ ServerClient ʵ����
        ServerGame = serverGame;
		PeerData = peerData;
		peerData.Tag = this;
		ClientID = clientID;
		ClientName = clientName;
	}
    // �� PeerData �л�ȡ ServerClient ʵ����PeerData �� Tag ���Դ洢�˶�Ӧ�� ServerClient��
    internal static ServerClient FromPeerData(PeerData peerData)
	{
		return (ServerClient)peerData.Tag;
	}
}
