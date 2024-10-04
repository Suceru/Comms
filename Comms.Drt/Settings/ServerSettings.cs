namespace Comms.Drt;
/// <summary>
/// 服务器设置
/// 
/// 此类用于配置服务器的各种设置，包括名称、优先级、最大游戏数量、超时设置和同步检测参数。
/// 这些设置帮助服务器在运行期间进行优化和管理。
/// </summary>
public class ServerSettings
{
    /// <summary>
    /// 服务器名称
    /// 
    /// 此属性定义了服务器的名称，默认为 "Server"。
    /// 服务器名称可以在客户端发现时使用，帮助用户识别服务器。
    /// </summary>
    public string Name = "Server";
    /// <summary>
    /// 服务器优先级
    /// 
    /// 此属性定义了服务器的优先级，默认为 100。
    /// 服务器的优先级可以影响在客户端进行发现时的排序，值越小优先级越高。
    /// </summary>
    public int Priority = 100;
    /// <summary>
    /// 最大游戏数量
    /// 
    /// 此属性定义了服务器能够支持的最大游戏数量，默认为 1000。
    /// 当达到此限制时，服务器将不再接受新的游戏创建请求。
    /// </summary>
    public int MaxGames = 1000;
    /// <summary>
    /// 最大游戏列表数量
    /// 
    /// 此属性定义了服务器在响应游戏列表请求时返回的最大游戏数量，默认为 50。
    /// 这有助于减少客户端的负担，提高响应效率。
    /// </summary>
    public int MaxGamesToList = 50;
    /// <summary>
    /// 游戏列表缓存时间，单位为秒
    /// 
    /// 此属性定义了游戏列表在服务器上缓存的时间，默认为 1 秒。
    /// 在此时间内，客户端请求的游戏列表将从缓存中提供，而不是重新生成，减少了处理开销。
    /// </summary>
    public float GameListCacheTime = 1f;
    /// <summary>
    /// 加入请求超时时间，单位为秒
    /// 
    /// 此属性定义了客户端加入游戏时的请求超时时间，默认为 15 秒。
    /// 如果在此时间内未收到服务器的响应，客户端将认为请求失败。
    /// </summary>
    public float JoinRequestTimeout = 15f;
    /// <summary>
    /// 状态请求周期，单位为秒
    /// 
    /// 此属性定义了客户端请求游戏状态的时间间隔，默认为 2 秒。
    /// 通过定期请求状态，客户端能够保持对游戏的更新信息。
    /// </summary>
    public float StateRequestPeriod = 2f;
    /// <summary>
    /// 游戏描述请求周期，单位为秒
    /// 
    /// 此属性定义了客户端请求游戏描述的时间间隔，默认为 2 秒。
    /// 客户端通过定期请求游戏描述来更新对游戏的理解。
    /// </summary>
    public float GameDescriptionRequestPeriod = 2f;
    /// <summary>
    /// 回合制游戏的等待时间，单位为秒
    /// 
    /// 此属性定义了回合制游戏中每个回合的等待时间，默认为 0.05 秒。
    /// 这允许玩家在每个回合之间有足够的时间进行操作。
    /// </summary>
    public float TurnBasedTickWaitTime = 0.05f;
    /// <summary>
    /// 同步检测模式
    /// 
    /// 此属性定义了服务器的同步检测模式，默认为 DesyncDetectionMode.Detect。
    /// 可以设置为 None、Detect 或 Locate，以适应不同的需求。
    /// </summary>
    public DesyncDetectionMode DesyncDetectionMode = DesyncDetectionMode.Detect;
    /// <summary>
    /// 同步检测周期
    /// 
    /// 此属性定义了同步检测的时间间隔，默认为 20。
    /// 该周期决定了服务器进行同步检测的频率。
    /// </summary>
    public int DesyncDetectionPeriod = 20;
    /// <summary>
    /// 同步检测状态超时时间，单位为秒
    /// 
    /// 此属性定义了同步检测状态的超时时间，默认为 15 秒。
    /// 如果在此时间内未收到足够的状态信息，服务器将停止检测。
    /// </summary>
    public float DesyncDetectionStatesTimeout = 15f;
}
