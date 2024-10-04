using System.Collections.Generic;

namespace Comms.Drt;
/// <summary>
/// 表示在游戏中发生不同步（Desync）时的相关数据。
/// 该类通常用于检测或处理客户端之间游戏状态的不同步问题。
/// </summary>
public class DesyncData
{
    /// <summary>
    /// 游戏的唯一标识符 (GameID)。
    /// 用于区分不同的游戏实例。
    /// </summary>
    public int GameID;
    /// <summary>
    /// 当前不同步发生时的游戏步数 (Step)。
    /// 用于表示游戏的进度，通常是游戏循环中的某个时间点。
    /// </summary>
    public int Step;
    /// <summary>
    /// 参与游戏的客户端数量 (ClientsCount)。
    /// 用于表示当前参与游戏的客户端总数。
    /// </summary>
    public int ClientsCount;
    /// <summary>
    /// 保存先前游戏状态的字典 (PriorStates)。
    /// 键是客户端ID，值是该客户端在当前步之前的游戏状态。
    /// 该字典用于在检测到不同步问题时进行状态回溯和比较。
    /// </summary>
    public Dictionary<int, byte[]> PriorStates = new Dictionary<int, byte[]>();
    /// <summary>
    /// 保存当前游戏状态的字典 (States)。
    /// 键是客户端ID，值是该客户端在当前步的游戏状态。
    /// 该字典用于存储每个客户端在当前步的游戏状态，以进行同步验证。
    /// </summary>
    public Dictionary<int, byte[]> States = new Dictionary<int, byte[]>();
}
