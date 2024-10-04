using System.Net;

namespace Comms.Drt;
/// <summary>
/// 表示连接超时的数据结构。
/// </summary>
public struct ConnectTimedOutData
{
    /// <summary>
    /// 连接请求的目标地址。
    /// </summary>
    public IPEndPoint Address;
}
