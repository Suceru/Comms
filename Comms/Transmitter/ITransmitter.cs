using System;
using System.Net;

namespace Comms;
/// <summary>
/// 表示一个数据传输器的接口
/// 
/// 该接口定义了发送和接收数据包的基本功能，允许实现类处理网络通信。
/// </summary>
public interface ITransmitter : IDisposable
{
    /// <summary>
    /// 获取允许发送的最大数据包大小（以字节为单位）
    /// 
    /// 该属性表示在网络上传输时，单个数据包的最大字节数。
    /// 实现类应根据其网络协议和配置来设置此值。
    /// </summary>
    int MaxPacketSize { get; }
    /// <summary>
    /// 获取当前传输器的网络地址
    /// 
    /// 该属性表示当前传输器的 IP 地址和端口，通常用于指定数据的发送和接收目标。
    /// </summary>
    IPEndPoint Address { get; }
    /// <summary>
    /// 错误事件
    /// 
    /// 当传输过程中发生异常时触发此事件，提供错误信息以供处理。
    /// </summary>
    event Action<Exception> Error;
    /// <summary>
    /// 调试事件
    /// 
    /// 用于在传输过程中提供调试信息，方便开发人员进行问题排查。
    /// </summary>
    event Action<string> Debug;
    /// <summary>
    /// 数据包接收事件
    /// 
    /// 当接收到新的数据包时触发此事件，提供接收到的包数据，以供处理。
    /// </summary>
    event Action<Packet> PacketReceived;
    /// <summary>
    /// 发送数据包的方法
    /// 
    /// 该方法用于将数据包发送到指定的目的地。
    /// 实现类应处理数据包的序列化和网络传输。
    /// </summary>
    /// <param name="packet">要发送的数据包</param>
    void SendPacket(Packet packet);
}
