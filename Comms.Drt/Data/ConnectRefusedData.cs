using System.Net;

namespace Comms.Drt;
/// <summary>
/// 表示连接被拒绝时的数据结构。
/// </summary>
public struct ConnectRefusedData
{
    /// <summary>
    /// 连接请求的目标地址。
    /// </summary>
    public IPEndPoint Address;
    /// <summary>
    /// 拒绝连接的原因。
    /// </summary>
    public string Reason;
}
