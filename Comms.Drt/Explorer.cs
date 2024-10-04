using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Comms.Drt;
/// <summary>
///Explorer 类是用于发现、管理和与游戏服务器通信的组件。
///它负责通过本地广播或互联网寻找游戏服务器，并处理与这些服务器的消息交换。
///实现了 IDisposable 接口以确保资源的正确释放。
/// </summary>
public class Explorer : IDisposable
{
    // 内部类 DnsCache 用于缓存 DNS 查询结果，避免重复查询。
    private class DnsCache
	{
        // 字典存储 DNS 查询缓存，key 为主机名，value 为 IP 地址列表。
        private Dictionary<string, IPAddress[]> Cache = new Dictionary<string, IPAddress[]>();

        // 根据主机名查询 IP 地址缓存。如果缓存中存在则返回，否则返回 null。
        public IPAddress[] Query(string host)
		{
			lock (Cache)
			{
				Cache.TryGetValue(host, out var value);
				return value;
			}
		}
        // 将主机名和对应的 IP 地址数组添加到缓存中。
        public void Add(string host, IPAddress[] addresses)
		{
			lock (Cache)
			{
				Cache[host] = addresses;
			}
		}
        // 清空缓存。
        public void Clear()
		{
			lock (Cache)
			{
				Cache.Clear();
			}
		}
	}

	private bool IsDisposed;// 指示对象是否已被释放。

    private int[] ServerPorts;// 服务器端口列表。

    private bool LocalBroadcast;// 是否启用本地广播以发现局域网中的服务器。

    private string[] InternetHosts;// 需要通过互联网发现的服务器主机名列表。

    private double LocalLastTime;// 上次本地服务器发现的时间。

    private double InternetLastTime;// 上次互联网服务器发现的时间。

    private Dictionary<IPEndPoint, double> InternetRequestTimes = new Dictionary<IPEndPoint, double>();// 记录互联网请求的时间。

    private Alarm Alarm;// 定时器对象，用于触发周期性任务。

    private DnsCache Cache = new DnsCache();// DNS 缓存。

    private List<ServerDescription> ServersList = new List<ServerDescription>();// 已发现的服务器列表。

    private IReadOnlyList<ServerDescription> ServersReadonlyList;// 只读的服务器列表缓存。

    internal MessageSerializer MessageSerializer;// 用于序列化和反序列化消息的对象。

    /// <summary>
    /// 只读属性：游戏类型 ID，由 MessageSerializer 提供。
    /// </summary>
    public int GameTypeID => MessageSerializer.GameTypeID;
    /// <summary>
    /// 只读属性：与此 Explorer 实例关联的 Peer 对象，管理通信。
    /// </summary>
    public Peer Peer { get; }
    /// <summary>
    /// 只读属性：此 Explorer 实例的 IP 端点。
    /// </summary>
    public IPEndPoint Address => Peer.Address;
    /// <summary>
    /// 只读属性：Explorer 的设置对象，提供相关的配置参数。
    /// </summary>
    public ExplorerSettings Settings { get; } = new ExplorerSettings();

    /// <summary>
    /// 只读属性：指示是否已经启动了服务器发现流程。
    /// </summary>
    public bool IsDiscoveryStarted => Alarm != null;
    /// <summary>
    /// 只读属性：已发现的服务器列表（线程安全）。
    /// </summary>
    public IReadOnlyList<ServerDescription> DiscoveredServers
	{
		get
		{
			lock (Peer.Lock)
			{
				CheckNotDisposed();
				if (ServersReadonlyList == null)
				{
					ServersReadonlyList = ServersList.ToArray();
				}
				return ServersReadonlyList;
			}
		}
	}
    /// <summary>
    /// 资源接收事件，当接收到服务器资源时触发。
    /// </summary>
    public event Action<ResourceData> ResourceReceived;
    /// <summary>
    /// 错误事件，当发生异常时触发。
    /// </summary>
    public event Action<Exception> Error;
    /// <summary>
    /// 调试事件，用于输出调试信息。
    /// </summary>
    public event Action<string> Debug;
    /// <summary>
    /// 服务器发现事件，当发现新的服务器时触发。
    /// </summary>
    public event Action<ServerDescription> ServerDiscovered;
    /// <summary>
    /// 构造函数，根据给定的游戏类型 ID 和服务器端口，创建 Explorer 实例。
    /// </summary>
    /// <param name="gameTypeID">游戏类型 ID </param>
    /// <param name="serverPort">服务器端口</param>
    /// <param name="localPort">本地端口</param>
    public Explorer(int gameTypeID, int serverPort, int localPort = 0)
		: this(gameTypeID, new int[1] { serverPort }, localPort)
	{
	}
    /// <summary>
    /// 构造函数，支持多个服务器端口。
    /// </summary>
    /// <param name="gameTypeID">游戏类型 ID</param>
    /// <param name="serverPorts">多个服务器端口</param>
    /// <param name="localPort">本地端口</param>
    public Explorer(int gameTypeID, IEnumerable<int> serverPorts, int localPort = 0)
		: this(gameTypeID, serverPorts, new UdpTransmitter(localPort))
	{
	}
    /// <summary>
    /// 构造函数，支持自定义传输器（Transmitter），如 UDP。
    /// </summary>
    /// <param name="gameTypeID">游戏类型 ID</param>
    /// <param name="serverPort">服务器端口</param>
    /// <param name="transmitter">传输器</param>
    public Explorer(int gameTypeID, int serverPort, ITransmitter transmitter)
		: this(gameTypeID, new int[1] { serverPort }, transmitter)
	{
	}
    /// <summary>
    /// 主构造函数，初始化游戏类型 ID、服务器端口和传输器，并启动 Peer。
    /// </summary>
    /// <param name="gameTypeID">游戏类型 ID</param>
    /// <param name="serverPorts">多个服务器端口</param>
    /// <param name="transmitter">传输器</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ProtocolViolationException"></exception>
    public Explorer(int gameTypeID, IEnumerable<int> serverPorts, ITransmitter transmitter)
	{
		if (transmitter == null)
		{
			throw new ArgumentNullException("transmitter");
		}
		if (serverPorts.Any((int p) => p < 0 || p > 65535))
		{
			throw new ArgumentOutOfRangeException("serverPorts");
		}
		MessageSerializer = new MessageSerializer(gameTypeID);
		ServerPorts = serverPorts.ToArray();
		Peer = new Peer(transmitter);
		Peer.Error += delegate(Exception e)
		{
			if (!(e is MalformedMessageException))
			{
				InvokeError(e);
			}
		};
		Peer.PeerDiscoveryRequest += delegate
		{
		};
		Peer.PeerDiscovered += delegate(Packet p)
		{
			if (!IsDisposed)
			{
				Message message = MessageSerializer.Read(p.Bytes, p.Address);
				if (!(message is ServerDiscoveryResponseMessage message2))
				{
					if (!(message is ServerResourceMessage message3))
					{
						throw new ProtocolViolationException($"Unexpected message type {message.GetType()}.");
					}
					Handle(message3, p.Address);
				}
				else
				{
					Handle(message2, p.Address);
				}
			}
		};
		Peer.Start();
	}
    /// <summary>
    /// 实现 IDisposable 接口，释放资源。
    /// </summary>
    public void Dispose()
	{
		lock (Peer.Lock)
		{
			if (IsDisposed)
			{
				return;
			}
			IsDisposed = true;
		}
		if (Alarm != null)
		{
			Alarm.Dispose();
			Alarm = null;
		}
		Peer.Dispose();
	}
    /// <summary>
    /// 开始服务器发现流程，支持本地广播和互联网主机发现。
    /// </summary>
    /// <param name="localBroadcast">本地广播</param>
    /// <param name="internetHosts">互联网主机</param>
    public void StartDiscovery(bool localBroadcast = true, IEnumerable<string> internetHosts = null)
	{
		lock (Peer.Lock)
		{
			CheckNotDisposed();
			LocalBroadcast = localBroadcast;
			InternetHosts = ((internetHosts != null) ? internetHosts.ToArray() : Array.Empty<string>());
			LocalLastTime = double.MinValue;
			InternetLastTime = double.MinValue;
			Cache.Clear();
			if (Alarm != null)
			{
				Alarm.Dispose();
			}
			Alarm = new Alarm(AlarmFunction);
			Alarm.Error += delegate(Exception e)
			{
				InvokeError(e);
			};
			Alarm.Set(0.0);
		}
	}
    /// <summary>
    /// 停止服务器发现流程，清空服务器列表。
    /// </summary>
    public void StopDiscovery()
	{
		lock (Peer.Lock)
		{
			CheckNotDisposed();
			ServersList.Clear();
			ServersReadonlyList = null;
			if (Alarm != null)
			{
				Alarm.Dispose();
				Alarm = null;
			}
		}
	}
    /// <summary>
    /// 向指定服务器请求资源。
    /// </summary>
    /// <param name="serverAddress">服务器地址</param>
    /// <param name="name">名字</param>
    /// <param name="minimumVersion">最小版本</param>
    public void RequestResource(IPEndPoint serverAddress, string name, int minimumVersion)
	{
		lock (Peer.Lock)
		{
			CheckNotDisposed();
			Peer.DiscoverPeer(serverAddress, MessageSerializer.Write(new ClientResourceRequestMessage
			{
				Name = name,
				MinimumVersion = minimumVersion
			}));
		}
	}
    /// <summary>
    /// 使用主机名和端口向服务器请求资源，异步执行 DNS 查询。
    /// </summary>
    /// <param name="host">主机</param>
    /// <param name="port">端口</param>
    /// <param name="name">名字</param>
    /// <param name="minimumVersion">最小版本</param>
    public void RequestResource(string host, int port, string name, int minimumVersion)
	{
		Task.Run(delegate
		{
			IPAddress[] array = DnsQueryHost(host);
			foreach (IPAddress address in array)
			{
				RequestResource(new IPEndPoint(address, port), name, minimumVersion);
			}
		});
	}
    // 定时器的回调函数，周期性地执行本地和互联网服务器发现任务。
    private void AlarmFunction()
	{
		lock (Peer.Lock)
		{
			if (!IsDisposed)
			{
				double time = Comm.GetTime();
				if (time >= LocalLastTime + (double)Settings.LocalDiscoveryPeriod)
				{
					LocalLastTime = time;
					DiscoverLocalServers();
				}
				if (time >= InternetLastTime + (double)Settings.InternetDiscoveryPeriod)
				{
					InternetLastTime = time;
					DiscoverInternetServers(InternetHosts);
				}
				double val = (LocalBroadcast ? ((double)Settings.LocalDiscoveryPeriod - (time - LocalLastTime)) : double.MaxValue);
				double val2 = ((InternetHosts.Count() > 0) ? ((double)Settings.InternetDiscoveryPeriod - (time - InternetLastTime)) : double.MaxValue);
				double waitTime = Math.Min(val, val2);
				if (ServersList.RemoveAll(delegate(ServerDescription s)
				{
					double num = Math.Max((double)(s.IsLocal ? Settings.LocalRemoveTime : Settings.InternetRemoveTime) - (time - s.DiscoveryTime), 0.0);
					waitTime = Math.Min(waitTime, num);
					return num <= 0.0;
				}) > 0)
				{
					ServersReadonlyList = null;
				}
				Alarm.Set(waitTime);
			}
		}
	}
    // 执行本地服务器发现，通过广播向局域网中的服务器发送发现请求。
    private void DiscoverLocalServers()
	{
		if (!LocalBroadcast)
		{
			return;
		}
		int[] serverPorts = ServerPorts;
		foreach (int peerPort in serverPorts)
		{
			try
			{
				Peer.DiscoverLocalPeers(peerPort, MessageSerializer.Write(new ClientDiscoveryRequestMessage()));
			}
			catch (Exception error)
			{
				InvokeError(error);
			}
		}
	}
    // 执行互联网服务器发现，向指定的主机名发送发现请求。
    private void DiscoverInternetServers(IEnumerable<string> hosts)
	{
		foreach (string host in hosts)
		{
			Task.Run(delegate
			{
				IPAddress[] array = DnsQueryHost(host);
				if (array != null)
				{
					lock (Peer.Lock)
					{
						IPAddress[] array2 = array;
						foreach (IPAddress address in array2)
						{
							int[] serverPorts = ServerPorts;
							foreach (int port in serverPorts)
							{
								try
								{
									IPEndPoint iPEndPoint = new IPEndPoint(address, port);
									InternetRequestTimes[iPEndPoint] = Comm.GetTime();
									Peer.DiscoverPeer(iPEndPoint, MessageSerializer.Write(new ClientDiscoveryRequestMessage()));
								}
								catch (Exception error)
								{
									InvokeError(error);
								}
							}
						}
					}
				}
			});
		}
	}
    // 检查对象是否已被释放，如果已释放则抛出异常。
    private void CheckNotDisposed()
	{
		if (Peer == null)
		{
			throw new ObjectDisposedException("Server");
		}
	}
    // DNS 查询主机名并返回 IP 地址列表，查询结果会被缓存。
    private IPAddress[] DnsQueryHost(string host)
	{
		IPAddress[] array = Cache.Query(host);
		if (array == null)
		{
			try
			{
				array = Dns.GetHostEntry(host).AddressList.Where((IPAddress a) => a.AddressFamily == AddressFamily.InterNetwork || a.AddressFamily == AddressFamily.InterNetworkV6).ToArray();
				Cache.Add(host, array);
			}
			catch
			{
			}
		}
		return array ?? Array.Empty<IPAddress>();
	}
    // 处理服务器发现响应消息，更新服务器列表。
    private void Handle(ServerDiscoveryResponseMessage message, IPEndPoint address)
	{
		if (!IsDisposed && Alarm != null)
		{
			double time = Comm.GetTime();
			bool isLocal;
			float ping;
			if (InternetRequestTimes.TryGetValue(address, out var value))
			{
				isLocal = false;
				ping = (float)(time - value);
			}
			else
			{
				isLocal = true;
				ping = (float)(time - LocalLastTime);
			}
			ServersList.RemoveAll((ServerDescription s) => object.Equals(s.Address, address));
			ServerDescription serverDescription = new ServerDescription
			{
				Address = address,
				Name = message.Name,
				Priority = message.Priority,
				IsLocal = isLocal,
				Ping = ping,
				DiscoveryTime = time,
				GameDescriptions = message.GamesDescriptions
			};
			GameDescription[] gameDescriptions = serverDescription.GameDescriptions;
			for (int i = 0; i < gameDescriptions.Length; i++)
			{
				gameDescriptions[i].ServerDescription = serverDescription;
			}
			ServersList.Add(serverDescription);
			ServersReadonlyList = null;
			this.ServerDiscovered?.Invoke(serverDescription);
		}
	}
    // 处理服务器资源消息，触发 ResourceReceived 事件。
    private void Handle(ServerResourceMessage message, IPEndPoint address)
	{
		if (!IsDisposed)
		{
			this.ResourceReceived?.Invoke(new ResourceData
			{
				Name = message.Name,
				Version = message.Version,
				Bytes = message.Bytes
			});
		}
	}
    // 触发错误事件。
    internal void InvokeError(Exception error)
	{
		this.Error?.Invoke(error);
	}
    // 调试信息输出，使用 DEBUG 预处理指令。
    [Conditional("DEBUG")]
	internal void InvokeDebug(string format, params object[] args)
	{
		if (this.Debug != null)
		{
			this.Debug?.Invoke(string.Format(format, args));
		}
	}
}
