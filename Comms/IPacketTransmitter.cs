using System;
using System.Net;

namespace Comms;

public interface IPacketTransmitter : IDisposable
{
	int MaxPacketSize { get; }

	IPEndPoint Address { get; }

	event Action<Exception> Error;

	event Action<Packet> PacketReceived;

	void SendPacket(Packet packet);
}
