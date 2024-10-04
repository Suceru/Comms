using System.Runtime.InteropServices;

namespace Comms.Drt;
/// <summary>
/// 表示游戏状态请求的数据结构。
/// 用于当客户端或服务器希望获取当前游戏状态时，向游戏状态管理器发送的请求结构体。
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct GameStateRequestData
{
}
