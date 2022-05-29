using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

namespace Comms;

public class Peer : IDisposable
{
	internal abstract class Message
	{
		private static Dictionary<int, Type> MessageTypesByMessageId;

		private static Dictionary<Type, int> MessageIdsByMessageTypes;

		static Message()
		{
			MessageTypesByMessageId = new Dictionary<int, Type>();
			MessageIdsByMessageTypes = new Dictionary<Type, int>();
			TypeInfo[] array = (from t in typeof(Message).GetTypeInfo().get_Assembly().DefinedTypes
				where typeof(Message).GetTypeInfo().IsAssignableFrom(t)
				orderby t.Name
				select t).ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				MessageTypesByMessageId[i] = array[i].AsType();
				MessageIdsByMessageTypes[array[i].AsType()] = i;
			}
		}

		public static Message Read(byte[] bytes)
		{
			BinaryReader binaryReader = new BinaryReader(new MemoryStream(bytes));
			byte key = binaryReader.ReadByte();
			if (MessageTypesByMessageId.TryGetValue(key, out var value))
			{
				Message obj = (Message)Activator.CreateInstance(value);
				obj.Read(binaryReader);
				return obj;
			}
			return null;
		}

		public static byte[] Write(Message message)
		{
			BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
			binaryWriter.Write((byte)MessageIdsByMessageTypes[message.GetType()]);
			message.Write(binaryWriter);
			return ((MemoryStream)binaryWriter.BaseStream).ToArray();
		}

		public static void Handle(Peer peer, Packet packet)
		{
			Message message = Read(packet.Data);
			if (message != null)
			{
				message.Handle(peer, packet.Address);
				return;
			}
			throw new InvalidOperationException("Unrecognized message.");
		}

		protected abstract void Read(BinaryReader reader);

		protected abstract void Write(BinaryWriter writer);

		protected virtual void Handle(Peer peer, IPEndPoint address)
		{
		}

		protected static IPEndPoint ReadAddress(BinaryReader reader)
		{
			byte family = reader.ReadByte();
			byte b = reader.ReadByte();
			byte[] array = reader.ReadBytes(b);
			SocketAddress socketAddress = new SocketAddress((AddressFamily)family, b);
			for (int i = 0; i < array.Length; i++)
			{
				socketAddress[i] = array[i];
			}
			return (IPEndPoint)new IPEndPoint((socketAddress.Family == AddressFamily.InterNetworkV6) ? IPAddress.IPv6Any : IPAddress.Any, 0).Create(socketAddress);
		}

		protected static void WriteAddress(BinaryWriter writer, IPEndPoint address)
		{
			SocketAddress socketAddress = address.Serialize();
			byte[] array = new byte[socketAddress.Size];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = socketAddress[i];
			}
			writer.Write((byte)socketAddress.Family);
			writer.Write((byte)array.Length);
			writer.Write(array);
		}
	}

	internal class DiscoveryRequestMessage : Message
	{
		public byte[] DiscoveryRequestData;

		protected override void Read(BinaryReader reader)
		{
			DiscoveryRequestData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
		}

		protected override void Write(BinaryWriter writer)
		{
			writer.Write(DiscoveryRequestData);
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			if (peer.PeerDiscoveryRequest != null)
			{
				peer.PeerDiscoveryRequest(new Packet(address, DiscoveryRequestData));
			}
			else
			{
				peer.RespondToDiscovery(address, DeliveryMode.Raw);
			}
		}
	}

	internal class DiscoveryResponseMessage : Message
	{
		public byte[] DiscoveryResponseData;

		protected override void Read(BinaryReader reader)
		{
			DiscoveryResponseData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
		}

		protected override void Write(BinaryWriter writer)
		{
			writer.Write(DiscoveryResponseData);
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			peer.PeerDiscovered?.Invoke(new Packet(address, DiscoveryResponseData));
		}
	}

	internal class ConnectRequestMessage : Message
	{
		public byte[] ConnectRequestData;

		protected override void Read(BinaryReader reader)
		{
			ConnectRequestData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
		}

		protected override void Write(BinaryWriter writer)
		{
			writer.Write(ConnectRequestData);
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			if (peer.FindPeer(address) == null)
			{
				PeerData peerData = new PeerData(address);
				if (peer.ConnectRequest != null)
				{
					peer.ConnectRequest(new PeerPacket(peerData, ConnectRequestData));
				}
				else
				{
					peer.AcceptConnect(peerData);
				}
				return;
			}
			throw new InvalidOperationException("Connect request ignored, peer already connected.");
		}
	}

	internal class ConnectAcceptedMessage : Message
	{
		public IPEndPoint[] PeersAddresses;

		public byte[] ConnectAcceptedData;

		protected override void Read(BinaryReader reader)
		{
			int num = reader.ReadInt16();
			PeersAddresses = new IPEndPoint[num];
			for (int i = 0; i < num; i++)
			{
				PeersAddresses[i] = Message.ReadAddress(reader);
			}
			ConnectAcceptedData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
		}

		protected override void Write(BinaryWriter writer)
		{
			writer.Write((short)PeersAddresses.Length);
			for (int i = 0; i < PeersAddresses.Length; i++)
			{
				Message.WriteAddress(writer, PeersAddresses[i]);
			}
			writer.Write(ConnectAcceptedData);
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			if (peer.ConnectedTo != null)
			{
				throw new InvalidOperationException("Unexpected connection accept ignored, peer already connected.");
			}
			if (!object.Equals(peer.ConnectingTo, address))
			{
				throw new InvalidOperationException($"Unexpected connection accept from {address} ignored.");
			}
			peer.ConnectedTo = new PeerData(address);
			peer.ConnectingTo = null;
			peer.PeersByAddress.Clear();
			IPEndPoint[] peersAddresses = PeersAddresses;
			foreach (IPEndPoint iPEndPoint in peersAddresses)
			{
				peer.PeersByAddress.Add(iPEndPoint, new PeerData(iPEndPoint));
			}
			peer.ConnectAccepted?.Invoke(new PeerPacket(peer.ConnectedTo, ConnectAcceptedData));
		}
	}

	internal class ConnectRefusedMessage : Message
	{
		public byte[] ConnectRefusedData;

		protected override void Read(BinaryReader reader)
		{
			ConnectRefusedData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
		}

		protected override void Write(BinaryWriter writer)
		{
			writer.Write(ConnectRefusedData);
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			if (peer.ConnectedTo != null || !object.Equals(peer.ConnectingTo, address))
			{
				throw new InvalidOperationException("Unexpected connection refuse ignored.");
			}
			peer.ConnectingTo = null;
			peer.ConnectRefused?.Invoke(new Packet(address, ConnectRefusedData));
		}
	}

	internal class DisconnectRequestMessage : Message
	{
		protected override void Read(BinaryReader reader)
		{
		}

		protected override void Write(BinaryWriter writer)
		{
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			PeerData peerData = peer.FindPeer(address);
			if (peerData == null)
			{
				return;
			}
			peer.PeersByAddress.Remove(address);
			if (peer.Settings.SendPeerConnectDisconnectNotifications)
			{
				foreach (PeerData value in peer.PeersByAddress.Values)
				{
					peer.InternalSend(value.Address, DeliveryMode.ReliableSequenced, new PeerDisconnectedMessage
					{
						PeerAddress = address
					});
				}
			}
			peer.PeerDisconnected?.Invoke(peerData);
		}
	}

	internal class DisconnectedMessage : Message
	{
		protected override void Read(BinaryReader reader)
		{
		}

		protected override void Write(BinaryWriter writer)
		{
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			if (peer.ConnectedTo != null)
			{
				peer.InternalDisconnect();
			}
		}
	}

	internal class PeerConnectedMessage : Message
	{
		public IPEndPoint PeerAddress;

		protected override void Read(BinaryReader reader)
		{
			PeerAddress = Message.ReadAddress(reader);
		}

		protected override void Write(BinaryWriter writer)
		{
			Message.WriteAddress(writer, PeerAddress);
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			if (peer.ConnectedTo != null)
			{
				if (peer.FindPeer(address) != null)
				{
					throw new InvalidOperationException("Peer connection notification ignored, peer already connected.");
				}
				PeerData peerData = new PeerData(PeerAddress);
				peer.PeersByAddress.Add(PeerAddress, peerData);
				peer.PeerConnected?.Invoke(peerData);
			}
		}
	}

	internal class PeerDisconnectedMessage : Message
	{
		public IPEndPoint PeerAddress;

		protected override void Read(BinaryReader reader)
		{
			PeerAddress = Message.ReadAddress(reader);
		}

		protected override void Write(BinaryWriter writer)
		{
			Message.WriteAddress(writer, PeerAddress);
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			if (peer.ConnectedTo != null)
			{
				PeerData peerData = peer.FindPeer(PeerAddress);
				if (peerData != null)
				{
					peer.PeersByAddress.Remove(PeerAddress);
					peer.PeerDisconnected?.Invoke(peerData);
				}
			}
		}
	}

	internal class KeepAliveRequestMessage : Message
	{
		public double RequestSendTime;

		protected override void Read(BinaryReader reader)
		{
			RequestSendTime = reader.ReadDouble();
		}

		protected override void Write(BinaryWriter writer)
		{
			writer.Write(RequestSendTime);
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			PeerData peerData = ((peer.ConnectedTo != null && object.Equals(address, peer.ConnectedTo.Address)) ? peer.ConnectedTo : peer.FindPeer(address));
			if (peerData != null)
			{
				peerData.LastKeepAliveReceiveTime = Comm.GetTime();
				peer.InternalSend(address, DeliveryMode.Unreliable, new KeepAliveResponseMessage
				{
					RequestSendTime = RequestSendTime
				});
			}
		}
	}

	internal class KeepAliveResponseMessage : Message
	{
		public double RequestSendTime;

		protected override void Read(BinaryReader reader)
		{
			RequestSendTime = reader.ReadDouble();
		}

		protected override void Write(BinaryWriter writer)
		{
			writer.Write(RequestSendTime);
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			PeerData peerData = ((peer.ConnectedTo != null && object.Equals(address, peer.ConnectedTo.Address)) ? peer.ConnectedTo : peer.FindPeer(address));
			if (peerData != null)
			{
				double time = Comm.GetTime();
				peerData.NextKeepAliveSendTime = time + (double)peer.Settings.KeepAlivePeriod;
				peerData.Ping = (float)(time - RequestSendTime);
			}
		}
	}

	internal class DataMessage : Message
	{
		public byte[] Bytes;

		protected override void Read(BinaryReader reader)
		{
			Bytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
		}

		protected override void Write(BinaryWriter writer)
		{
			writer.Write(Bytes);
		}

		protected override void Handle(Peer peer, IPEndPoint address)
		{
			if (peer.ConnectedTo != null && object.Equals(address, peer.ConnectedTo.Address))
			{
				peer.DataMessageReceived?.Invoke(new PeerPacket(peer.ConnectedTo, Bytes));
				return;
			}
			PeerData peerData = peer.FindPeer(address);
			if (peerData != null)
			{
				peer.DataMessageReceived?.Invoke(new PeerPacket(peerData, Bytes));
			}
		}
	}

	private volatile bool IsDisposed;

	private Task Task;

	private double ConnectStartTime;

	private Dictionary<IPEndPoint, PeerData> PeersByAddress = new Dictionary<IPEndPoint, PeerData>();

	public object Lock { get; } = new object();


	public Comm Comm { get; private set; }

	public PeerSettings Settings { get; } = new PeerSettings();


	public IPEndPoint Address => Comm.Address;

	public PeerData ConnectedTo { get; private set; }

	public IPEndPoint ConnectingTo { get; private set; }

	public IReadOnlyList<PeerData> Peers
	{
		get
		{
			lock (Lock)
			{
				return PeersByAddress.Values.ToArray();
			}
		}
	}

	public event Action<Exception> Error;

	public event Action<Packet> PeerDiscoveryRequest;

	public event Action<Packet> PeerDiscovered;

	public event Action<PeerPacket> ConnectRequest;

	public event Action<PeerPacket> ConnectAccepted;

	public event Action<Packet> ConnectRefused;

	public event Action<IPEndPoint> ConnectTimedOut;

	public event Action Disconnected;

	public event Action<PeerData> PeerConnected;

	public event Action<PeerData> PeerDisconnected;

	public event Action<PeerPacket> DataMessageReceived;

	public Peer(int localPort = 0)
		: this(new UdpPacketTransmitter(localPort))
	{
	}

	public Peer(IPacketTransmitter transmitter)
	{
		Comm = new Comm(transmitter);
		Comm.Error += delegate(Exception e)
		{
			this.Error?.Invoke(e);
		};
		Comm.Received += delegate(Packet packet)
		{
			try
			{
				lock (Lock)
				{
					if (!IsDisposed)
					{
						Message.Handle(this, packet);
					}
				}
			}
			catch (Exception obj)
			{
				this.Error?.Invoke(obj);
			}
		};
		Task = Task.Factory.StartNew(ThreadFunction, TaskCreationOptions.LongRunning);
	}

	public void Dispose()
	{
		if (IsDisposed)
		{
			return;
		}
		lock (Lock)
		{
			IsDisposed = true;
		}
		Task.Wait();
		lock (Lock)
		{
			if (Comm != null)
			{
				Comm.Dispose();
				Comm = null;
			}
		}
	}

	public PeerData FindPeer(IPEndPoint address)
	{
		CheckNotDisposed();
		lock (Lock)
		{
			PeerData value;
			return PeersByAddress.TryGetValue(address, out value) ? value : null;
		}
	}

	public void DiscoverLocalPeers(int peerPort, byte[] discoveryQueryData = null)
	{
		CheckNotDisposed();
		if (Comm.Address.AddressFamily == AddressFamily.InterNetworkV6)
		{
			InternalSend(new IPEndPoint(UdpPacketTransmitter.IPV6BroadcastAddress, peerPort), DeliveryMode.Raw, new DiscoveryRequestMessage
			{
				DiscoveryRequestData = (discoveryQueryData ?? new byte[0])
			});
		}
		else
		{
			InternalSend(new IPEndPoint(IPAddress.Broadcast, peerPort), DeliveryMode.Raw, new DiscoveryRequestMessage
			{
				DiscoveryRequestData = (discoveryQueryData ?? new byte[0])
			});
		}
	}

	public void DiscoverPeer(IPEndPoint address, byte[] discoveryQueryData = null)
	{
		CheckNotDisposed();
		InternalSend(address, DeliveryMode.Raw, new DiscoveryRequestMessage
		{
			DiscoveryRequestData = (discoveryQueryData ?? new byte[0])
		});
	}

	public void Connect(IPEndPoint address, byte[] connectRequestData = null)
	{
		CheckNotDisposed();
		lock (Lock)
		{
			if (ConnectedTo == null && ConnectingTo == null)
			{
				ConnectingTo = address;
				ConnectStartTime = Comm.GetTime();
				InternalSend(address, DeliveryMode.ReliableSequenced, new ConnectRequestMessage
				{
					ConnectRequestData = (connectRequestData ?? new byte[0])
				});
				return;
			}
			throw new InvalidOperationException("Connect is not allowed.");
		}
	}

	public void Disconnect()
	{
		CheckNotDisposed();
		lock (Lock)
		{
			if (ConnectedTo != null)
			{
				InternalSend(ConnectedTo.Address, DeliveryMode.ReliableSequenced, new DisconnectRequestMessage());
				InternalDisconnect();
			}
		}
	}

	public void RespondToDiscovery(IPEndPoint address, DeliveryMode deliveryMode, byte[] discoveryResponseData = null)
	{
		CheckNotDisposed();
		lock (Lock)
		{
			InternalSend(address, deliveryMode, new DiscoveryResponseMessage
			{
				DiscoveryResponseData = (discoveryResponseData ?? new byte[0])
			});
		}
	}

	public void AcceptConnect(PeerData peerData, byte[] connectAcceptedData = null)
	{
		CheckNotDisposed();
		lock (Lock)
		{
			if (peerData == null || peerData == ConnectedTo || peerData.Address == ConnectingTo || Peers.Contains(peerData))
			{
				throw new ArgumentException("peerData");
			}
			InternalSend(peerData.Address, DeliveryMode.ReliableSequenced, new ConnectAcceptedMessage
			{
				ConnectAcceptedData = (connectAcceptedData ?? new byte[0]),
				PeersAddresses = (Settings.SendPeerConnectDisconnectNotifications ? PeersByAddress.Keys.ToArray() : new IPEndPoint[0])
			});
			if (Settings.SendPeerConnectDisconnectNotifications)
			{
				foreach (PeerData value in PeersByAddress.Values)
				{
					InternalSend(value.Address, DeliveryMode.ReliableSequenced, new PeerConnectedMessage
					{
						PeerAddress = peerData.Address
					});
				}
			}
			PeersByAddress.Add(peerData.Address, peerData);
			this.PeerConnected?.Invoke(peerData);
		}
	}

	public void RefuseConnect(PeerData peerData, byte[] refuseConnectData = null)
	{
		CheckNotDisposed();
		lock (Lock)
		{
			if (peerData == null || peerData == ConnectedTo || peerData.Address == ConnectingTo || Peers.Contains(peerData))
			{
				throw new ArgumentException("peerData");
			}
			InternalSend(peerData.Address, DeliveryMode.ReliableSequenced, new ConnectRefusedMessage
			{
				ConnectRefusedData = (refuseConnectData ?? new byte[0])
			});
		}
	}

	public void SendDataMessage(PeerData peerData, DeliveryMode deliveryMode, byte[] bytes)
	{
		CheckNotDisposed();
		lock (Lock)
		{
			if (peerData == null || (peerData != ConnectedTo && !Peers.Contains(peerData)))
			{
				throw new ArgumentException("peerData");
			}
			InternalSend(peerData.Address, deliveryMode, new DataMessage
			{
				Bytes = bytes
			});
		}
	}

	public void SendDataMessages(PeerData peerData, DeliveryMode deliveryMode, IEnumerable<byte[]> bytes)
	{
		CheckNotDisposed();
		lock (Lock)
		{
			if (peerData == null || (peerData != ConnectedTo && !Peers.Contains(peerData)))
			{
				throw new ArgumentException("peerData");
			}
			IEnumerable<DataMessage> messages = bytes.Select((byte[] b) => new DataMessage
			{
				Bytes = b
			});
			InternalSend(peerData.Address, deliveryMode, messages);
		}
	}

	public void DisconnectPeer(PeerData peerData)
	{
		CheckNotDisposed();
		lock (Lock)
		{
			if (!PeersByAddress.ContainsValue(peerData))
			{
				return;
			}
			InternalSend(peerData.Address, DeliveryMode.ReliableSequenced, new DisconnectedMessage());
			PeersByAddress.Remove(peerData.Address);
			if (Settings.SendPeerConnectDisconnectNotifications)
			{
				foreach (PeerData value in PeersByAddress.Values)
				{
					InternalSend(value.Address, DeliveryMode.ReliableSequenced, new PeerDisconnectedMessage
					{
						PeerAddress = peerData.Address
					});
				}
			}
			this.PeerDisconnected?.Invoke(peerData);
		}
	}

	public void DisconnectAllPeers()
	{
		CheckNotDisposed();
		lock (Lock)
		{
			PeerData[] array = PeersByAddress.Values.ToArray();
			foreach (PeerData peerData in array)
			{
				DisconnectPeer(peerData);
			}
		}
	}

	private void ThreadFunction()
	{
		List<PeerData> list = new List<PeerData>();
		while (!IsDisposed)
		{
			lock (Lock)
			{
				double time = Comm.GetTime();
				list.Clear();
				if (ConnectedTo != null)
				{
					list.Add(ConnectedTo);
				}
				else
				{
					list.AddRange(PeersByAddress.Values);
				}
				foreach (PeerData item in list)
				{
					double num = time - item.LastKeepAliveReceiveTime;
					if (num >= (double)Settings.ConnectionLostPeriod)
					{
						if (item == ConnectedTo)
						{
							this.Error?.Invoke(new KeepAliveTimeoutException($"Server {item.Address} did not respond for {num:0.0}s, disconnecting."));
							Disconnect();
						}
						else
						{
							this.Error?.Invoke(new KeepAliveTimeoutException($"Peer {item.Address} did not respond for {num:0.0}s, disconnecting."));
							DisconnectPeer(item);
						}
					}
					else if (time >= item.NextKeepAliveSendTime)
					{
						InternalSend(item.Address, DeliveryMode.Unreliable, new KeepAliveRequestMessage
						{
							RequestSendTime = time
						});
						item.NextKeepAliveSendTime = time + (double)Settings.KeepAliveResendPeriod;
					}
				}
				if (ConnectingTo != null && time - ConnectStartTime >= (double)Settings.ConnectTimeOut)
				{
					IPEndPoint connectingTo = ConnectingTo;
					ConnectingTo = null;
					try
					{
						this.ConnectTimedOut?.Invoke(connectingTo);
					}
					catch (Exception obj)
					{
						this.Error?.Invoke(obj);
					}
				}
			}
			float num2 = 0.2f;
			float num3 = Math.Min(Settings.KeepAlivePeriod, Math.Min(Settings.KeepAliveResendPeriod, Math.Min(Settings.ConnectTimeOut, Settings.ConnectionLostPeriod)));
			Task.Delay(Math.Min(Math.Max((int)(1000f * num3 * num2), 10), 250)).Wait();
		}
	}

	private void InternalDisconnect()
	{
		ConnectedTo = null;
		PeersByAddress.Clear();
		ConnectingTo = null;
		this.Disconnected?.Invoke();
	}

	private void InternalSend(IPEndPoint address, DeliveryMode deliveryMode, Message message)
	{
		byte[] bytes = Message.Write(message);
		Comm.Send(address, deliveryMode, bytes);
	}

	private void InternalSend(IPEndPoint address, DeliveryMode deliveryMode, IEnumerable<Message> messages)
	{
		List<byte[]> list = new List<byte[]>();
		foreach (Message message in messages)
		{
			list.Add(Message.Write(message));
		}
		Comm.Send(address, deliveryMode, list);
	}

	private void CheckNotDisposed()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("Peer");
		}
	}
}
