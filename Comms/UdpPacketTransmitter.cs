using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Comms;

public class UdpPacketTransmitter : IPacketTransmitter, IDisposable
{
	private volatile bool IsDisposed;

	private Task Task;

	private Socket Socket4;

	private Socket Socket6;

	public static IPAddress IPV6BroadcastAddress { get; } = IPAddress.Parse("ff08::1");


	public int MaxPacketSize => 1024;

	public IPEndPoint Address { get; private set; }

	public event Action<Exception> Error;

	public event Action<Packet> PacketReceived;

	public UdpPacketTransmitter(int localPort = 0)
	{
		try
		{
			Socket4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			Socket4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, optionValue: true);
			Socket4.Bind(new IPEndPoint(IPAddress.Any, localPort));
			Socket4.ReceiveTimeout = 1000;
			if (Address == null)
			{
				try
				{
					using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					socket.Bind(new IPEndPoint(IPAddress.Any, 0));
					socket.Connect("8.8.8.8", 12345);
					Address = new IPEndPoint(((IPEndPoint)socket.LocalEndPoint).Address, ((IPEndPoint)Socket4.LocalEndPoint).Port);
				}
				catch (Exception)
				{
				}
			}
		}
		catch (Exception ex2)
		{
			Socket4?.Dispose();
			Socket4 = null;
			if (ex2 is SocketException ex3 && ex3.SocketErrorCode == SocketError.AddressAlreadyInUse)
			{
				throw ex3;
			}
		}
		try
		{
			Socket6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
			Socket6.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, optionValue: true);
			Socket6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, optionValue: true);
			Socket6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(IPV6BroadcastAddress));
			Socket6.Bind(new IPEndPoint(IPAddress.IPv6Any, localPort));
			Socket6.ReceiveTimeout = 1000;
			if (Address == null)
			{
				try
				{
					using Socket socket2 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
					socket2.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, optionValue: true);
					socket2.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
					socket2.Connect("2001:4860:4860::8888", 12345);
					Address = new IPEndPoint(((IPEndPoint)socket2.LocalEndPoint).Address, ((IPEndPoint)Socket6.LocalEndPoint).Port);
				}
				catch (Exception)
				{
				}
			}
		}
		catch (Exception ex5)
		{
			Socket6?.Dispose();
			Socket6 = null;
			if (ex5 is SocketException ex6 && ex6.SocketErrorCode == SocketError.AddressAlreadyInUse)
			{
				throw ex6;
			}
		}
		if (Socket4 == null && Socket6 == null)
		{
			throw new InvalidOperationException("No network connectivity.");
		}
		Task = Task.Factory.StartNew(ThreadFunction, TaskCreationOptions.LongRunning);
	}

	public void SendPacket(Packet packet)
	{
		CheckNotDisposed();
		if (packet.Address.AddressFamily == AddressFamily.InterNetwork && Socket4 != null)
		{
			Socket4.SendTo(packet.Data, packet.Address);
		}
		else if (packet.Address.AddressFamily == AddressFamily.InterNetworkV6 && Socket6 != null)
		{
			Socket6.SendTo(packet.Data, packet.Address);
		}
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			IsDisposed = true;
			Task.Wait();
			Socket4?.Dispose();
			Socket6?.Dispose();
		}
	}

	private void ThreadFunction()
	{
		List<Socket> list = new List<Socket>();
		byte[] array = new byte[65536];
		while (!IsDisposed)
		{
			try
			{
				list.Clear();
				if (Socket4 != null)
				{
					list.Add(Socket4);
				}
				if (Socket6 != null)
				{
					list.Add(Socket6);
				}
				Socket.Select(list, null, null, 1000000);
				foreach (Socket item in list)
				{
					EndPoint remoteEP = ((item.AddressFamily != AddressFamily.InterNetwork) ? new IPEndPoint(IPAddress.IPv6Any, 0) : new IPEndPoint(IPAddress.Any, 0));
					int num = item.ReceiveFrom(array, ref remoteEP);
					byte[] array2 = new byte[num];
					Array.Copy(array, 0, array2, 0, num);
					this.PacketReceived?.Invoke(new Packet((IPEndPoint)remoteEP, array2));
				}
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode != SocketError.TimedOut && ex.SocketErrorCode != SocketError.Interrupted)
				{
					this.Error?.Invoke((Exception)(object)ex);
				}
			}
		}
	}

	private void CheckNotDisposed()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("UdpPacketTransmitter");
		}
	}
}
