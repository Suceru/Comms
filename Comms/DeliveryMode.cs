namespace Comms;
/// <summary>
///DeliveryMode 枚举用于指定数据包在网络通信中的传递模式。
///不同的模式提供了不同的可靠性和顺序保障，以满足不同的网络传输需求。
/// </summary>
public enum DeliveryMode
{
    /// <summary>
    /// Raw 模式表示原始传输，不提供任何可靠性保障，数据包可能丢失或乱序。
    /// </summary>
    Raw,
    /// <summary>
    /// Unreliable 模式表示不可靠传输，数据包可能丢失，但无需保证数据包按发送顺序到达。
    /// </summary>
	Unreliable,
    /// <summary>
    /// UnreliableSequenced 模式表示不可靠的有序传输，数据包可能丢失，但接收到的数据包会按照顺序排列，丢失的数据包将被忽略。
    /// </summary>
	UnreliableSequenced,
    /// <summary>
    /// Reliable 模式表示可靠传输，保证数据包将被成功接收，但无需按顺序排列。
    /// </summary>
	Reliable,
    /// <summary>
    /// ReliableSequenced 模式表示可靠且有序传输，数据包不仅保证成功接收，还保证按发送顺序排列。
    /// </summary>
	ReliableSequenced
}
