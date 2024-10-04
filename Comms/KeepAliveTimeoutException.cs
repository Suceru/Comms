using System.Net;

namespace Comms;
/// <summary>
/// KeepAliveTimeoutException 类继承自 ProtocolViolationException。
/// 该类表示在网络通信中，由于 "Keep Alive" 机制超时而引发的协议违规异常。
/// "Keep Alive" 是一种用于维持网络连接活动的机制，通常用于检测长时间空闲的网络连接
/// </summary>
public class KeepAliveTimeoutException : ProtocolViolationException
{
    /// <summary>
    /// 构造函数：接收一条消息字符串，表示超时时的错误信息。
    /// </summary>
    /// <param name="message">描述异常的详细信息的字符串。</param>
    public KeepAliveTimeoutException(string message)
		: base(message)// 调用基类 ProtocolViolationException 的构造函数，传递异常消息。
    {
	}
}
