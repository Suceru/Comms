using System.Net;

namespace Comms;
/// <summary>
/// PeerData 类表示与服务器连接的客户端信息
/// </summary>
public class PeerData
{
    // 最近一次从客户端收到 KeepAlive（心跳）消息的时间
    internal double LastKeepAliveReceiveTime;
    // 下次需要向客户端发送 KeepAlive（心跳）消息的时间
    internal double NextKeepAliveSendTime;
    /// <summary>
    ///所属的 Peer 对象（服务器端的 Peer 实例）
    /// </summary>
    public Peer Owner { get; internal set; }
    /// <summary>
    /// 客户端的 IP 地址和端口
    /// </summary>
    public IPEndPoint Address { get; internal set; }
    /// <summary>
    /// 从客户端接收到的 Ping 值，表示延迟（以毫秒为单位）
    /// </summary>
	public float Ping { get; internal set; }
    /// <summary>
    /// 一个可以用来存储与这个 Peer 关联的任意对象。此字段可以用于自定义数据。
    /// </summary>
	public object Tag { get; set; }
    /// <summary>
    /// 构造函数，初始化 PeerData 实例
    /// </summary>
    /// <param name="owner">当前 PeerData 所属的服务器端 Peer 对象</param>
    /// <param name="address">客户端的 IP 地址和端口</param>
	internal PeerData(Peer owner, IPEndPoint address)
    { // 设定 PeerData 所属的 Peer 实例（表示服务器端）
        Owner = owner;
        // 设定 PeerData 对应的客户端地址（IP + 端口）
        Address = address;
        // 初始化最近一次收到 KeepAlive 消息的时间为当前时间
        LastKeepAliveReceiveTime = Comm.GetTime();
        // 计算下次发送 KeepAlive 消息的时间为当前时间加上 KeepAlive 的间隔时间
        NextKeepAliveSendTime = LastKeepAliveReceiveTime + (double)owner.Settings.KeepAlivePeriod;
	}
}
