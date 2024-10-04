using System.Net;

namespace Comms;
/// <summary>
/// Packet 结构体用于封装网络数据包信息，包含数据包的目标或来源地址以及数据内容。
/// </summary>
public struct Packet
{
    /// <summary>
    /// Address 字段：表示数据包的源地址或目的地址。
	/// 这是一个 IPEndPoint 对象，封装了 IP 地址和端口号，用于网络通信。
    /// </summary>
    public IPEndPoint Address;
    /// <summary>
    /// Bytes 字段：存储数据包的实际数据。
    /// 这是一个字节数组，包含了发送或接收的原始数据内容。
    /// </summary>
    public byte[] Bytes;
    /// <summary>
    /// Packet 构造函数：用于初始化 Packet 实例。
    /// </summary>
    /// <param name="address">数据包的源地址或目的地址（IPEndPoint 类型）。</param>
    /// <param name="bytes">包含数据包内容的字节数组。</param>
	public Packet(IPEndPoint address, byte[] bytes)
	{
        // 将传入的 address 参数赋值给 Address 字段。
        Address = address;
        // 将传入的 bytes 参数赋值给 Bytes 字段。
        Bytes = bytes;
	}
}
