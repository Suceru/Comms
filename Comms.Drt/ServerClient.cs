using System.Collections.Generic;
using System.Net;

namespace Comms.Drt;
/// <summary>
/// ServerClient 类代表与服务器通信的客户端，存储了与特定客户端相关的信息，如客户端 ID、名称、网络地址等。
/// </summary>
public class ServerClient
{
    //保存客户端发送给服务器的输入数据的字节数组列表。这些数据通常是游戏或应用中的输入（例如控制命令）。
    internal List<byte[]> InputsBytes = new List<byte[]>();
    // 与该客户端相关的 PeerData 对象，包含客户端的网络连接信息和相关状态。
    internal PeerData PeerData { get; }
    /// <summary>
    /// 客户端所属的 ServerGame 对象，表示该客户端参与的游戏。
    /// </summary>
    public ServerGame ServerGame { get; }
    /// <summary>
    /// 客户端的唯一标识符，每个客户端在服务器上都有一个唯一的 ID。
    /// </summary>
    public int ClientID { get; }
    /// <summary>
    /// 客户端的名称，用于标识客户端的显示名称或玩家名称。
    /// </summary>
    public string ClientName { get; }
    /// <summary>
    /// 过 PeerData 获取客户端的网络地址（IP 地址和端口）。
    /// </summary>
    public IPEndPoint Address => PeerData.Address;
    // 构造函数，初始化 ServerClient 对象。为客户端分配唯一 ID 和名称，并将 PeerData 与 ServerClient 关联。
    internal ServerClient(ServerGame serverGame, PeerData peerData, int clientID, string clientName)
	{
        // 检查 PeerData 的 Tag 属性是否已经有 ServerClient 实例。如果有，抛出协议违规异常。
        if (peerData.Tag != null)
		{
			throw new ProtocolViolationException("PeerData already has a ServerClient assigned.");
		}
        // 初始化 ServerGame、PeerData 和客户端相关信息。将 PeerData 的 Tag 属性设置为当前 ServerClient 实例。
        ServerGame = serverGame;
		PeerData = peerData;
		peerData.Tag = this;
		ClientID = clientID;
		ClientName = clientName;
	}
    // 从 PeerData 中获取 ServerClient 实例。PeerData 的 Tag 属性存储了对应的 ServerClient。
    internal static ServerClient FromPeerData(PeerData peerData)
	{
		return (ServerClient)peerData.Tag;
	}
}
