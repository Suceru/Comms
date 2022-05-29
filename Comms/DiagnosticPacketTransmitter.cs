using System;
using System.Net;

namespace Comms;

public class DiagnosticPacketTransmitter : IPacketTransmitter, IDisposable
{
	public long PacketsReceived { get; set; }

	public long PacketsSent { get; set; }

	public long BytesSent { get; set; }

	public long BytesReceived { get; set; }

	public IPacketTransmitter BaseTransmitter { get; private set; }

	public int MaxPacketSize => BaseTransmitter.MaxPacketSize;

	public IPEndPoint Address => BaseTransmitter.Address;

	public event Action<Exception> Error;

	public event Action<Packet> PacketSent;

	public event Action<Packet> PacketReceived;

	public DiagnosticPacketTransmitter(IPacketTransmitter baseTransmitter)
	{
		BaseTransmitter = baseTransmitter ?? throw new ArgumentNullException("baseTransmitter");
		BaseTransmitter.Error += delegate(Exception e)
		{
			this.Error?.Invoke(e);
		};
		BaseTransmitter.PacketReceived += delegate(Packet packet)
		{
			PacketsReceived++;
			BytesReceived += packet.Data.Length;
			this.PacketReceived?.Invoke(packet);
		};
	}

	public void SendPacket(Packet packet)
	{
		PacketsSent++;
		BytesSent += packet.Data.Length;
		BaseTransmitter.SendPacket(packet);
		this.PacketSent?.Invoke(packet);
	}

	public void Dispose()
	{
		BaseTransmitter.Dispose();
	}
}
