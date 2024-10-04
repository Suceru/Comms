using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Comms.Drt;
/// <summary>
/// ServerGame �ฺ�����������˵���Ϸ״̬������ͻ��˵���������������Ϣ���Ͽ����ӵȲ�������ͬ����Ϸ��ʱ�䲽����tick����
/// </summary>
public class ServerGame
{
    // JoinRequest �� ServerGame �ڲ���һ���࣬���ڱ���ͻ��˼�����Ϸ��������Ϣ��
    private class JoinRequest
    {
		// �ͻ��˵� PeerData������������Ϣ��
        public PeerData PeerData;
        // �ͻ��˵�Ψһ ID��
        public int ClientID;
        // �ͻ��˵����ơ�
        public string ClientName;
        // ����������ֽ����ݡ�
        public byte[] JoinRequestBytes;
        // �����ʱ��������ڳ�ʱ�жϡ�
        public double RequestTime;
        // ��Ǹ������Ƿ��ѱ�ת���������ͻ��ˡ�
        public bool Forwarded;
        // ��ǽ��ܸ�����Ŀͻ��ˡ�
        public ServerClient AcceptedBy;
        // ��Ǿܾ�������Ŀͻ��ˡ�
        public ServerClient RefusedBy;
        // ��һ��״̬�����������������ѯ����״̬��
        public int NextStateRequestIndex;
        // ��һ��״̬�����ʱ�����
        public double NextStateRequestTime;
        // ��Ǹ������Ƿ��Ѵ���
        public bool Processed;
        // ���캯����ʼ�� JoinRequest ����
        public JoinRequest(PeerData peerData, int clientID, string clientName, byte[] joinRequestBytes)
		{
			PeerData = peerData;
			ClientID = clientID;
			ClientName = clientName;
			JoinRequestBytes = joinRequestBytes;
			RequestTime = Comm.GetTime();// ��ȡ��ǰʱ��
            NextStateRequestIndex = clientID;// ��ʼ��״̬��������
        }
	}
    // ���浱ǰ�����ӵĿͻ����б�
    private List<ServerClient> ServerClients = new List<ServerClient>();

    // �������еļ�������
    private List<JoinRequest> JoinRequests = new List<JoinRequest>();

    // �������е����߿ͻ��� ID��
    private List<int> Leaves = new List<int>();
    // ���������ѷ��͵� tick ��Ϣ��
    private List<ServerTickMessage> SentTickMessages = new List<ServerTickMessage>();
    // ���ڼ�ⲻͬ����desync���Ĺ��ߡ�
    private DesyncDetector DesyncDetector;
    // ��һ���ͻ��˵�Ψһ ID��
    private int NextClientID;
    // ��Ϸ�������裬����ͬ����Ϸ������
    private int GameDescriptionStep;
    // ��Ϸ�������ֽ����ݡ�
    private byte[] GameDescriptionBytes;
    // ��һ�� tick ��ʱ�䡣
    private double NextTickTime;
    // ��һ�����������ʱ�����
    private double NextDescriptionRequestTime;
    // ��һ�����������������
    private int NextDescriptionRequestIndex;
    // �ڲ����ԣ�ͬ�����ģʽ��
    internal DesyncDetectionMode DesyncDetectionMode { get; private set; }
    // �ڲ����ԣ�ͬ��������ڡ�
    internal int DesyncDetectionPeriod { get; private set; }
    /// <summary>
    /// ServerGame ������ Server ����
    /// </summary>
    public Server Server { get; }
    /// <summary>
    /// ��Ϸ��Ψһ ID��
    /// </summary>
    public int GameID { get; }
    /// <summary>
    /// ��ǰ����Ϸ tick��
    /// </summary>
    public int Tick { get; private set; }
    /// <summary>
    /// ��ȡֻ���Ŀͻ����б�
    /// </summary>
    public IReadOnlyList<ServerClient> Clients => ServerClients;
    //// ServerGame �Ĺ��캯������ʼ����Ϸ�Ļ�����Ϣ��
    internal ServerGame(Server server, PeerData creatorPeerData, int gameID, ClientCreateGameRequestMessage message)
	{
		Server = server;
		GameID = gameID;
		GameDescriptionBytes = message.GameDescriptionBytes;
        // ������һ���ͻ��ˣ�������Ϸ�Ŀͻ��ˣ���
        ServerClients.Add(new ServerClient(this, creatorPeerData, NextClientID++, message.ClientName));
        // ��ʼ��ͬ��������á�
        DesyncDetectionMode = server.Settings.DesyncDetectionMode;
		DesyncDetectionPeriod = server.Settings.DesyncDetectionPeriod;
		DesyncDetector = new DesyncDetector(this);
	}
    // ����ͻ������������Ϸ����Ϣ��
    internal void Handle(ClientJoinGameRequestMessage message, PeerData peerData)
	{
		JoinRequest item = new JoinRequest(peerData, NextClientID++, message.ClientName, message.JoinRequestBytes);
		JoinRequests.Add(item);
	}
    // ����ͻ��˽��������ͻ��˼�����Ϸ����Ϣ��
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
                // ����״̬������Ϣ��
                Server.Peer.SendDataMessage(serverClient.PeerData, DeliveryMode.Reliable, Server.MessageSerializer.Write(new ServerStateRequestMessage()));
				joinRequest.NextStateRequestTime = Comm.GetTime() + 1.0;
			}
			return;
		}
		throw new ProtocolViolationException($"Join game accept from {serverClient.PeerData.Address} for non-existent join request with client ID {message.ClientID}.");
	}
    // ����ͻ��˾ܾ������ͻ��˼�����Ϸ����Ϣ��
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
                // �ܾ���������
                Server.Peer.RefuseConnect(joinRequest.PeerData, Server.MessageSerializer.Write(new ServerConnectRefusedMessage
				{
					Reason = message.Reason
				}));
			}
			return;
		}
		throw new ProtocolViolationException($"Join game refuse from {serverClient.PeerData.Address} for non-existent client ID {message.ClientID}.");
	}
    // ����ͻ���������Ϣ��
    internal void Handle(ClientInputMessage message, ServerClient serverClient)
	{
		serverClient.InputsBytes.Add(message.InputBytes);
	}
    // ����ͻ���״̬��Ϣ��
    internal void Handle(ClientStateMessage message, ServerClient serverClient)
	{
        // �������еļ������󣬴���δ����Ĳ��ѱ����ܵ�����
        foreach (JoinRequest joinRequest in JoinRequests)
		{
			if (!joinRequest.Processed && joinRequest.AcceptedBy != null)
			{
                // ������С tick��
                int minimumTick = (message.Step + Server.StepsPerTick - 1) / Server.StepsPerTick;
				ServerTickMessage[] array = SentTickMessages.Where((ServerTickMessage m) => m.Tick >= minimumTick).ToArray();
				if (array.Length != 0 && array[0].Tick != minimumTick)
				{
					Server.InvokeWarning($"Not enough stored TickMessages for received state at step {message.Step}, earliest stored tick is {array[0].Tick}, tick required for this step is {minimumTick}.");
					continue;
				}
                // ���ܿͻ��˼�����Ϸ��
                ServerClient serverClient2 = new ServerClient(this, joinRequest.PeerData, joinRequest.ClientID, joinRequest.ClientName);
				ServerClients.Add(serverClient2);
                // ���ͼ��������Ϣ��
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
    // ����ͻ���ͬ��״̬����Ϣ��
    internal void Handle(ClientDesyncStateMessage message, ServerClient serverClient)
	{
		DesyncDetector.HandleDesyncState(message.Step, message.StateBytes, message.IsDeflated, serverClient);
	}
    // ����ͻ���״̬��ϣֵ����Ϣ������ͬ����⡣
    internal void Handle(ClientStateHashesMessage message, ServerClient serverClient)
	{
		DesyncDetector.HandleHashes(message.FirstHashStep, message.Hashes, serverClient);
	}
    // ����ͻ��˵���Ϸ������Ϣ��������Ϸ������
    internal void Handle(ClientGameDescriptionMessage message, ServerClient serverClient)
	{
		if (message.Step > GameDescriptionStep)
		{
			GameDescriptionStep = message.Step;
			GameDescriptionBytes = message.GameDescriptionBytes;
		}
	}
    // ����ͻ��˶Ͽ����ӵĲ�����
    internal void HandleDisconnect(ServerClient serverClient)
	{
		Server.InvokeInformation($"Client \"{serverClient.ClientName}\" at {serverClient.PeerData.Address} disconnected from game {GameID}.");
		ServerClients.Remove(serverClient);
		serverClient.PeerData.Tag = null;
		Leaves.Add(serverClient.ClientID);
	}
    // ��Ϸ����ѭ���߼������� tick ���ɡ�������Ȳ�����
    internal double Run(double time)
	{
		double num2;
        // �����Ϸ�� tick ����ʱ����� 0���� tick �߼����С�
        if (Server.TickDuration > 0f)
		{
			if (NextTickTime == 0.0)
			{
                // ������һ�� tick ��ʱ�䡣
                NextTickTime = CalculateNextTickTime(time);
			}
			if (time >= NextTickTime)
			{
                // �����ǰʱ���Ѿ�������һ�� tick ʱ�䣬�����ɲ����Ͷ�� tick ��Ϣ��
                int num = 1 + (int)Math.Min(Math.Floor((time - NextTickTime) / (double)Server.TickDuration), 10.0);
				for (int i = 0; i < num; i++)
				{
					ServerTickMessage serverTickMessage = CreateTickMessage();
					SendDataMessageToAllClients(serverTickMessage);
					SentTickMessages.Add(serverTickMessage);
					int tick = Tick + 1;
					Tick = tick;
				}
                // ������һ�� tick ʱ�䡣
                NextTickTime = CalculateNextTickTime(time);
			}
			num2 = NextTickTime;
		}
		else
		{
            // �����Ϸ�ǻغ��ƣ������� tick ����ʱ�䣬��������õĵȴ�ʱ�䴦��
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
        // ɾ����ʱ�ļ�������
        JoinRequests.RemoveAll((JoinRequest r) => time - r.RequestTime >= (double)Server.Settings.JoinRequestTimeout);
		if (ServerClients.Count > 0)
		{
            // ��������δ��ɵļ������󣬼�������ͻ���״̬��
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
            // ���û���������󣬳�ʼ��ʱ�䡣
            if (NextDescriptionRequestTime == 0.0)
			{
				NextDescriptionRequestTime = Server.Settings.GameDescriptionRequestPeriod;
			}
            // ����ͻ��˵���Ϸ������Ϣ��
            if (time >= NextDescriptionRequestTime)
			{
				ServerClient serverClient2 = ServerClients[NextDescriptionRequestIndex % ServerClients.Count];
				Server.Peer.SendDataMessage(serverClient2.PeerData, DeliveryMode.Reliable, Server.MessageSerializer.Write(new ServerGameDescriptionRequestMessage()));
				NextDescriptionRequestTime = time + (double)Server.Settings.GameDescriptionRequestPeriod;
				NextDescriptionRequestIndex++;
			}
			num2 = Math.Min(num2, NextDescriptionRequestTime);
		}
        // �Ƴ����ڵ� tick ��Ϣ��
        double earliestTimeToKeep = time - (double)Server.Settings.JoinRequestTimeout;
		int num3 = SentTickMessages.FindIndex((ServerTickMessage m) => m.SentTime >= earliestTimeToKeep);
		if (num3 > 0)
		{
			SentTickMessages.RemoveRange(0, num3);
		}
        // ����Ƿ��в�ͬ����desync���¼���
        DesyncDetector.Run();
		return num2;
	}
    // ������Ϸ��������
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
    // ����һ���µ� tick ��Ϣ�������ͻ��˵������״̬�仯��
    private ServerTickMessage CreateTickMessage()
	{
		ServerTickMessage serverTickMessage = new ServerTickMessage
		{
			Tick = Tick,
			DesyncDetectedStep = DesyncDetector.DesyncDetectedStep,
			ClientsTickData = new List<ServerTickMessage.ClientTickData>()
		};
        // �������пͻ��˵����롣
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
        // ��������δת���ļ�������
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
        // ���������뿪�Ŀͻ��ˡ�
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
    // ����Ϣ���͸����������ӵĿͻ��ˡ�
    internal void SendDataMessageToAllClients(Message message)
	{
		byte[] bytes = Server.MessageSerializer.Write(message);
		foreach (ServerClient serverClient in ServerClients)
		{
			Server.Peer.SendDataMessage(serverClient.PeerData, DeliveryMode.ReliableSequenced, bytes);
		}
	}
    // ������һ�� tick ��ʱ�䡣
    private double CalculateNextTickTime(double time)
	{
		return Math.Floor(time / (double)Server.TickDuration + 1.0) * (double)Server.TickDuration;
	}
}
