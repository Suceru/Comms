namespace Comms;

public struct PeerPacket
{
	public PeerData Peer;

	public byte[] Data;

	public PeerPacket(PeerData peer, byte[] data)
	{
		Peer = peer;
		Data = data;
	}
}
