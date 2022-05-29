using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Comms;

public class NetworkSimulatorPacketTransmitter : IPacketTransmitter, IDisposable
{
	private static double LastActivityTime = Comm.GetTime();

	private static object StaticLock = new object();

	private volatile bool IsDisposed;

	private Random Random = new Random(0);

	private int SimulatedMaxPacketSize = int.MaxValue;

	private SortedDictionary<double, List<Action>> PendingActions = new SortedDictionary<double, List<Action>>();

	private Task Task;

	private object Lock = new object();

	public IPacketTransmitter BaseTransmitter { get; private set; }

	public float MinimumDelay { get; set; }

	public float MaximumDelay { get; set; }

	public float DropRatio { get; set; }

	public float DuplicateRatio { get; set; }

	public float ByteCorruptRatio { get; set; }

	public float TruncateRatio { get; set; }

	public long PacketsReceived { get; set; }

	public long PacketsSent { get; set; }

	public long PacketsDropped { get; set; }

	public long BytesSent { get; set; }

	public long BytesReceived { get; set; }

	public int MaxPacketSize
	{
		get
		{
			return Math.Max(Math.Min(SimulatedMaxPacketSize, BaseTransmitter.MaxPacketSize), 0);
		}
		set
		{
			SimulatedMaxPacketSize = value;
		}
	}

	public IPEndPoint Address => BaseTransmitter.Address;

	public event Action<Exception> Error;

	public event Action<Packet> PacketReceived;

	public NetworkSimulatorPacketTransmitter(IPacketTransmitter baseTransmitter)
	{
		BaseTransmitter = baseTransmitter ?? throw new ArgumentNullException("baseTransmitter");
		BaseTransmitter.Error += delegate(Exception e)
		{
			this.Error?.Invoke(e);
		};
		BaseTransmitter.PacketReceived += delegate(Packet packet)
		{
			lock (StaticLock)
			{
				LastActivityTime = Comm.GetTime();
			}
			if (DropRatio <= 0f || Random.NextDouble() >= (double)DropRatio)
			{
				PacketsReceived++;
				BytesReceived += packet.Data.Length;
				this.PacketReceived?.Invoke(packet);
			}
		};
		Task = new Task(delegate
		{
			while (!IsDisposed || PendingActions.Count > 0)
			{
				try
				{
					Task.Delay(10).Wait();
					ExecutePendingActions();
				}
				catch (Exception obj)
				{
					this.Error?.Invoke(obj);
				}
			}
		}, TaskCreationOptions.LongRunning);
		Task.Start();
	}

	public static float GetIdleTime()
	{
		lock (StaticLock)
		{
			return (float)(Comm.GetTime() - LastActivityTime);
		}
	}

	public static void SleepUntilIdle(float idleTime)
	{
		Task.Delay(100).Wait();
		while (GetIdleTime() <= idleTime)
		{
			Task.Delay(10).Wait();
		}
	}

	public void SendPacket(Packet packet)
	{
		lock (StaticLock)
		{
			LastActivityTime = Comm.GetTime();
		}
		lock (Lock)
		{
			PacketsSent++;
			BytesSent += packet.Data.Length;
			if (DropRatio <= 0f || Random.NextDouble() >= (double)DropRatio)
			{
				if (TruncateRatio > 0f && packet.Data.Length != 0 && Random.NextDouble() < (double)TruncateRatio)
				{
					packet.Data = packet.Data.Take(Random.Next(packet.Data.Length)).ToArray();
				}
				if (ByteCorruptRatio > 0f)
				{
					packet.Data = packet.Data.ToArray();
					for (int i = 0; i < packet.Data.Length; i++)
					{
						if (Random.NextDouble() < (double)ByteCorruptRatio)
						{
							packet.Data[i] = (byte)Random.Next();
						}
					}
				}
				if (DuplicateRatio > 0f && Random.NextDouble() < (double)DuplicateRatio)
				{
					QueueAction(RandomizeDelay(), delegate
					{
						SendPacket(packet);
					});
				}
				if (MinimumDelay > 0f || MaximumDelay > 0f)
				{
					QueueAction(RandomizeDelay(), delegate
					{
						BaseTransmitter.SendPacket(packet);
					});
				}
				else
				{
					BaseTransmitter.SendPacket(packet);
				}
			}
			else
			{
				PacketsDropped++;
			}
		}
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			IsDisposed = true;
			Task.Wait();
			BaseTransmitter.Dispose();
		}
	}

	private void QueueAction(double delay, Action action)
	{
		double time = Comm.GetTime();
		double num = Math.Round((time + delay) * 100.0) / 100.0;
		if (num <= time)
		{
			action();
			return;
		}
		lock (Lock)
		{
			if (!PendingActions.TryGetValue(num, out var value))
			{
				value = new List<Action>();
				PendingActions.Add(num, value);
			}
			value.Add(action);
		}
	}

	private void ExecutePendingActions()
	{
		lock (Lock)
		{
			double time = Comm.GetTime();
			while (PendingActions.Count > 0)
			{
				KeyValuePair<double, List<Action>> keyValuePair = PendingActions.First();
				if (keyValuePair.Key > time)
				{
					break;
				}
				PendingActions.Remove(keyValuePair.Key);
				foreach (Action item in keyValuePair.Value)
				{
					item();
				}
			}
		}
	}

	private double RandomizeDelay()
	{
		float minimumDelay = MinimumDelay;
		float num = Math.Max(MaximumDelay, MinimumDelay);
		return Random.NextDouble() * (double)(num - minimumDelay) + (double)minimumDelay;
	}
}
