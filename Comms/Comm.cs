using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Comms;

public class Comm
{
	private class Connection
	{
		public Guid OurGuid = Guid.NewGuid();

		public Guid TheirGuid;

		public bool InitAckReceived;

		public Dictionary<uint, UnackedPacket> UnackedPackets = new Dictionary<uint, UnackedPacket>();

		public Dictionary<uint, MessageParts> MessageParts = new Dictionary<uint, MessageParts>();

		public Dictionary<uint, byte[]> SequencedBytes = new Dictionary<uint, byte[]>();

		public List<uint> PacketIdsToAck = new List<uint>();

		public HashSet<uint> ReceivedPacketIdsOld = new HashSet<uint>();

		public HashSet<uint> ReceivedPacketIdsCurrent = new HashSet<uint>();

		public double LastReceivedPacketsIdsSwitchTime;

		public uint? NextUnreliableReceiveSequenceIndex;

		public uint NextUnreliableSendSequenceIndex;

		public uint? NextReliableReceiveSequenceIndex;

		public uint NextReliableSendSequenceIndex;

		public double LastSendTime = double.MinValue;

		public double LastReceiveTime = double.MinValue;

		public void NewTheirGuid(Guid theirGuid)
		{
			TheirGuid = theirGuid;
			MessageParts.Clear();
			SequencedBytes.Clear();
			PacketIdsToAck.Clear();
			ReceivedPacketIdsOld.Clear();
			ReceivedPacketIdsCurrent.Clear();
			NextUnreliableReceiveSequenceIndex = 0u;
			NextReliableReceiveSequenceIndex = 0u;
		}
	}

	private class MessageParts
	{
		public double LastReceiveTime = double.MinValue;

		public int LastPartIndex = -1;

		public Dictionary<int, byte[]> Parts = new Dictionary<int, byte[]>();
	}

	private enum PacketType : byte
	{
		RawData = 1,
		UnreliableData,
		ReliableData,
		DataAck,
		InitAck
	}

	private struct PacketHeader
	{
		public bool IsInvalid;

		public PacketType PacketType;

		public bool IsConnectionInit;

		public uint PacketId;

		public Guid InitGuid;

		public static PacketHeader Read(BinaryReader reader)
		{
			PacketHeader result = default(PacketHeader);
			if (reader.BaseStream.Length - reader.BaseStream.Position >= 1)
			{
				byte b = reader.ReadByte();
				result.PacketType = (PacketType)(b & 0xFu);
				if (result.PacketType == PacketType.RawData)
				{
					return result;
				}
				if (result.PacketType == PacketType.UnreliableData || result.PacketType == PacketType.ReliableData)
				{
					result.IsConnectionInit = (b & 0x80) != 0;
					int num = (result.IsConnectionInit ? 20 : 4);
					if (reader.BaseStream.Length - reader.BaseStream.Position >= num)
					{
						if (result.IsConnectionInit)
						{
							result.InitGuid = new Guid(reader.ReadBytes(16));
						}
						result.PacketId = reader.ReadUInt32();
						return result;
					}
				}
				else if (result.PacketType == PacketType.DataAck)
				{
					if (reader.BaseStream.Length - reader.BaseStream.Position >= 4)
					{
						return result;
					}
				}
				else if (result.PacketType == PacketType.InitAck && reader.BaseStream.Length - reader.BaseStream.Position == 16)
				{
					result.InitGuid = new Guid(reader.ReadBytes(16));
					return result;
				}
			}
			result.IsInvalid = true;
			return result;
		}

		public static void WriteRaw(BinaryWriter writer)
		{
			writer.Write((byte)1);
		}

		public static void WriteData(BinaryWriter writer, Guid? initGuid, uint packetId, bool requiresAck)
		{
			byte b = (byte)(requiresAck ? 3 : 2);
			if (initGuid.HasValue)
			{
				b = (byte)(b | 0x80u);
			}
			writer.Write(b);
			if (initGuid.HasValue)
			{
				writer.Write(initGuid.Value.ToByteArray());
			}
			writer.Write(packetId);
		}

		public static void WriteDataAck(BinaryWriter writer)
		{
			writer.Write((byte)4);
		}

		public static void WriteInitAck(BinaryWriter writer, Guid initGuid)
		{
			writer.Write((byte)5);
			writer.Write(initGuid.ToByteArray());
		}
	}

	private struct MessagePartHeader
	{
		public bool IsInvalid;

		public uint MessageId;

		public uint? SequenceIndex;

		public int PartIndex;

		public bool IsFinalPart;

		public int DataSize;

		public static MessagePartHeader Read(BinaryReader reader)
		{
			MessagePartHeader result = default(MessagePartHeader);
			try
			{
				byte b = reader.ReadByte();
				result.MessageId = ((((uint)b & (true ? 1u : 0u)) != 0 || (b & 4) == 0) ? reader.ReadUInt32() : 0u);
				result.SequenceIndex = (((b & 8u) != 0) ? new uint?(reader.ReadUInt32()) : null);
				result.PartIndex = ((((uint)b & (true ? 1u : 0u)) != 0) ? Read7BitEncodedInt(reader) : 0);
				result.DataSize = (((b & 2u) != 0) ? Read7BitEncodedInt(reader) : (-1));
				result.IsFinalPart = (b & 4) != 0;
				return result;
			}
			catch (EndOfStreamException)
			{
				result.IsInvalid = true;
				return result;
			}
		}

		public static void Write(BinaryWriter writer, uint messageId, uint? sequenceIndex, int partIndex, bool isFinalPart, int dataSize)
		{
			byte b = 0;
			if (partIndex != 0)
			{
				b = (byte)(b | 1u);
			}
			if (dataSize >= 0)
			{
				b = (byte)(b | 2u);
			}
			if (isFinalPart)
			{
				b = (byte)(b | 4u);
			}
			if (sequenceIndex.HasValue)
			{
				b = (byte)(b | 8u);
			}
			writer.Write(b);
			if (partIndex != 0 || !isFinalPart)
			{
				writer.Write(messageId);
			}
			if (sequenceIndex.HasValue)
			{
				writer.Write(sequenceIndex.Value);
			}
			if (partIndex != 0)
			{
				Write7BitEncodedInt(writer, partIndex);
			}
			if (dataSize >= 0)
			{
				Write7BitEncodedInt(writer, dataSize);
			}
		}
	}

	private class UnackedPacket
	{
		public double LastSendTime;

		public int SendCount;

		public Packet Packet;
	}

	private abstract class Job
	{
		public abstract void Execute(Comm comm);
	}

	private class SendMessagesJob : Job
	{
		public IPEndPoint Address;

		public DeliveryMode DeliveryMode;

		public byte[][] Bytes;

		public override void Execute(Comm comm)
		{
			comm.SendMessages(Address, Bytes, DeliveryMode);
		}
	}

	private class ReceivePacketJob : Job
	{
		public Packet Packet;

		public override void Execute(Comm comm)
		{
			comm.ProcessReceivedPacket(Packet);
		}
	}

	private class QueryUnackedPacketsJob : Job
	{
		public IPEndPoint Address;

		public ManualResetEvent CompletedEvent = new ManualResetEvent(initialState: false);

		public int Result;

		public override void Execute(Comm comm)
		{
			Result = comm.QueryUnackedPacketsCount(Address);
			CompletedEvent.Set();
		}
	}

	private volatile bool IsDisposed;

	private Task Task;

	private uint NextPacketId;

	private uint NextMessageId;

	private Dictionary<IPEndPoint, Connection> Connections = new Dictionary<IPEndPoint, Connection>();

	private ProducerConsumerQueue<Job> Jobs = new ProducerConsumerQueue<Job>();

	private List<uint> ToRemoveUInt = new List<uint>();

	private List<IPEndPoint> ToRemoveEndpoint = new List<IPEndPoint>();

	public CommSettings Settings { get; private set; } = new CommSettings();


	public IPacketTransmitter Transmitter { get; private set; }

	public IPEndPoint Address => Transmitter.Address;

	public event Action<Packet> Received;

	public event Action<Exception> Error;

	public Comm(int localPort = 0)
		: this(new UdpPacketTransmitter(localPort))
	{
	}

	public Comm(IPacketTransmitter transmitter)
	{
		Transmitter = transmitter ?? throw new ArgumentNullException("transmitter");
		Transmitter.Error += delegate(Exception e)
		{
			this.Error?.Invoke(e);
		};
		Transmitter.PacketReceived += delegate(Packet packet)
		{
			Jobs.Add(new ReceivePacketJob
			{
				Packet = packet
			});
		};
		Task = Task.Factory.StartNew(ThreadFunction, TaskCreationOptions.LongRunning);
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			IsDisposed = true;
			Task.Wait();
			Transmitter.Dispose();
		}
	}

	public void Send(IPEndPoint address, DeliveryMode deliveryMode, byte[] bytes)
	{
		Send(address, deliveryMode, new byte[1][] { bytes });
	}

	public void Send(IPEndPoint address, DeliveryMode deliveryMode, IEnumerable<byte[]> bytes)
	{
		CheckNotDisposed();
		if ((object.Equals(address.Address, IPAddress.Broadcast) || object.Equals(address.Address, UdpPacketTransmitter.IPV6BroadcastAddress)) && deliveryMode != 0)
		{
			throw new InvalidOperationException("Broadcast messages must use DeliveryMode.Raw");
		}
		Jobs.Add(new SendMessagesJob
		{
			Address = address,
			DeliveryMode = deliveryMode,
			Bytes = bytes.Select((byte[] b) => b.ToArray()).ToArray()
		});
	}

	public int GetUnackedPacketsCount(IPEndPoint address)
	{
		CheckNotDisposed();
		QueryUnackedPacketsJob queryUnackedPacketsJob = new QueryUnackedPacketsJob
		{
			Address = address
		};
		Jobs.Add(queryUnackedPacketsJob);
		queryUnackedPacketsJob.CompletedEvent.WaitOne();
		return queryUnackedPacketsJob.Result;
	}

	public static double GetTime()
	{
		return (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
	}

	public static int Read7BitEncodedInt(BinaryReader reader)
	{
		int num = 0;
		int num2 = 0;
		byte b;
		do
		{
			if (num2 == 35)
			{
				throw new FormatException();
			}
			b = reader.ReadByte();
			num |= (b & 0x7F) << num2;
			num2 += 7;
		}
		while ((b & 0x80u) != 0);
		return num;
	}

	public static void Write7BitEncodedInt(BinaryWriter writer, int value)
	{
		uint num;
		for (num = (uint)value; num >= 128; num >>= 7)
		{
			writer.Write((byte)(num | 0x80u));
		}
		writer.Write((byte)num);
	}

	private void ThreadFunction()
	{
		while (!IsDisposed)
		{
			ProcessConnections();
			double time = GetTime();
			do
			{
				if (Jobs.TryTake(out var t, 10))
				{
					try
					{
						t.Execute(this);
					}
					catch (Exception obj)
					{
						this.Error?.Invoke(obj);
					}
				}
			}
			while (!IsDisposed && GetTime() - time < 0.2 * (double)Settings.ResendPeriods[0]);
		}
	}

	private void ProcessReceivedPacket(Packet packet)
	{
		double time = GetTime();
		BinaryReader binaryReader = new BinaryReader(new MemoryStream(packet.Data));
		PacketHeader packetHeader = PacketHeader.Read(binaryReader);
		if (packetHeader.IsInvalid)
		{
			this.Error?.Invoke(new InvalidOperationException($"Invalid packet header received from {packet.Address.ToString()}"));
			return;
		}
		if (packetHeader.PacketType == PacketType.RawData)
		{
			int count = (int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position);
			byte[] bytes = binaryReader.ReadBytes(count);
			FireReceivedEvent(packet.Address, bytes);
			return;
		}
		if (!Connections.TryGetValue(packet.Address, out var value))
		{
			value = new Connection();
			Connections.Add(packet.Address, value);
		}
		value.LastReceiveTime = time;
		if (packetHeader.PacketType == PacketType.UnreliableData || packetHeader.PacketType == PacketType.ReliableData)
		{
			if (packetHeader.IsConnectionInit)
			{
				if (value.TheirGuid == Guid.Empty || packetHeader.InitGuid != value.TheirGuid)
				{
					value.NewTheirGuid(packetHeader.InitGuid);
				}
				SendInitAckPacket(packet.Address, packetHeader.InitGuid, value);
			}
			else if (value.TheirGuid == Guid.Empty)
			{
				this.Error?.Invoke(new InvalidOperationException($"No InitGuid in packet received from {packet.Address.ToString()}"));
				return;
			}
			bool flag = packetHeader.PacketType == PacketType.ReliableData;
			if (flag)
			{
				value.PacketIdsToAck.Add(packetHeader.PacketId);
			}
			if (value.ReceivedPacketIdsOld.Contains(packetHeader.PacketId))
			{
				value.ReceivedPacketIdsCurrent.Add(packetHeader.PacketId);
			}
			else
			{
				if (value.ReceivedPacketIdsCurrent.Contains(packetHeader.PacketId))
				{
					return;
				}
				value.ReceivedPacketIdsCurrent.Add(packetHeader.PacketId);
				while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
				{
					MessagePartHeader messagePartHeader = MessagePartHeader.Read(binaryReader);
					if (messagePartHeader.IsInvalid)
					{
						this.Error?.Invoke(new InvalidOperationException($"Invalid message part header received from {packet.Address.ToString()}"));
						break;
					}
					int count2 = (int)((messagePartHeader.DataSize >= 0) ? messagePartHeader.DataSize : (binaryReader.BaseStream.Length - binaryReader.BaseStream.Position));
					byte[] array = binaryReader.ReadBytes(count2);
					if (messagePartHeader.PartIndex == 0 && messagePartHeader.IsFinalPart)
					{
						ProcessReceivedMessage(packet.Address, value, messagePartHeader, array, flag);
						continue;
					}
					if (!value.MessageParts.TryGetValue(messagePartHeader.MessageId, out var value2))
					{
						value2 = new MessageParts();
						value.MessageParts.Add(messagePartHeader.MessageId, value2);
					}
					value2.LastReceiveTime = time;
					value2.Parts[messagePartHeader.PartIndex] = array;
					if (messagePartHeader.IsFinalPart)
					{
						value2.LastPartIndex = messagePartHeader.PartIndex;
					}
					if (value2.LastPartIndex < 0 || value2.Parts.Count != value2.LastPartIndex + 1)
					{
						continue;
					}
					bool flag2 = true;
					int num = 0;
					for (int i = 0; i <= value2.LastPartIndex; i++)
					{
						if (!value2.Parts.TryGetValue(i, out var value3))
						{
							flag2 = false;
							break;
						}
						num += value3.Length;
					}
					if (flag2)
					{
						byte[] array2 = new byte[num];
						int j = 0;
						int num2 = 0;
						for (; j <= value2.LastPartIndex; j++)
						{
							byte[] array3 = value2.Parts[j];
							Array.Copy(array3, 0, array2, num2, array3.Length);
							num2 += array3.Length;
						}
						ProcessReceivedMessage(packet.Address, value, messagePartHeader, array2, flag);
					}
					value.MessageParts.Remove(messagePartHeader.MessageId);
				}
			}
		}
		else if (packetHeader.PacketType == PacketType.DataAck)
		{
			while (binaryReader.BaseStream.Length - binaryReader.BaseStream.Position >= 4)
			{
				uint key = binaryReader.ReadUInt32();
				value.UnackedPackets.Remove(key);
			}
		}
		else
		{
			if (packetHeader.PacketType != PacketType.InitAck)
			{
				throw new InvalidOperationException($"Invalid packet type received from {packet.Address.ToString()}");
			}
			if (!(packetHeader.InitGuid == value.OurGuid))
			{
				throw new InvalidOperationException($"Invalid InitAck GUID received from {packet.Address.ToString()}");
			}
			value.InitAckReceived = true;
		}
	}

	private void ProcessReceivedMessage(IPEndPoint address, Connection connection, MessagePartHeader messagePartHeader, byte[] bytes, bool isReliable)
	{
		if (messagePartHeader.SequenceIndex.HasValue)
		{
			if (isReliable)
			{
				if (!connection.NextReliableReceiveSequenceIndex.HasValue || messagePartHeader.SequenceIndex.Value == connection.NextReliableReceiveSequenceIndex)
				{
					connection.NextReliableReceiveSequenceIndex = messagePartHeader.SequenceIndex.Value + 1;
					FireReceivedEvent(address, bytes);
					byte[] value;
					while (connection.SequencedBytes.TryGetValue(connection.NextReliableReceiveSequenceIndex.Value, out value))
					{
						connection.SequencedBytes.Remove(connection.NextReliableReceiveSequenceIndex.Value);
						connection.NextReliableReceiveSequenceIndex++;
						FireReceivedEvent(address, value);
					}
				}
				else
				{
					connection.SequencedBytes.Add(messagePartHeader.SequenceIndex.Value, bytes);
				}
			}
			else if (!connection.NextUnreliableReceiveSequenceIndex.HasValue || CompareSequenceNumbers(messagePartHeader.SequenceIndex.Value, connection.NextUnreliableReceiveSequenceIndex.Value) >= 0)
			{
				connection.NextUnreliableReceiveSequenceIndex = messagePartHeader.SequenceIndex.Value + 1;
				FireReceivedEvent(address, bytes);
			}
		}
		else
		{
			FireReceivedEvent(address, bytes);
		}
	}

	private void FireReceivedEvent(IPEndPoint address, byte[] bytes)
	{
		try
		{
			this.Received?.Invoke(new Packet(address, bytes));
		}
		catch (Exception obj)
		{
			this.Error?.Invoke(obj);
		}
	}

	private int QueryUnackedPacketsCount(IPEndPoint address)
	{
		if (!Connections.TryGetValue(address, out var value))
		{
			return 0;
		}
		return value.UnackedPackets.Count;
	}

	private void ProcessConnections()
	{
		double time = GetTime();
		foreach (KeyValuePair<IPEndPoint, Connection> connection in Connections)
		{
			IPEndPoint key = connection.Key;
			Connection value = connection.Value;
			if (value.PacketIdsToAck.Count > 0)
			{
				SendDataAckPackets(key, value.PacketIdsToAck, value);
				value.PacketIdsToAck.Clear();
			}
			foreach (KeyValuePair<uint, UnackedPacket> unackedPacket in value.UnackedPackets)
			{
				float num = Settings.ResendPeriods[Math.Min(unackedPacket.Value.SendCount - 1, Settings.ResendPeriods.Length - 1)];
				if (unackedPacket.Value.SendCount - 1 >= Settings.MaxResends)
				{
					ToRemoveUInt.Add(unackedPacket.Key);
				}
				else if (time >= unackedPacket.Value.LastSendTime + (double)num)
				{
					SendPacket(unackedPacket.Value.Packet, value);
					unackedPacket.Value.LastSendTime = time;
					unackedPacket.Value.SendCount++;
				}
			}
			foreach (uint item in ToRemoveUInt)
			{
				value.UnackedPackets.Remove(item);
			}
			ToRemoveUInt.Clear();
			foreach (KeyValuePair<uint, MessageParts> messagePart in value.MessageParts)
			{
				if (time - messagePart.Value.LastReceiveTime > (double)((float)Settings.MaxResends * Settings.ResendPeriods[Settings.ResendPeriods.Length - 1]))
				{
					ToRemoveUInt.Add(messagePart.Key);
				}
			}
			foreach (uint item2 in ToRemoveUInt)
			{
				value.MessageParts.Remove(item2);
			}
			ToRemoveUInt.Clear();
			if (time >= value.LastReceivedPacketsIdsSwitchTime + (double)Settings.DuplicatePacketsDetectionTime)
			{
				value.ReceivedPacketIdsOld.Clear();
				HashSet<uint> receivedPacketIdsOld = value.ReceivedPacketIdsOld;
				value.ReceivedPacketIdsOld = value.ReceivedPacketIdsCurrent;
				value.ReceivedPacketIdsCurrent = receivedPacketIdsOld;
				value.LastReceivedPacketsIdsSwitchTime = time;
			}
			if (time - value.LastSendTime >= (double)Settings.IdleTime && time - value.LastReceiveTime >= (double)(2f * Settings.IdleTime))
			{
				ToRemoveEndpoint.Add(key);
			}
		}
		foreach (IPEndPoint item3 in ToRemoveEndpoint)
		{
			Connections.Remove(item3);
		}
		ToRemoveEndpoint.Clear();
	}

	private void SendMessages(IPEndPoint address, byte[][] bytes, DeliveryMode deliveryMode)
	{
		if (deliveryMode == DeliveryMode.Raw)
		{
			foreach (byte[] buffer in bytes)
			{
				BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
				PacketHeader.WriteRaw(binaryWriter);
				binaryWriter.Write(buffer);
				Transmitter.SendPacket(new Packet(address, ((MemoryStream)binaryWriter.BaseStream).ToArray()));
			}
			return;
		}
		if (!Connections.TryGetValue(address, out var value))
		{
			value = new Connection();
			Connections.Add(address, value);
		}
		BinaryWriter binaryWriter2 = null;
		byte[] array = null;
		int num = 0;
		uint packetId = 0u;
		uint messageId = 0u;
		uint? sequenceIndex = 0u;
		int num2 = 0;
		int num3 = 0;
		while (true)
		{
			if (array == null)
			{
				if (num3 >= bytes.Length)
				{
					break;
				}
				array = bytes[num3++];
				num = 0;
				messageId = NextMessageId++;
				sequenceIndex = deliveryMode switch
				{
					DeliveryMode.UnreliableSequenced => value.NextUnreliableSendSequenceIndex++, 
					DeliveryMode.ReliableSequenced => value.NextReliableSendSequenceIndex++, 
					_ => null, 
				};
				num2 = 0;
			}
			if (binaryWriter2 == null)
			{
				binaryWriter2 = new BinaryWriter(new MemoryStream());
				packetId = NextPacketId;
				NextPacketId++;
				Guid? initGuid = (value.InitAckReceived ? null : new Guid?(value.OurGuid));
				PacketHeader.WriteData(binaryWriter2, initGuid, packetId, deliveryMode == DeliveryMode.Reliable || deliveryMode == DeliveryMode.ReliableSequenced);
			}
			int num4 = (int)binaryWriter2.BaseStream.Length;
			int num5 = array.Length - num;
			MessagePartHeader.Write(binaryWriter2, messageId, sequenceIndex, num2, isFinalPart: true, num5);
			if (binaryWriter2.BaseStream.Length + num5 <= Transmitter.MaxPacketSize)
			{
				binaryWriter2.Write(array, num, num5);
				array = null;
				continue;
			}
			binaryWriter2.BaseStream.SetLength(num4);
			MessagePartHeader.Write(binaryWriter2, messageId, sequenceIndex, num2, isFinalPart: true, -1);
			if (binaryWriter2.BaseStream.Length + num5 <= Transmitter.MaxPacketSize)
			{
				binaryWriter2.Write(array, num, num5);
				array = null;
				continue;
			}
			binaryWriter2.BaseStream.SetLength(num4);
			MessagePartHeader.Write(binaryWriter2, messageId, sequenceIndex, num2, isFinalPart: false, -1);
			num5 = Transmitter.MaxPacketSize - (int)binaryWriter2.BaseStream.Length;
			if (num5 > 0)
			{
				binaryWriter2.Write(array, num, num5);
				num += num5;
				num2++;
			}
			else
			{
				binaryWriter2.BaseStream.SetLength(num4);
			}
			SendDataPacket(address, ((MemoryStream)binaryWriter2.BaseStream).ToArray(), packetId, deliveryMode, value);
			binaryWriter2 = null;
		}
		if (binaryWriter2 != null && binaryWriter2.BaseStream.Position > 0)
		{
			SendDataPacket(address, ((MemoryStream)binaryWriter2.BaseStream).ToArray(), packetId, deliveryMode, value);
		}
	}

	private void SendDataPacket(IPEndPoint address, byte[] bytes, uint packetId, DeliveryMode deliveryMode, Connection connection)
	{
		Packet packet = new Packet(address, bytes);
		if (deliveryMode == DeliveryMode.Reliable || deliveryMode == DeliveryMode.ReliableSequenced)
		{
			connection.UnackedPackets.Add(packetId, new UnackedPacket
			{
				Packet = packet,
				SendCount = 1,
				LastSendTime = GetTime()
			});
		}
		SendPacket(packet, connection);
	}

	private void SendDataAckPackets(IPEndPoint address, List<uint> acks, Connection connection)
	{
		int num = 0;
		while (num < acks.Count)
		{
			BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
			PacketHeader.WriteDataAck(binaryWriter);
			int num2 = Transmitter.MaxPacketSize - (int)binaryWriter.BaseStream.Position;
			int num3 = Math.Min(val2: acks.Count - num, val1: num2 / 4);
			for (int i = 0; i < num3; i++)
			{
				binaryWriter.Write(acks[num++]);
			}
			SendPacket(new Packet(address, ((MemoryStream)binaryWriter.BaseStream).ToArray()), connection);
		}
	}

	private void SendInitAckPacket(IPEndPoint address, Guid initGuid, Connection connection)
	{
		BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
		PacketHeader.WriteInitAck(binaryWriter, initGuid);
		SendPacket(new Packet(address, ((MemoryStream)binaryWriter.BaseStream).ToArray()), connection);
	}

	private void SendPacket(Packet packet, Connection connection)
	{
		connection.LastSendTime = GetTime();
		Transmitter.SendPacket(packet);
	}

	private void CheckNotDisposed()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("Comm");
		}
	}

	private static int CompareSequenceNumbers(uint s1, uint s2)
	{
		if (s1 == s2)
		{
			return 0;
		}
		if ((s1 < s2 && s2 - s1 < int.MaxValue) || (s1 > s2 && s1 - s2 > int.MaxValue))
		{
			return -1;
		}
		return 1;
	}
}
