using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Comms.Drt;

public class Server : IDisposable
{
    // 标识服务器是否已被释放
    private volatile bool IsDisposed;
    // 用于定时任务的闹钟
    private Alarm Alarm;
    // 下一局游戏的ID
    private int NextGameId;
    // 服务器发现响应消息缓存
    private byte[] DiscoveryResponseMessage;
    // 缓存的发现响应消息的生成时间
    private double DiscoveryResponseMessageTime;
    // 当前服务器中的游戏列表
    private List<ServerGame> ServerGames = new List<ServerGame>();
    // 消息序列化器，用于处理网络消息
    internal MessageSerializer MessageSerializer;
    // 获取当前服务器游戏的类型ID
    public int GameTypeID => MessageSerializer.GameTypeID;
    // 获取每次更新的时间间隔
    public float TickDuration { get; }
    // 每次更新的步骤数
    public int StepsPerTick { get; }
    // 获取服务器端Peer对象，负责网络传输
    public Peer Peer { get; private set; }
    // 获取服务器的IP地址
    public IPEndPoint Address => Peer.Address;
    // 只读的当前服务器中的游戏列表
    public IReadOnlyList<ServerGame> Games => ServerGames;
    // 服务器设置对象
    public ServerSettings Settings { get; } = new ServerSettings();

    // 各种事件，用于通知外部组件
    public event Action<ResourceRequestData> ResourceRequest;

	public event Action<DesyncData> Desync;

	public event Action<Exception> Error;

	public event Action<string> Warning;

	public event Action<string> Information;

	public event Action<string> Debug;

    // 通过端口创建服务器
    public Server(int gameTypeID, float tickDuration, int stepsPerTick, int localPort)
		: this(gameTypeID, tickDuration, stepsPerTick, new UdpTransmitter(localPort))
	{
	}

    /// <summary>
	/// 通过自定义传输器创建服务器
	/// </summary>
	/// <param name="gameTypeID">游戏类型ID</param>
	/// <param name="tickDuration">当前tick</param>
	/// <param name="stepsPerTick">每tick步数</param>
	/// <param name="transmitter">传输器</param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	/// <exception cref="ProtocolViolationException"></exception>
    public Server(int gameTypeID, float tickDuration, int stepsPerTick, ITransmitter transmitter)
	{
        // 参数验证，确保步数和tick时间的有效性
        if (stepsPerTick < 1)
		{
			throw new ArgumentOutOfRangeException("stepsPerTick");
		}
		if (tickDuration != 0f && (tickDuration < 0.01f || tickDuration > 10f))
		{
			throw new ArgumentOutOfRangeException("tickDuration");
		}
		if (tickDuration == 0f && stepsPerTick != 1)
		{
			throw new ArgumentOutOfRangeException("stepsPerTick");
		}
        // 初始化TickDuration和StepsPerTick
        TickDuration = tickDuration;
		StepsPerTick = stepsPerTick;
        // 初始化消息序列化器
        MessageSerializer = new MessageSerializer(gameTypeID);
        // 初始化Peer，用于网络传输
        Peer = new Peer(transmitter);
		Peer.Settings.SendPeerConnectDisconnectNotifications = false;

        // 处理网络通信中的各种消息类型和事件
        Peer.Error += delegate(Exception e)
		{
			if (e is MalformedMessageException ex)
			{
				InvokeWarning($"Malformed message from {ex.SenderAddress} ignored. {ex.Message}");
			}
			else
			{
				InvokeError(e);
			}
		};
        // 客户端发现请求处理
        Peer.PeerDiscoveryRequest += delegate(Packet p)
		{
			if (!IsDisposed)
			{
				Message message12 = MessageSerializer.Read(p.Bytes, p.Address);
				if (!(message12 is ClientDiscoveryRequestMessage message13))
				{
					if (!(message12 is ClientResourceRequestMessage message14))
					{
						throw new ProtocolViolationException($"Unexpected message type {message12.GetType()}.");
					}
					Handle(message14, p.Address);
				}
				else
				{
					Handle(message13, p.Address);
				}
			}
		};
        // 客户端连接请求处理
        Peer.ConnectRequest += delegate(PeerPacket p)
		{
			if (!IsDisposed)
			{
				Message message9 = MessageSerializer.Read(p.Bytes, p.PeerData.Address);
				if (!(message9 is ClientCreateGameRequestMessage message10))
				{
					if (!(message9 is ClientJoinGameRequestMessage message11))
					{
						throw new ProtocolViolationException($"Unexpected message type {message9.GetType()}.");
					}
					Handle(message11, p.PeerData);
				}
				else
				{
					Handle(message10, p.PeerData);
				}
			}
		};
        // 处理收到的客户端数据消息
        Peer.DataMessageReceived += delegate(PeerPacket p)
		{
			if (!IsDisposed)
			{
				Message message = MessageSerializer.Read(p.Bytes, p.PeerData.Address);
                // 依次处理不同的客户端消息类型
                if (!(message is ClientJoinGameAcceptedMessage message2))
				{
					if (!(message is ClientJoinGameRefusedMessage message3))
					{
						if (!(message is ClientInputMessage message4))
						{
							if (!(message is ClientStateMessage message5))
							{
								if (!(message is ClientDesyncStateMessage message6))
								{
									if (!(message is ClientStateHashesMessage message7))
									{
										if (!(message is ClientGameDescriptionMessage message8))
										{
											throw new ProtocolViolationException($"Unexpected message type {message.GetType()}.");
										}
										Handle(message8, p.PeerData);
									}
									else
									{
										Handle(message7, p.PeerData);
									}
								}
								else
								{
									Handle(message6, p.PeerData);
								}
							}
							else
							{
								Handle(message5, p.PeerData);
							}
						}
						else
						{
							Handle(message4, p.PeerData);
						}
					}
					else
					{
						Handle(message3, p.PeerData);
					}
				}
				else
				{
					Handle(message2, p.PeerData);
				}
			}
		};
        // 处理客户端断开连接的情况
        Peer.PeerDisconnected += delegate(PeerData p)
		{
			if (!IsDisposed)
			{
				HandleDisconnect(p);
			}
		};
	}
    /// <summary>
	/// 释放资源，无返回值。
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
		Alarm?.Dispose();
		Peer?.Dispose();
	}
    /// <summary>
	/// 启动服务器，无返回值。
	/// </summary>
	/// <exception cref="InvalidOperationException"></exception>
    public void Start()
	{
		lock (Peer.Lock)
		{
			CheckNotDisposed();
			if (Alarm != null)
			{
				throw new InvalidOperationException("Server is already started.");
			}
			InvokeInformation($"Server {Address} started at {DateTime.UtcNow}");
			Peer.Start();
			Alarm = new Alarm(AlarmFunction);
			Alarm.Error += delegate(Exception e)
			{
				InvokeError(e);
			};
			Alarm.Set(0.0);
		}
	}
    /// <summary>
	/// 断开所有客户端连接，无返回值。
	/// </summary>
    public void DisconnectAllClients()
	{
		CheckNotDisposedAndStarted();
		Peer.DisconnectAllPeers();
	}
    /// <summary>
	/// 发送资源，无返回值。
	/// </summary>
	/// <param name="address">地址</param>
	/// <param name="name">名字</param>
	/// <param name="version">版本</param>
	/// <param name="bytes">字节数据</param>
    public void SendResource(IPEndPoint address, string name, int version, byte[] bytes)
	{
		lock (Peer.Lock)
		{
			CheckNotDisposed();
			Peer.RespondToDiscovery(address, DeliveryMode.Reliable, MessageSerializer.Write(new ServerResourceMessage
			{
				Name = name,
				Version = version,
				Bytes = bytes
			}));
		}
	}
    /// <summary>
	/// 闹钟功能，每次执行时更新游戏状态，无返回值。
	/// </summary>
    private void AlarmFunction()
	{
		lock (Peer.Lock)
		{
			if (IsDisposed)
			{
				return;
			}
			double time = Comm.GetTime();
			double num = double.PositiveInfinity;
			foreach (ServerGame serverGame in ServerGames)
			{
				double val = serverGame.Run(time);
				num = Math.Min(num, val);
			}
            // 移除已结束的游戏
            ServerGames.RemoveAll(delegate(ServerGame g)
			{
				if (g.Clients.Count == 0)
				{
					InvokeInformation($"Game {g.GameID} finished.");
					return true;
				}
				return false;
			});
            // 设置下一次闹钟时间
            Alarm.Set(Math.Max(num - Comm.GetTime(), 0.0));
		}
	}
    /// <summary>
	/// 检查服务器是否已经释放资源，无返回值。
	/// </summary>
	/// <exception cref="ObjectDisposedException"></exception>
    private void CheckNotDisposed()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("Server");
		}
	}
    /// <summary>
	/// 检查服务器是否未释放且已启动，无返回值。
	/// </summary>
	/// <exception cref="InvalidOperationException">服务器已启动</exception>
    private void CheckNotDisposedAndStarted()
	{
		CheckNotDisposed();
		if (Alarm == null)
		{
			throw new InvalidOperationException("Server is not started.");
		}
	}
    /// <summary>
	/// 处理客户端发现请求报文，无返回值。
	/// </summary>
	/// <param name="message">报文</param>
	/// <param name="address">IP</param>
    private void Handle(ClientDiscoveryRequestMessage message, IPEndPoint address)
	{
		double time = Comm.GetTime();
		if (DiscoveryResponseMessage == null || time > DiscoveryResponseMessageTime + (double)Settings.GameListCacheTime)
		{
			DiscoveryResponseMessageTime = time;
			DiscoveryResponseMessage = MessageSerializer.Write(new ServerDiscoveryResponseMessage
			{
				Name = Settings.Name,
				Priority = Settings.Priority,
				GamesDescriptions = (from g in ServerGames.OrderBy((ServerGame g) => g.Tick).Take(Settings.MaxGamesToList)
					select g.CreateGameDescription()).ToArray()
			});
		}
		Peer.RespondToDiscovery(address, DeliveryMode.Unreliable, DiscoveryResponseMessage);
	}
    /// <summary>
	/// 处理客户端资源请求报文，无返回值。
	/// </summary>
	/// <param name="message">报文</param>
	/// <param name="address">IP</param>
    private void Handle(ClientResourceRequestMessage message, IPEndPoint address)
	{
		this.ResourceRequest?.Invoke(new ResourceRequestData
		{
			Address = address,
			Name = message.Name,
			MinimumVersion = message.MinimumVersion
		});
	}
    /// <summary>
	/// 处理游戏创建请求报文，无返回值。
	/// </summary>
	/// <param name="message">报文</param>
	/// <param name="peerData">PD</param>
    private void Handle(ClientCreateGameRequestMessage message, PeerData peerData)
	{
		if (Settings.Priority <= 0)
		{
			Peer.RefuseConnect(peerData, MessageSerializer.Write(new ServerConnectRefusedMessage
			{
				Reason = "Server is currently not accepting new games."
			}));
			InvokeWarning($"Create game request from client \"{message.ClientName}\" at {peerData.Address} refused because priority is <= 0.");
			return;
		}
		if (ServerGames.Count >= Settings.MaxGames)
		{
			Peer.RefuseConnect(peerData, MessageSerializer.Write(new ServerConnectRefusedMessage
			{
				Reason = "Too many games."
			}));
			InvokeWarning($"Create game request from client \"{message.ClientName}\" at {peerData.Address} refused because of too many games ({ServerGames.Count}).");
			return;
		}
		ServerGame serverGame = new ServerGame(this, peerData, NextGameId++, message);
		ServerGames.Add(serverGame);
		InvokeInformation($"Client \"{message.ClientName}\" at {peerData.Address} created game {serverGame.GameID}.");
		Peer.AcceptConnect(peerData, MessageSerializer.Write(new ServerCreateGameAcceptedMessage
		{
			GameID = serverGame.GameID,
			CreatorAddress = peerData.Address,
			TickDuration = TickDuration,
			StepsPerTick = StepsPerTick,
			DesyncDetectionMode = serverGame.DesyncDetectionMode,
			DesyncDetectionPeriod = serverGame.DesyncDetectionPeriod
		}));
		Alarm.Set(0.0);
	}
    /// <summary>
	/// 处理客户端加入游戏请求报文，无返回值。
	/// </summary>
	/// <param name="message">报文</param>
	/// <param name="peerData">PD</param>
    private void Handle(ClientJoinGameRequestMessage message, PeerData peerData)
	{
		ServerGame serverGame = ServerGames.FirstOrDefault((ServerGame g) => g.GameID == message.GameID);
		if (serverGame != null)
		{
			serverGame.Handle(message, peerData);
			return;
		}
		InvokeWarning($"Join game request from {peerData.Address} for nonexistent game {message.GameID}.");
		Peer.RefuseConnect(peerData, MessageSerializer.Write(new ServerConnectRefusedMessage
		{
			Reason = "Game does not exist."
		}));
	}
    /// <summary>
	/// 处理客户端同意加入游戏报文，无返回值。
	/// </summary>
	/// <param name="message">报文</param>
	/// <param name="peerData">PD</param>
    private void Handle(ClientJoinGameAcceptedMessage message, PeerData peerData)
	{
		ServerClient serverClient = ServerClient.FromPeerData(peerData);
		if (serverClient != null)
		{
			serverClient.ServerGame.Handle(message, serverClient);
		}
		else
		{
			InvokeWarning($"Game join accepted from {peerData.Address}, which is not a connected client.");
		}
	}
    /// <summary>
	/// 处理客户端拒绝加入游戏报文，无返回值。
	/// </summary>
	/// <param name="message">报文</param>
	/// <param name="peerData">PD</param>
    private void Handle(ClientJoinGameRefusedMessage message, PeerData peerData)
	{
		ServerClient serverClient = ServerClient.FromPeerData(peerData);
		if (serverClient != null)
		{
			serverClient.ServerGame.Handle(message, serverClient);
		}
		else
		{
			InvokeWarning($"Game join refused from {peerData.Address}, which is not a connected client.");
		}
	}
    /// <summary>
	/// 处理客户端输入报文，无返回值。
	/// </summary>
	/// <param name="message">报文</param>
	/// <param name="peerData">PD</param>
    private void Handle(ClientInputMessage message, PeerData peerData)
	{
		ServerClient serverClient = ServerClient.FromPeerData(peerData);
		if (serverClient != null)
		{
			serverClient.ServerGame.Handle(message, serverClient);
		}
		else
		{
			InvokeWarning($"Input from {peerData.Address}, which is not a connected client.");
		}
	}
    /// <summary>
	/// 处理客户端状态报文，无返回值。
	/// </summary>
	/// <param name="message">报文</param>
	/// <param name="peerData">PD</param>
    private void Handle(ClientStateMessage message, PeerData peerData)
	{
		ServerClient serverClient = ServerClient.FromPeerData(peerData);
		if (serverClient != null)
		{
			serverClient.ServerGame.Handle(message, serverClient);
		}
		else
		{
			InvokeWarning($"State from {peerData.Address}, which is not a connected client.");
		}
	}
    /// <summary>
	/// 处理客户端同步失效状态报文，无返回值。
	/// </summary>
	/// <param name="message">报文</param>
	/// <param name="peerData">PD</param>
    private void Handle(ClientDesyncStateMessage message, PeerData peerData)
	{
		ServerClient serverClient = ServerClient.FromPeerData(peerData);
		if (serverClient != null)
		{
			serverClient.ServerGame.Handle(message, serverClient);
		}
		else
		{
			InvokeWarning($"Desync state from {peerData.Address}, which is not a connected client.");
		}
	}
    /// <summary>
	/// 处理客户端状态Hash报文，无返回值。
	/// </summary>
	/// <param name="message">报文</param>
	/// <param name="peerData">PD</param>
    private void Handle(ClientStateHashesMessage message, PeerData peerData)
	{
		ServerClient serverClient = ServerClient.FromPeerData(peerData);
		if (serverClient != null)
		{
			serverClient.ServerGame.Handle(message, serverClient);
		}
		else
		{
			InvokeWarning($"State hashes from {peerData.Address}, which is not a connected client.");
		}
	}
    /// <summary>
	/// 处理客户端游戏描述报文，无返回值。
	/// </summary>
	/// <param name="message">报文</param>
	/// <param name="peerData">PD</param>
    private void Handle(ClientGameDescriptionMessage message, PeerData peerData)
	{
		ServerClient serverClient = ServerClient.FromPeerData(peerData);
		if (serverClient != null)
		{
			serverClient.ServerGame.Handle(message, serverClient);
		}
		else
		{
			InvokeWarning($"Game description from {peerData.Address}, which is not a connected client.");
		}
	}
    /// <summary>
	/// 处理客户端断开连接，无返回值。
	/// </summary>
	/// <param name="peerData">PD</param>
    private void HandleDisconnect(PeerData peerData)
	{
		ServerClient serverClient = ServerClient.FromPeerData(peerData);
		if (serverClient != null)
		{
			serverClient.ServerGame.HandleDisconnect(serverClient);
		}
		else
		{
			InvokeWarning($"Disconnect received from {peerData.Address}, which is not a connected client.");
		}
	}
    // 调用同步失效事件
    internal void InvokeDesync(DesyncData desyncData)
	{
		this.Desync?.Invoke(desyncData);
	}
    // 调用错误事件
    internal void InvokeError(Exception error)
	{
		this.Error?.Invoke(error);
	}
    // 调用警告事件
    internal void InvokeWarning(string warning)
	{
		this.Warning?.Invoke(warning);
	}
    // 调用信息事件
    internal void InvokeInformation(string information)
	{
		this.Information?.Invoke(information);
	}

    // 调用调试事件
    [Conditional("DEBUG")]
	internal void InvokeDebug(string format, params object[] args)
	{
		if (this.Debug != null)
		{
			this.Debug?.Invoke(string.Format(format, args));
		}
	}
}
