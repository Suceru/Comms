using System.Collections.Generic;
using System.Threading;

namespace Comms;

internal class ProducerConsumerQueue<T>
{
	private Queue<T> Queue = new Queue<T>();

	public void Add(T t)
	{
		lock (Queue)
		{
			Queue.Enqueue(t);
			Monitor.PulseAll(Queue);
		}
	}

	public bool TryTake(out T t, int timeout)
	{
		lock (Queue)
		{
			while (Queue.Count == 0)
			{
				if (!Monitor.Wait(Queue, timeout))
				{
					t = default(T);
					return false;
				}
			}
			t = Queue.Dequeue();
			return true;
		}
	}
}
