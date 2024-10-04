using System.Net;

namespace Comms.Drt;
// MalformedMessageException 类是一个自定义异常，继承自 ProtocolViolationException。
// 它用于表示在处理消息时，发现了不符合预期格式或协议的消息。
internal class MalformedMessageException : ProtocolViolationException
{
    // SenderAddress 保存了发送该错误消息的发送者的 IP 地址和端口。
    // 它可以用于追踪哪个远程端点（IP 地址和端口）发送了无效的消息。
    public IPEndPoint SenderAddress;

    // 构造函数，用于初始化 MalformedMessageException 的实例。
    // message 是异常的描述性消息，senderAddress 是发送该消息的远程端点。
    public MalformedMessageException(string message, IPEndPoint senderAddress)
		: base(message)// 调用基类 ProtocolViolationException 的构造函数，并传递异常信息。
    {
        // 保存远程发送者的 IP 地址和端口到 SenderAddress 属性。
        SenderAddress = senderAddress;
	}
}
