namespace Comms.Drt;
/// <summary>
///DesyncDetectionMode 是一个枚举，用于指定不同的不同步检测模式。
///在网络通信或游戏同步系统中，不同步可能会导致客户端和服务器的状态不一致。
///这个枚举提供了几种模式，决定是否以及如何检测不同步问题。
/// </summary>
public enum DesyncDetectionMode
{
    /// <summary>
    /// None 表示不进行任何不同步检测。
    /// 在这种模式下，系统不会检查客户端和服务器之间的状态是否不同步。
    /// </summary>
    None,
    /// <summary>
    ///Detect 表示进行不同步检测，但不具体定位不同步发生的地方。
    ///  系统只会检查是否存在不同步问题，但不会提供具体的错误信息或细节。
    /// </summary>
	Detect,
    /// <summary>
    /// Locate 表示不仅检测不同步，还会尝试定位不同步发生的具体位置或原因。
    /// 在这种模式下，系统会更加详细地分析不同步问题，并提供更多的调试信息。
    /// </summary>
	Locate
}
