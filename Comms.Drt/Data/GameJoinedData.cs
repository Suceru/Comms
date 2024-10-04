namespace Comms.Drt;
/// <summary>
/// 表示玩家成功加入游戏时的状态数据结构。
/// 当玩家加入游戏后，服务器或其他客户端会发送当前的游戏步数和状态数据，以便新加入的玩家同步到当前的游戏进度。
/// </summary>
public struct GameJoinedData
{
    /// <summary>
    /// 表示当前游戏的步数（Step）。
    /// Step 是游戏进程中的标识符，用于标记游戏进行的时刻或阶段。
    /// </summary>
    public int Step;
    /// <summary>
    /// 表示游戏的状态数据（StateBytes）。
    /// 这是一个字节数组，包含了游戏当前状态的序列化数据。新加入的玩家会通过该数据同步游戏的当前状态。
    /// </summary>
    public byte[] StateBytes;
}
