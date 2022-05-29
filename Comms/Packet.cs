using System.Net;

namespace Comms;

public struct Packet
{
	public IPEndPoint Address;

	public byte[] Data;

	public Packet(IPEndPoint address, byte[] data)
	{
		Address = address;
		Data = data;
	}
}
