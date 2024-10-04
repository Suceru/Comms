using System.Runtime.InteropServices;

namespace Comms.Drt;
/// <summary>
/// 表示断开连接事件的数据结构。
/// 此结构通常用于网络通信中，当客户端或服务器断开连接时触发该事件。
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct DisconnectedData
{
}
