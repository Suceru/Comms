namespace Comms.Drt;
/// <summary>
/// 表示在游戏发生不同步（Desync）时，客户端或服务器请求特定游戏状态的结构体。
/// 该结构主要用于请求与游戏某个步骤相关的状态数据，以便进行不同步的校验或修复。
/// </summary>
public struct GameDesyncStateRequestData
{
    /// <summary>
    /// 表示请求的游戏步骤（Step）。
    /// Step 是游戏中的一个特定时刻，用于标识在该时刻的游戏状态。
    /// </summary>
    public int Step;
}
