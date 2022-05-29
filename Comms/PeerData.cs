using System.Net;

namespace Comms;

public class PeerData
{
	internal double LastKeepAliveReceiveTime;

	internal double NextKeepAliveSendTime;

	public IPEndPoint Address { get; internal set; }

	public float Ping { get; internal set; }

	public object Tag { get; set; }

	internal PeerData(IPEndPoint address)
	{
		Address = address;
		LastKeepAliveReceiveTime = Comm.GetTime();
	}
}
