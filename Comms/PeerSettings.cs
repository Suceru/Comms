namespace Comms;
/// <summary>
///PeerSettings 类用于配置与网络通信中节点（Peer）相关的设置。
///包括连接和断开通知、超时时间、保活周期等参数。
/// </summary>
public class PeerSettings
{
    /// <summary>
    /// 控制是否在网络节点（Peer）连接或断开时发送通知。
    /// 默认为 true，表示发送通知。
    /// </summary>
    public bool SendPeerConnectDisconnectNotifications = true;
    /// <summary>
    /// 定义连接超时时间（以秒为单位）。
    /// 如果在该时间内未能建立连接，则会触发连接超时逻辑。
    /// 默认为 8 秒。
    /// </summary>
    public float ConnectTimeOut = 8f;
    /// <summary>
    /// 定义保活消息的发送周期（以秒为单位）。
    /// 该时间间隔决定服务器或客户端多久发送一次保活消息，确保连接的持续性。
    /// 默认为 10 秒。
    /// </summary>
    public float KeepAlivePeriod = 10f;
    /// <summary>
    /// 定义保活消息的重发周期（以秒为单位）。
    /// 如果在该时间段内未收到保活消息的响应，则会重发保活消息。
    /// 默认为 1 秒。
    /// </summary>
    public float KeepAliveResendPeriod = 1f;
    /// <summary>
    /// 定义连接丢失的检测周期（以秒为单位）。
    /// 如果在该时间内没有收到任何消息，连接将被认为已丢失。
    /// 默认为 30 秒。
    /// </summary>
    public float ConnectionLostPeriod = 30f;
}
