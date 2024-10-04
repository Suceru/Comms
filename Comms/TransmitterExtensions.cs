namespace Comms;
/// <summary>
/// TransmitterExtensions 类为 ITransmitter 接口提供扩展方法，方便操作传输器对象。
/// </summary>
public static class TransmitterExtensions
{
    /// <summary>
    /// RootTransmitter 扩展方法用于获取传输器的最底层（根）的传输器对象。
	/// 这个方法适用于实现了 IWrapperTransmitter 接口的传输器，该接口通常用于包装另一个传输器。
    /// </summary>
    /// <param name="transmitter">传输器</param>
    /// <returns></returns>
    public static ITransmitter RootTransmitter(this ITransmitter transmitter)
	{
        // 通过循环不断获取包装器传输器的基础传输器，直到找到最底层的传输器为止。
        while (transmitter is IWrapperTransmitter wrapperTransmitter)
        {
            // 将当前传输器设置为包装器的基础传输器
            transmitter = wrapperTransmitter.BaseTransmitter;
		}
        // 返回最底层的传输器
        return transmitter;
	}
}
