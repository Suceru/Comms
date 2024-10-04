using System.Net;

namespace Comms.Drt;
/// <summary>
/// 表示游戏创建事件的数据结构。
/// 当一个新游戏被创建时，使用该结构来传递游戏创建者的地址信息。
/// </summary>
public struct GameCreatedData
{
    /// <summary>
    /// 表示游戏创建者的网络终结点地址。
    /// 这是一个包含IP地址和端口号的 IPEndPoint 实例。
    /// </summary>
    public IPEndPoint CreatorAddress;
}
