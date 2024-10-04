namespace Comms.Drt;
/// <summary>
/// 表示资源数据的结构体。
/// 用于在客户端和服务器之间传输资源信息，如游戏中的文件、配置或其他重要数据。
/// </summary>
public struct ResourceData
{
    /// <summary>
    /// 资源的名称。
    /// 例如文件的名称、资源标识符等，用于标识该资源。
    /// </summary>
    public string Name;
    /// <summary>
    /// 资源的版本号。
    /// 用于标识资源的版本，不同的版本号可以用于区分资源的更新状态。
    /// </summary>
    public int Version;
    /// <summary>
    /// 资源的原始字节数据。
    /// 资源的内容以字节数组的形式存储，可能是文件、图片、二进制数据等。
    /// </summary>
    public byte[] Bytes;
}
