using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Comms;

public class InProcessPacketTransmitter : IPacketTransmitter, IDisposable
{
	private static ushort NextPort = 0;

	private static Dictionary<int, InProcessPacketTransmitter> Transmitters = new Dictionary<int, InProcessPacketTransmitter>();

	private bool IsDisposed;

	public int MaxPacketSize { get; set; } = 1024;


	public IPEndPoint Address { get; private set; }

	public event Action<Exception> Error
	{
		add
		{
		}
		remove
		{
		}
	}

	public event Action<Packet> PacketReceived;

	public InProcessPacketTransmitter()
	{
		lock (Transmitters)
		{
			while (Transmitters.ContainsKey(NextPort))
			{
				NextPort++;
			}
			Address = new IPEndPoint(0L, NextPort++);
			Transmitters.Add(Address.Port, this);
		}
	}

	public InProcessPacketTransmitter(int port)
	{
		lock (Transmitters)
		{
			Address = new IPEndPoint(0L, port);
			Transmitters.Add(Address.Port, this);
		}
	}

	public void SendPacket(Packet packet)
	{
		CheckNotDisposed();
		lock (Transmitters)
		{
			if (object.Equals(packet.Address.Address, IPAddress.Broadcast))
			{
				foreach (InProcessPacketTransmitter value2 in Transmitters.Values)
				{
					if (value2 != this)
					{
						value2.PacketReceived?.Invoke(new Packet(Address, packet.Data.ToArray()));
					}
				}
				return;
			}
			if (Transmitters.TryGetValue(packet.Address.Port, out var value))
			{
				value.PacketReceived?.Invoke(new Packet(Address, packet.Data.ToArray()));
			}
		}
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			IsDisposed = true;
			lock (Transmitters)
			{
				Transmitters.Remove(Address.Port);
			}
		}
	}

	private void CheckNotDisposed()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("InProcessPacketTransmitter");
		}
	}
}
