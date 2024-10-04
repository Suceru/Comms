using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Comms.Drt;
/// <summary>
/// ServerGame 类负责管理服务器端的游戏状态，处理客户端的连接请求、输入信息、断开连接等操作，并同步游戏的时间步进（tick）。
/// </summary>
public class ServerGame
{
    // JoinRequest 是 ServerGame 内部的一个类，用于保存客户端加入游戏的请求信息。
    private class JoinRequest
    {
		// 客户端的 PeerData，包含连接信息。
        public PeerData PeerData;
        // 客户端的唯一 ID。
        public int ClientID;
        // 客户端的名称。
        public string ClientName;
        // 加入请求的字节数据。
        public byte[] JoinRequestBytes;
        // 请求的时间戳，用于超时判断。
        public double RequestTime;
        // 标记该请求是否已被转发给其他客户端。
        public bool Forwarded;
        // 标记接受该请求的客户端。
        public ServerClient AcceptedBy;
        // 标记拒绝该请求的客户端。
        public ServerClient RefusedBy;
        // 下一个状态请求的索引，用于轮询请求状态。
        public int NextStateRequestIndex;
        // 下一个状态请求的时间戳。
        public double NextStateRequestTime;
        // 标记该请求是否已处理。
        public bool Processed;
        // 构造函数初始化 JoinRequest 对象。
        public JoinRequest(PeerData peerData, int clientID, string clientName, byte[] joinRequestBytes)
		{
			PeerData = peerData;
			ClientID = clientID;
			ClientName = clientName;
			JoinRequestBytes = joinRequestBytes;
			RequestTime = Comm.GetTime();// 获取当前时间
            NextStateRequestIndex = clientID;// 初始化状态请求索引
        }
	}
    // 保存当前已连接的客户端列表。
    private List<ServerClient> ServerClients = new List<ServerClient>();

    // 保存所有的加入请求。
    private List<JoinRequest> JoinRequests = new List<JoinRequest>();

    // 保存所有的离线客户端 ID。
    private List<int> Leaves = new List<int>();
    // 保存所有已发送的 tick 消息。
    private List<ServerTickMessage> SentTickMessages = new List<ServerTickMessage>();
    // 用于检测不同步（desync）的工具。
    private DesyncDetector DesyncDetector;
    // 下一个客户端的唯一 ID。
    private int NextClientID;
    // 游戏描述步骤，用于同步游戏描述。
    private int GameDescriptionStep;
    // 游戏描述的字节数据。
    private byte[] GameDescriptionBytes;
    // 下一次 tick 的时间。
    private double NextTickTime;
    // 下一个描述请求的时间戳。
    private double NextDescriptionRequestTime;
    // 下一个描述请求的索引。
    private int NextDescriptionRequestIndex;
    // 内部属性：同步检测模式。
    internal DesyncDetectionMode DesyncDetectionMode { get; private set; }
    // 内部属性：同步检测周期。
    internal int DesyncDetectionPeriod { get; private set; }
    /// <summary>
    /// ServerGame 所属的 Server 对象。
    /// </summary>
    public Server Server { get; }
    /// <summary>
    /// 游戏的唯一 ID。
    /// </summary>
    public int GameID { get; }
    /// <summary>
    /// 当前的游戏 tick。
    /// </summary>
    public int Tick { get; private set; }
    /// <summary>
    /// 获取只读的客户端列表。
    /// </summary>
    public IReadOnlyList<ServerClient> Clients => ServerClients;
    //// ServerGame 的构造函数，初始化游戏的基本信息。
    internal ServerGame(Server server, PeerData creatorPeerData, int gameID, ClientCreateGameRequestMessage message)
	{
		Server = server;
		GameID = gameID;
		GameDescriptionBytes = message.GameDescriptionBytes;
        // 创建第一个客户端（创建游戏的客户端）。
        ServerClients.Add(new ServerClient(this, creatorPeerData, NextClientID++, message.ClientName));
        // 初始化同步检测设置。
        DesyncDetectionMode = server.Settings.DesyncDetectionMode;
		DesyncDetectionPeriod = server.Settings.DesyncDetectionPeriod;
		DesyncDetector = new DesyncDetector(this);
	}
    // 处理客户端请求加入游戏的消息。
    internal void Handle(ClientJoinGameRequestMessage message, PeerData peerData)
	{
		JoinRequest item = new JoinRequest(peerData, NextClientID++, message.ClientName, message.JoinRequestBytes);
		JoinRequests.Add(item);
	}
    // 处理客户端接受其他客户端加入游戏的消息。
    internal void Handle(ClientJoinGameAcceptedMessage message, ServerClient serverClient)
	{
		JoinRequest joinRequest = JoinRequests.FirstOrDefault((JoinRequest r) => object.Equals(r.ClientID, message.ClientID));
		if (joinRequest != null)
		{
			if (joinRequest.RefusedBy != null)
			{
				throw new ProtocolViolationException($"Join game accept from {serverClient.PeerData.Address} for client {message.ClientID} \"{joinRequest.ClientName}\" at {joinRequest.PeerData.Address}, which was already refused by {joinRequest.RefusedBy.PeerData.Address}.");
			}
			if (joinRequest.AcceptedBy == null)
			{
				joinRequest.AcceptedBy = serverClient;
                // 发送状态请求消息。
                Server.Peer.SendDataMessage(serverClient.PeerData, DeliveryMode.Reliable, Server.MessageSerializer.Write(new ServerStateRequestMessage()));
				joinRequest.NextStateRequestTime = Comm.GetTime() + 1.0;
			}
			return;
		}
		throw new ProtocolViolationException($"Join game accept from {serverClient.PeerData.Address} for non-existent join request with client ID {message.ClientID}.");
	}
    // 处理客户端拒绝其他客户端加入游戏的消息。
    internal void Handle(ClientJoinGameRefusedMessage message, ServerClient serverClient)
	{
		JoinRequest joinRequest = JoinRequests.FirstOrDefault((JoinRequest r) => object.Equals(r.ClientID, message.ClientID));
		if (joinRequest != null)
		{
			if (joinRequest.AcceptedBy != null)
			{
				throw new ProtocolViolationException($"Join game refuse from {serverClient.PeerData.Address} for client ID {message.ClientID}, which was already accepted by {joinRequest.AcceptedBy.PeerData.Address}.");
			}
			if (joinRequest.RefusedBy == null)
			{
				joinRequest.RefusedBy = serverClient;
                // 拒绝连接请求。
                Server.Peer.RefuseConnect(joinRequest.PeerData, Server.MessageSerializer.Write(new ServerConnectRefusedMessage
				{
					Reason = message.Reason
				}));
			}
			return;
		}
		throw new ProtocolViolationException($"Join game refuse from {serverClient.PeerData.Address} for non-existent client ID {message.ClientID}.");
	}
    // 处理客户端输入消息。
    internal void Handle(ClientInputMessage message, ServerClient serverClient)
	{
		serverClient.InputsBytes.Add(message.InputBytes);
	}
    // 处理客户端状态消息。
    internal void Handle(ClientStateMessage message, ServerClient serverClient)
	{
        // 遍历所有的加入请求，处理未处理的并已被接受的请求。
        foreach (JoinRequest joinRequest in JoinRequests)
		{
			if (!joinRequest.Processed && joinRequest.AcceptedBy != null)
			{
                // 计算最小 tick。
                int minimumTick = (message.Step + Server.StepsPerTick - 1) / Server.StepsPerTick;
				ServerTickMessage[] array = SentTickMessages.Where((ServerTickMessage m) => m.Tick >= minimumTick).ToArray();
				if (array.Length != 0 && array[0].Tick != minimumTick)
				{
					Server.InvokeWarning($"Not enough stored TickMessages for received state at step {message.Step}, earliest stored tick is {array[0].Tick}, tick required for this step is {minimumTick}.");
					continue;
				}
                // 接受客户端加入游戏。
                ServerClient serverClient2 = new ServerClient(this, joinRequest.PeerData, joinRequest.ClientID, joinRequest.ClientName);
				ServerClients.Add(serverClient2);
                // 发送加入接受消息。
                Server.Peer.AcceptConnect(serverClient2.PeerData, Server.MessageSerializer.Write(new ServerJoinGameAcceptedMessage
				{
					GameID = GameID,
					ClientID = serverClient2.ClientID,
					TickDuration = Server.TickDuration,
					StepsPerTick = Server.StepsPerTick,
					DesyncDetectionMode = DesyncDetectionMode,
					DesyncDetectionPeriod = DesyncDetectionPeriod,
					Step = message.Step,
					StateBytes = message.StateBytes,
					TickMessages = array
				}));
				Server.InvokeInformation($"Client \"{joinRequest.ClientName}\" at {joinRequest.PeerData.Address} joined game {GameID} at step {message.Step} (state size {message.StateBytes.Length} bytes).");
				joinRequest.Processed = true;
			}
		}
	}
    // 处理客户端同步状态的消息。
    internal void Handle(ClientDesyncStateMessage message, ServerClient serverClient)
	{
		DesyncDetector.HandleDesyncState(message.Step, message.StateBytes, message.IsDeflated, serverClient);
	}
    // 处理客户端状态哈希值的消息，用于同步检测。
    internal void Handle(ClientStateHashesMessage message, ServerClient serverClient)
	{
		DesyncDetector.HandleHashes(message.FirstHashStep, message.Hashes, serverClient);
	}
    // 处理客户端的游戏描述消息，更新游戏描述。
    internal void Handle(ClientGameDescriptionMessage message, ServerClient serverClient)
	{
		if (message.Step > GameDescriptionStep)
		{
			GameDescriptionStep = message.Step;
			GameDescriptionBytes = message.GameDescriptionBytes;
		}
	}
    // 处理客户端断开连接的操作。
    internal void HandleDisconnect(ServerClient serverClient)
	{
		Server.InvokeInformation($"Client \"{serverClient.ClientName}\" at {serverClient.PeerData.Address} disconnected from game {GameID}.");
		ServerClients.Remove(serverClient);
		serverClient.PeerData.Tag = null;
		Leaves.Add(serverClient.ClientID);
	}
    // 游戏的主循环逻辑，处理 tick 生成、请求处理等操作。
    internal double Run(double time)
	{
		double num2;
        // 如果游戏的 tick 持续时间大于 0，按 tick 逻辑运行。
        if (Server.TickDuration > 0f)
		{
			if (NextTickTime == 0.0)
			{
                // 计算下一次 tick 的时间。
                NextTickTime = CalculateNextTickTime(time);
			}
			if (time >= NextTickTime)
			{
                // 如果当前时间已经超过下一个 tick 时间，则生成并发送多个 tick 消息。
                int num = 1 + (int)Math.Min(Math.Floor((time - NextTickTime) / (double)Server.TickDuration), 10.0);
				for (int i = 0; i < num; i++)
				{
					ServerTickMessage serverTickMessage = CreateTickMessage();
					SendDataMessageToAllClients(serverTickMessage);
					SentTickMessages.Add(serverTickMessage);
					int tick = Tick + 1;
					Tick = tick;
				}
                // 更新下一次 tick 时间。
                NextTickTime = CalculateNextTickTime(time);
			}
			num2 = NextTickTime;
		}
		else
		{
            // 如果游戏是回合制，不存在 tick 持续时间，则根据配置的等待时间处理。
            ServerTickMessage serverTickMessage2 = CreateTickMessage();
			if (!serverTickMessage2.IsEmpty)
			{
				SendDataMessageToAllClients(serverTickMessage2);
				SentTickMessages.Add(serverTickMessage2);
				int tick = Tick + 1;
				Tick = tick;
			}
			num2 = time + (double)Server.Settings.TurnBasedTickWaitTime;
		}
        // 删除超时的加入请求。
        JoinRequests.RemoveAll((JoinRequest r) => time - r.RequestTime >= (double)Server.Settings.JoinRequestTimeout);
		if (ServerClients.Count > 0)
		{
            // 处理所有未完成的加入请求，继续请求客户端状态。
            foreach (JoinRequest joinRequest in JoinRequests)
			{
				if (!joinRequest.Processed && joinRequest.AcceptedBy != null && time >= joinRequest.NextStateRequestTime)
				{
					ServerClient serverClient = ServerClients[joinRequest.NextStateRequestIndex % ServerClients.Count];
					Server.Peer.SendDataMessage(serverClient.PeerData, DeliveryMode.Reliable, Server.MessageSerializer.Write(new ServerStateRequestMessage()));
					joinRequest.NextStateRequestIndex++;
					joinRequest.NextStateRequestTime = time + (double)Server.Settings.StateRequestPeriod;
					num2 = Math.Min(num2, joinRequest.NextStateRequestTime);
				}
			}
            // 如果没有描述请求，初始化时间。
            if (NextDescriptionRequestTime == 0.0)
			{
				NextDescriptionRequestTime = Server.Settings.GameDescriptionRequestPeriod;
			}
            // 请求客户端的游戏描述信息。
            if (time >= NextDescriptionRequestTime)
			{
				ServerClient serverClient2 = ServerClients[NextDescriptionRequestIndex % ServerClients.Count];
				Server.Peer.SendDataMessage(serverClient2.PeerData, DeliveryMode.Reliable, Server.MessageSerializer.Write(new ServerGameDescriptionRequestMessage()));
				NextDescriptionRequestTime = time + (double)Server.Settings.GameDescriptionRequestPeriod;
				NextDescriptionRequestIndex++;
			}
			num2 = Math.Min(num2, NextDescriptionRequestTime);
		}
        // 移除过期的 tick 消息。
        double earliestTimeToKeep = time - (double)Server.Settings.JoinRequestTimeout;
		int num3 = SentTickMessages.FindIndex((ServerTickMessage m) => m.SentTime >= earliestTimeToKeep);
		if (num3 > 0)
		{
			SentTickMessages.RemoveRange(0, num3);
		}
        // 检测是否有不同步（desync）事件。
        DesyncDetector.Run();
		return num2;
	}
    // 创建游戏描述对象。
    internal GameDescription CreateGameDescription()
	{
		return new GameDescription
		{
			GameID = GameID,
			Step = GameDescriptionStep,
			ClientsCount = ServerClients.Count,
			GameDescriptionBytes = GameDescriptionBytes
		};
	}
    // 创建一个新的 tick 消息，包含客户端的输入和状态变化。
    private ServerTickMessage CreateTickMessage()
	{
		ServerTickMessage serverTickMessage = new ServerTickMessage
		{
			Tick = Tick,
			DesyncDetectedStep = DesyncDetector.DesyncDetectedStep,
			ClientsTickData = new List<ServerTickMessage.ClientTickData>()
		};
        // 处理所有客户端的输入。
        foreach (ServerClient serverClient in ServerClients)
		{
			if (serverClient.InputsBytes.Count > 0)
			{
				serverTickMessage.ClientsTickData.Add(new ServerTickMessage.ClientTickData
				{
					ClientID = serverClient.ClientID,
					InputsBytes = serverClient.InputsBytes.ToList()
				});
				serverClient.InputsBytes.Clear();
			}
		}
        // 处理所有未转发的加入请求。
        foreach (JoinRequest joinRequest in JoinRequests)
		{
			if (!joinRequest.Forwarded)
			{
				serverTickMessage.ClientsTickData.Add(new ServerTickMessage.ClientTickData
				{
					ClientID = joinRequest.ClientID,
					JoinAddress = joinRequest.PeerData.Address,
					JoinBytes = joinRequest.JoinRequestBytes
				});
				joinRequest.Forwarded = true;
			}
		}
        // 处理所有离开的客户端。
        foreach (int leaf in Leaves)
		{
			serverTickMessage.ClientsTickData.Add(new ServerTickMessage.ClientTickData
			{
				ClientID = leaf,
				Leave = true
			});
		}
		Leaves.Clear();
		return serverTickMessage;
	}
    // 将消息发送给所有已连接的客户端。
    internal void SendDataMessageToAllClients(Message message)
	{
		byte[] bytes = Server.MessageSerializer.Write(message);
		foreach (ServerClient serverClient in ServerClients)
		{
			Server.Peer.SendDataMessage(serverClient.PeerData, DeliveryMode.ReliableSequenced, bytes);
		}
	}
    // 计算下一次 tick 的时间。
    private double CalculateNextTickTime(double time)
	{
		return Math.Floor(time / (double)Server.TickDuration + 1.0) * (double)Server.TickDuration;
	}
}
