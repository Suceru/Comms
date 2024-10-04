namespace Comms;
/// <summary>
/// DiagnosticStats 类用于收集和存储网络通信过程中与数据包和字节相关的统计信息。
/// 该类包含发送和接收的数据包数、字节数，并提供了一个方法将这些统计数据转换为字符串格式。
/// </summary>
public class DiagnosticStats
{
    /// <summary>
    /// 记录接收到的数据包数量
    /// </summary>
    public long PacketsReceived;
    /// <summary>
    /// 记录发送出去的数据包数量
    /// </summary>
    public long PacketsSent;
    /// <summary>
    /// 记录发送出去的总字节数
    /// </summary>
	public long BytesSent;
    /// <summary>
    /// 记录接收到的总字节数
    /// </summary>
	public long BytesReceived;
    /// <summary>
    /// 重写 ToString 方法，返回包含统计信息的字符串。
    /// 使用了格式化输出，将字节数和数据包数量以标准化的千位分隔符表示 (N0)。
    /// </summary>
    /// <returns>字符串格式的统计信息，包含发送的字节和数据包数量，以及接收的字节和数据包数量。</returns>
	public override string ToString()
	{
		return $"Sent {BytesSent:N0} bytes ({PacketsSent:N0} packets), received {BytesReceived:N0} bytes ({PacketsReceived:N0} packets)";
	}
}
