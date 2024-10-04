using System.Net;

namespace Comms.Drt;
/// <summary>
/// 表示资源请求的数据结构体。
/// 该结构用于请求某一特定资源，指定资源的名称、最低版本号以及请求目的地址。
/// </summary>
public struct ResourceRequestData
{
    /// <summary>
    /// 请求目标的网络地址。
    /// 表示资源请求将发送到的目标，通常是某个服务器或客户端的 IP 地址和端口。
    /// </summary>
    public IPEndPoint Address;
    /// <summary>
    /// 请求的资源名称。
    /// 指定需要请求的资源的名称，比如文件名、资源标识符等，用于识别需要获取的具体资源。
    /// </summary>
    public string Name;
    /// <summary>
    /// 请求的资源最低版本号。
    /// 指定请求的资源必须是该版本或更高版本，确保客户端或服务器获取的是最新或特定版本的资源。
    /// </summary>
    public int MinimumVersion;
}
