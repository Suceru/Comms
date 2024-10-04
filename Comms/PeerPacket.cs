namespace Comms;
/// <summary>
/// PeerPacket 结构用于封装服务器与客户端之间的消息数据
/// </summary>
public struct PeerPacket
{
    /// <summary>
    /// 表示与该消息关联的客户端的相关数据（如 IP 地址、延迟等）
    /// </summary>
    public PeerData PeerData;
    /// <summary>
    /// 消息的实际内容，以字节数组形式表示
    /// </summary>
    public byte[] Bytes;
    /// <summary>
    /// 构造函数，用于初始化 PeerPacket 实例
    /// </summary>
    /// <param name="peerData">发送或接收消息的客户端相关信息</param>
    /// <param name="bytes">消息的字节数据</param>
	public PeerPacket(PeerData peerData, byte[] bytes)
    {// 初始化 PeerData 属性，表示消息来源或目的地的客户端信息
        PeerData = peerData;
        // 初始化 Bytes 属性，表示传输的消息内容
        Bytes = bytes;
	}
}
