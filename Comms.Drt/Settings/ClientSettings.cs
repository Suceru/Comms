namespace Comms.Drt;
/// <summary>
/// 客户端设置
/// 此类包含客户端的各种配置设置。 
/// 主要用于网络通信的相关参数配置，以确保客户端的稳定性和可靠性。
/// </summary>
public class ClientSettings
{
    /// <summary>
    /// 安全延迟，单位为秒
    /// 该属性用于设置客户端在发送请求或数据时的安全延迟时间。
    /// 这有助于防止由于网络延迟而导致的潜在问题，例如重发数据或处理超时。
    /// 默认值为 0.2 秒，可以根据实际情况进行调整。
    /// </summary>
    public float SafetyLag = 0.2f;
}
