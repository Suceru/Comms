namespace Comms;
/// <summary>
/// CommSettings 类用于定义网络通信的配置参数。
/// 它提供了对通信过程中的重传行为、重复包检测以及闲置时间的控制。
/// </summary>
public class CommSettings
{
    /// <summary>
    /// MaxResends 属性表示在数据传输失败时，允许的最大重传次数。
    /// 如果超过此值，则放弃重传并认为传输失败。
    /// 默认值为30，表示最多允许重传30次。
    /// </summary>
    public int MaxResends { get; set; } = 30;

    /// <summary>
    /// ResendPeriods 属性定义了重传周期的时间间隔。
    /// 它是一个包含两个浮点值的数组，第一个值表示初次重传的延迟时间，第二个值表示每次后续重传的间隔时间。
    /// 默认值为 [0.5f, 1f]，表示首次重传在 0.5 秒后触发，后续重传每隔 1 秒进行一次。
    /// </summary>
    public float[] ResendPeriods { get; set; } = new float[2] { 0.5f, 1f };

    /// <summary>
    /// DuplicatePacketsDetectionTime 属性定义了系统在多长时间内检测并丢弃重复数据包。
    /// 该值的单位是秒，默认值为 20 秒。
    /// 在这个时间段内，如果接收到相同的数据包，则会被视为重复包并被忽略。
    /// </summary>
    public float DuplicatePacketsDetectionTime { get; set; } = 20f;

    /// <summary>
    /// IdleTime 属性表示在通信通道上没有任何数据包活动时，经过多长时间系统会将其视为闲置。
    /// 该值的单位是秒，默认值为 120 秒。
    /// 如果超过该时间没有任何通信活动，可能会触发超时或连接断开等行为。
    /// </summary>
    public float IdleTime { get; set; } = 120f;

}
