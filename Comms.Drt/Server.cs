using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Comms.Drt;

public class Server : IDisposable
{
    // ��ʶ�������Ƿ��ѱ��ͷ�
    private volatile bool IsDisposed;
    // ���ڶ�ʱ���������
    private Alarm Alarm;
    // ��һ����Ϸ��ID
    private int NextGameId;
    // ������������Ӧ��Ϣ����
    private byte[] DiscoveryResponseMessage;
    // ����ķ�����Ӧ��Ϣ������ʱ��
    private double DiscoveryResponseMessageTime;
    // ��ǰ�������е���Ϸ�б�
    private List<ServerGame> ServerGames = new List<ServerGame>();
    // ��Ϣ���л��������ڴ���������Ϣ
    internal MessageSerializer MessageSerializer;
    // ��ȡ��ǰ��������Ϸ������ID
    public int GameTypeID => MessageSerializer.GameTypeID;
    // ��ȡÿ�θ��µ�ʱ����
    public float TickDuration { get; }
    // ÿ�θ��µĲ�����
    public int StepsPerTick { get; }
    // ��ȡ��������Peer���󣬸������紫��
    public Peer Peer { get; private set; }
    // ��ȡ��������IP��ַ
    public IPEndPoint Address => Peer.Address;
    // ֻ���ĵ�ǰ�������е���Ϸ�б�
    public IReadOnlyList<ServerGame> Games => ServerGames;
    // ���������ö���
    public ServerSettings Settings { get; } = new ServerSettings();

    // �����¼�������֪ͨ�ⲿ���
    public event Action<ResourceRequestData> ResourceRequest;

	public event Action<DesyncData> Desync;

	public event Action<Exception> Error;

	public event Action<string> Warning;

	public event Action<string> Information;

	public event Action<string> Debug;

    // ͨ���˿ڴ���������
    public Server(int gameTypeID, float tickDuration, int stepsPerTick, int localPort)
		: this(gameTypeID, tickDuration, stepsPerTick, new UdpTransmitter(localPort))
	{
	}

    /// <summary>
	/// ͨ���Զ��崫��������������
	/// </summary>
	/// <param name="gameTypeID">��Ϸ����ID</param>
	/// <param name="tickDuration">��ǰtick</param>
	/// <param name="stepsPerTick">ÿtick����</param>
	/// <param name="transmitter">������</param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	/// <exception cref="ProtocolViolationException"></exception>
    public Server(int gameTypeID, float tickDuration, int stepsPerTick, ITransmitter transmitter)
	{
        // ������֤��ȷ��������tickʱ�����Ч��
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
        // ��ʼ��TickDuration��StepsPerTick
        TickDuration = tickDuration;
		StepsPerTick = stepsPerTick;
        // ��ʼ����Ϣ���л���
        MessageSerializer = new MessageSerializer(gameTypeID);
        // ��ʼ��Peer���������紫��
        Peer = new Peer(transmitter);
		Peer.Settings.SendPeerConnectDisconnectNotifications = false;

        // ��������ͨ���еĸ�����Ϣ���ͺ��¼�
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
        // �ͻ��˷���������
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
        // �ͻ�������������
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
        // �����յ��Ŀͻ���������Ϣ
        Peer.DataMessageReceived += delegate(PeerPacket p)
		{
			if (!IsDisposed)
			{
				Message message = MessageSerializer.Read(p.Bytes, p.PeerData.Address);
                // ���δ���ͬ�Ŀͻ�����Ϣ����
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
        // ����ͻ��˶Ͽ����ӵ����
        Peer.PeerDisconnected += delegate(PeerData p)
		{
			if (!IsDisposed)
			{
				HandleDisconnect(p);
			}
		};
	}
    /// <summary>
	/// �ͷ���Դ���޷���ֵ��
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
	/// �������������޷���ֵ��
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
	/// �Ͽ����пͻ������ӣ��޷���ֵ��
	/// </summary>
    public void DisconnectAllClients()
	{
		CheckNotDisposedAndStarted();
		Peer.DisconnectAllPeers();
	}
    /// <summary>
	/// ������Դ���޷���ֵ��
	/// </summary>
	/// <param name="address">��ַ</param>
	/// <param name="name">����</param>
	/// <param name="version">�汾</param>
	/// <param name="bytes">�ֽ�����</param>
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
	/// ���ӹ��ܣ�ÿ��ִ��ʱ������Ϸ״̬���޷���ֵ��
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
            // �Ƴ��ѽ�������Ϸ
            ServerGames.RemoveAll(delegate(ServerGame g)
			{
				if (g.Clients.Count == 0)
				{
					InvokeInformation($"Game {g.GameID} finished.");
					return true;
				}
				return false;
			});
            // ������һ������ʱ��
            Alarm.Set(Math.Max(num - Comm.GetTime(), 0.0));
		}
	}
    /// <summary>
	/// ���������Ƿ��Ѿ��ͷ���Դ���޷���ֵ��
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
	/// ���������Ƿ�δ�ͷ������������޷���ֵ��
	/// </summary>
	/// <exception cref="InvalidOperationException">������������</exception>
    private void CheckNotDisposedAndStarted()
	{
		CheckNotDisposed();
		if (Alarm == null)
		{
			throw new InvalidOperationException("Server is not started.");
		}
	}
    /// <summary>
	/// ����ͻ��˷��������ģ��޷���ֵ��
	/// </summary>
	/// <param name="message">����</param>
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
	/// ����ͻ�����Դ�����ģ��޷���ֵ��
	/// </summary>
	/// <param name="message">����</param>
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
	/// ������Ϸ���������ģ��޷���ֵ��
	/// </summary>
	/// <param name="message">����</param>
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
	/// ����ͻ��˼�����Ϸ�����ģ��޷���ֵ��
	/// </summary>
	/// <param name="message">����</param>
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
	/// ����ͻ���ͬ�������Ϸ���ģ��޷���ֵ��
	/// </summary>
	/// <param name="message">����</param>
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
	/// ����ͻ��˾ܾ�������Ϸ���ģ��޷���ֵ��
	/// </summary>
	/// <param name="message">����</param>
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
	/// ����ͻ������뱨�ģ��޷���ֵ��
	/// </summary>
	/// <param name="message">����</param>
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
	/// ����ͻ���״̬���ģ��޷���ֵ��
	/// </summary>
	/// <param name="message">����</param>
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
	/// ����ͻ���ͬ��ʧЧ״̬���ģ��޷���ֵ��
	/// </summary>
	/// <param name="message">����</param>
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
	/// ����ͻ���״̬Hash���ģ��޷���ֵ��
	/// </summary>
	/// <param name="message">����</param>
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
	/// ����ͻ�����Ϸ�������ģ��޷���ֵ��
	/// </summary>
	/// <param name="message">����</param>
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
	/// ����ͻ��˶Ͽ����ӣ��޷���ֵ��
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
    // ����ͬ��ʧЧ�¼�
    internal void InvokeDesync(DesyncData desyncData)
	{
		this.Desync?.Invoke(desyncData);
	}
    // ���ô����¼�
    internal void InvokeError(Exception error)
	{
		this.Error?.Invoke(error);
	}
    // ���þ����¼�
    internal void InvokeWarning(string warning)
	{
		this.Warning?.Invoke(warning);
	}
    // ������Ϣ�¼�
    internal void InvokeInformation(string information)
	{
		this.Information?.Invoke(information);
	}

    // ���õ����¼�
    [Conditional("DEBUG")]
	internal void InvokeDebug(string format, params object[] args)
	{
		if (this.Debug != null)
		{
			this.Debug?.Invoke(string.Format(format, args));
		}
	}
}
