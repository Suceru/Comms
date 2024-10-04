using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Comms.Drt;
/// <summary>
///Explorer �������ڷ��֡����������Ϸ������ͨ�ŵ������
///������ͨ�����ع㲥������Ѱ����Ϸ������������������Щ����������Ϣ������
///ʵ���� IDisposable �ӿ���ȷ����Դ����ȷ�ͷš�
/// </summary>
public class Explorer : IDisposable
{
    // �ڲ��� DnsCache ���ڻ��� DNS ��ѯ����������ظ���ѯ��
    private class DnsCache
	{
        // �ֵ�洢 DNS ��ѯ���棬key Ϊ��������value Ϊ IP ��ַ�б�
        private Dictionary<string, IPAddress[]> Cache = new Dictionary<string, IPAddress[]>();

        // ������������ѯ IP ��ַ���档��������д����򷵻أ����򷵻� null��
        public IPAddress[] Query(string host)
		{
			lock (Cache)
			{
				Cache.TryGetValue(host, out var value);
				return value;
			}
		}
        // ���������Ͷ�Ӧ�� IP ��ַ������ӵ������С�
        public void Add(string host, IPAddress[] addresses)
		{
			lock (Cache)
			{
				Cache[host] = addresses;
			}
		}
        // ��ջ��档
        public void Clear()
		{
			lock (Cache)
			{
				Cache.Clear();
			}
		}
	}

	private bool IsDisposed;// ָʾ�����Ƿ��ѱ��ͷš�

    private int[] ServerPorts;// �������˿��б�

    private bool LocalBroadcast;// �Ƿ����ñ��ع㲥�Է��־������еķ�������

    private string[] InternetHosts;// ��Ҫͨ�����������ֵķ������������б�

    private double LocalLastTime;// �ϴα��ط��������ֵ�ʱ�䡣

    private double InternetLastTime;// �ϴλ��������������ֵ�ʱ�䡣

    private Dictionary<IPEndPoint, double> InternetRequestTimes = new Dictionary<IPEndPoint, double>();// ��¼�����������ʱ�䡣

    private Alarm Alarm;// ��ʱ���������ڴ�������������

    private DnsCache Cache = new DnsCache();// DNS ���档

    private List<ServerDescription> ServersList = new List<ServerDescription>();// �ѷ��ֵķ������б�

    private IReadOnlyList<ServerDescription> ServersReadonlyList;// ֻ���ķ������б��档

    internal MessageSerializer MessageSerializer;// �������л��ͷ����л���Ϣ�Ķ���

    /// <summary>
    /// ֻ�����ԣ���Ϸ���� ID���� MessageSerializer �ṩ��
    /// </summary>
    public int GameTypeID => MessageSerializer.GameTypeID;
    /// <summary>
    /// ֻ�����ԣ���� Explorer ʵ�������� Peer ���󣬹���ͨ�š�
    /// </summary>
    public Peer Peer { get; }
    /// <summary>
    /// ֻ�����ԣ��� Explorer ʵ���� IP �˵㡣
    /// </summary>
    public IPEndPoint Address => Peer.Address;
    /// <summary>
    /// ֻ�����ԣ�Explorer �����ö����ṩ��ص����ò�����
    /// </summary>
    public ExplorerSettings Settings { get; } = new ExplorerSettings();

    /// <summary>
    /// ֻ�����ԣ�ָʾ�Ƿ��Ѿ������˷������������̡�
    /// </summary>
    public bool IsDiscoveryStarted => Alarm != null;
    /// <summary>
    /// ֻ�����ԣ��ѷ��ֵķ������б��̰߳�ȫ����
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
    /// ��Դ�����¼��������յ���������Դʱ������
    /// </summary>
    public event Action<ResourceData> ResourceReceived;
    /// <summary>
    /// �����¼����������쳣ʱ������
    /// </summary>
    public event Action<Exception> Error;
    /// <summary>
    /// �����¼����������������Ϣ��
    /// </summary>
    public event Action<string> Debug;
    /// <summary>
    /// �����������¼����������µķ�����ʱ������
    /// </summary>
    public event Action<ServerDescription> ServerDiscovered;
    /// <summary>
    /// ���캯�������ݸ�������Ϸ���� ID �ͷ������˿ڣ����� Explorer ʵ����
    /// </summary>
    /// <param name="gameTypeID">��Ϸ���� ID </param>
    /// <param name="serverPort">�������˿�</param>
    /// <param name="localPort">���ض˿�</param>
    public Explorer(int gameTypeID, int serverPort, int localPort = 0)
		: this(gameTypeID, new int[1] { serverPort }, localPort)
	{
	}
    /// <summary>
    /// ���캯����֧�ֶ���������˿ڡ�
    /// </summary>
    /// <param name="gameTypeID">��Ϸ���� ID</param>
    /// <param name="serverPorts">����������˿�</param>
    /// <param name="localPort">���ض˿�</param>
    public Explorer(int gameTypeID, IEnumerable<int> serverPorts, int localPort = 0)
		: this(gameTypeID, serverPorts, new UdpTransmitter(localPort))
	{
	}
    /// <summary>
    /// ���캯����֧���Զ��崫������Transmitter������ UDP��
    /// </summary>
    /// <param name="gameTypeID">��Ϸ���� ID</param>
    /// <param name="serverPort">�������˿�</param>
    /// <param name="transmitter">������</param>
    public Explorer(int gameTypeID, int serverPort, ITransmitter transmitter)
		: this(gameTypeID, new int[1] { serverPort }, transmitter)
	{
	}
    /// <summary>
    /// �����캯������ʼ����Ϸ���� ID���������˿ںʹ������������� Peer��
    /// </summary>
    /// <param name="gameTypeID">��Ϸ���� ID</param>
    /// <param name="serverPorts">����������˿�</param>
    /// <param name="transmitter">������</param>
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
    /// ʵ�� IDisposable �ӿڣ��ͷ���Դ��
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
    /// ��ʼ�������������̣�֧�ֱ��ع㲥�ͻ������������֡�
    /// </summary>
    /// <param name="localBroadcast">���ع㲥</param>
    /// <param name="internetHosts">����������</param>
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
    /// ֹͣ�������������̣���շ������б�
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
    /// ��ָ��������������Դ��
    /// </summary>
    /// <param name="serverAddress">��������ַ</param>
    /// <param name="name">����</param>
    /// <param name="minimumVersion">��С�汾</param>
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
    /// ʹ���������Ͷ˿��������������Դ���첽ִ�� DNS ��ѯ��
    /// </summary>
    /// <param name="host">����</param>
    /// <param name="port">�˿�</param>
    /// <param name="name">����</param>
    /// <param name="minimumVersion">��С�汾</param>
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
    // ��ʱ���Ļص������������Ե�ִ�б��غͻ�������������������
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
    // ִ�б��ط��������֣�ͨ���㲥��������еķ��������ͷ�������
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
    // ִ�л��������������֣���ָ�������������ͷ�������
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
    // �������Ƿ��ѱ��ͷţ�������ͷ����׳��쳣��
    private void CheckNotDisposed()
	{
		if (Peer == null)
		{
			throw new ObjectDisposedException("Server");
		}
	}
    // DNS ��ѯ������������ IP ��ַ�б���ѯ����ᱻ���档
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
    // ���������������Ӧ��Ϣ�����·������б�
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
    // �����������Դ��Ϣ������ ResourceReceived �¼���
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
    // ���������¼���
    internal void InvokeError(Exception error)
	{
		this.Error?.Invoke(error);
	}
    // ������Ϣ�����ʹ�� DEBUG Ԥ����ָ�
    [Conditional("DEBUG")]
	internal void InvokeDebug(string format, params object[] args)
	{
		if (this.Debug != null)
		{
			this.Debug?.Invoke(string.Format(format, args));
		}
	}
}
