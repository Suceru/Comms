using System;
using System.Threading;

namespace Comms;
/// <summary>
/// NetworkSimulatorStats 类用于收集和管理网络模拟器中的统计数据。
/// 它继承自 DiagnosticStats 类，并扩展了网络活动的详细统计功能。
/// </summary>
public class NetworkSimulatorStats : DiagnosticStats
{
    // LastActivityTicks 字段：表示最后一次网络活动的时间（以系统滴答计时）。
    // 初始值为 -1，表示尚未发生任何活动。
    internal int LastActivityTicks = -1;
    /// <summary>
    /// PacketsDropped 字段：表示模拟器中已丢弃的数据包数量。
    /// </summary>
    public long PacketsDropped;
    /// <summary>
    /// ToString 方法：返回当前网络统计信息的字符串表示。
	/// 统计内容包括已发送和接收的字节数、数据包数，以及丢包数。
    /// </summary>
    /// <returns>字符串</returns>
    public override string ToString()
	{
        // N0 格式化字符串用于以千位分隔符格式化数字
        return $"Sent: {BytesSent:N0} bytes ({PacketsSent:N0} packets), received {BytesReceived:N0} bytes ({PacketsReceived:N0} packets), dropped {PacketsDropped:N0} packets";
	}
    /// <summary>
    /// GetIdleTime 方法：计算并返回网络自上次活动后的空闲时间（单位：秒）。
    /// 如果 LastActivityTicks 尚未被设置，则返回 0 表示没有空闲时间。
    /// </summary>
    /// <returns>单精度浮点数</returns>
    public float GetIdleTime()
	{
        // 如果没有记录到任何活动，则返回 0 秒的空闲时间。
        if (LastActivityTicks < 0)
		{
			return 0f;
		}
        // 使用系统的 Environment.TickCount 计算空闲时间，返回值为秒数。
        return (float)((Environment.TickCount & 0x7FFFFFFF) - LastActivityTicks) / 1000f;
	}
    /// <summary>
    /// WaitUntilIdle 方法：阻塞当前线程直到网络模拟器达到指定的空闲时间。
    /// </summary>
    /// <param name="idleTime">指定等待的空闲时间（单位：秒）</param>
	public void WaitUntilIdle(float idleTime)
	{
        // 循环等待，直到 GetIdleTime() 返回的值大于指定的 idleTime。
        while (GetIdleTime() <= idleTime)
		{
            // 每次循环时，线程暂停 10 毫秒以避免高 CPU 占用。
            Thread.Sleep(10);
		}
	}
}
