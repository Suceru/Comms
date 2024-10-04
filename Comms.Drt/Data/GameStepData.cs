using System.Net;

namespace Comms.Drt;
/// <summary>
/// 表示多人游戏中的游戏步骤数据，用于同步玩家加入、离开以及输入等信息。
/// </summary>
public struct GameStepData
{
    /// <summary>
    /// 表示玩家加入游戏的数据。
    /// </summary>
    public struct JoinData
	{
        /// <summary>
        /// 客户端的唯一标识符。
        /// </summary>
        public int ClientID;
        /// <summary>
        /// 客户端的网络地址。
        /// </summary>
        public IPEndPoint Address;
        /// <summary>
        /// 加入请求的原始字节数据。
        /// 通常包含了玩家的身份验证、请求参数等信息。
        /// </summary>
        public byte[] JoinRequestBytes;
	}
    /// <summary>
    /// 表示玩家离开游戏的数据。
    /// </summary>
    public struct LeaveData
	{
        /// <summary>
        /// 离开游戏的客户端的唯一标识符。
        /// </summary>
        public int ClientID;
	}
    /// <summary>
    /// 表示玩家输入数据的信息。
    /// </summary>
    public struct InputData
	{
        /// <summary>
        /// 发送输入数据的客户端的唯一标识符。
        /// </summary>
        public int ClientID;
        /// <summary>
        /// 输入的原始字节数据。
        /// 包含玩家的操作输入，如移动、攻击等动作的编码。
        /// </summary>
        public byte[] InputBytes;
	}
    /// <summary>
    /// 当前游戏步骤的编号。
    /// 每一步对应游戏中的一个状态更新周期。
    /// </summary>
    public int Step;
    /// <summary>
    /// 本步骤内所有玩家的加入数据集合。
    /// </summary>
    public JoinData[] Joins;
    /// <summary>
    /// 本步骤内所有玩家的离开数据集合。
    /// </summary>
    public LeaveData[] Leaves;
    /// <summary>
    /// 本步骤内所有玩家的输入数据集合。
    /// </summary>
    public InputData[] Inputs;
}
