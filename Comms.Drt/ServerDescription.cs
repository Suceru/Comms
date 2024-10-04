using System.Net;

namespace Comms.Drt;
/// <summary>
/// ServerDescription 类用于存储与服务器相关的描述信息，主要用于服务发现、网络游戏或分布式通信系统中的服务器信息。
/// </summary>
public class ServerDescription
{
    /// <summary>
    /// 服务器的网络地址（IP 地址和端口号）。
    /// </summary>
    public IPEndPoint Address;
    /// <summary>
    /// 指示服务器是否为本地服务器。如果为 true，表示该服务器位于本地网络中。
    /// </summary>
    public bool IsLocal;
    /// <summary>
    /// 服务器被发现的时间戳（通常是自某个参考点以来的秒数）。
    /// </summary>
	public double DiscoveryTime;
    /// <summary>
    /// 服务器的延迟（以毫秒为单位）。Ping 值越低，说明网络延迟越小。
    /// </summary>
	public float Ping;
    /// <summary>
    /// 服务器的名称，用于标识服务器的友好名称。
    /// </summary>
	public string Name;
    /// <summary>
    /// 服务器的优先级值，优先级越高的服务器将被优先考虑使用。
    /// </summary>
	public int Priority;
    /// <summary>
    /// 该服务器上所提供的游戏描述数组。每个 GameDescription 描述了一个服务器上提供的游戏。
    /// </summary>
	public GameDescription[] GameDescriptions;
}
