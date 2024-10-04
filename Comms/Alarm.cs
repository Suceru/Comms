using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Comms;
/// <summary>
/// ��ʾһ���ɵ��ȵĶ�ʱ����������ָ���ӳٺ�ִ�в�����
/// ʵ���� IDisposable �ӿڣ��Ա��ڲ�����Ҫʱ�ͷ���Դ��
/// </summary>
public class Alarm : IDisposable
{
    // ����ִ������ľ�̬ Task
    private static Task Task;
    // �����̼߳�ͬ���� AutoResetEvent
    private static AutoResetEvent WaitEvent;
    // �洢���ж�ʱ��������
    private static LinkedList<Alarm> Alarms;
    // ��ʶ�Ƿ��Ѿ����ͷ�
    private volatile bool IsDisposed;
    // ����ڵ㣬�����������и��ٸ� Alarm ʵ��
    private LinkedListNode<Alarm> Node;
    // ����ʱ������ʱҪִ�еĲ���
    private Action Handler;
    // ��ʱ���ĵ���ʱ��
    private double DueTime;
    
    /// /// <summary>
    /// ��������ʱ�������¼���
    /// </summary>
    public event Action<Exception> Error;
    /// <summary>
    /// ��̬���캯������ʼ����̬��Դ��
    /// </summary>
	static Alarm()
	{
		WaitEvent = new AutoResetEvent(initialState: false);// ��ʼ���Զ������¼�
        Alarms = new LinkedList<Alarm>();// ��ʼ������
        Task = new Task(TaskFunction, TaskCreationOptions.LongRunning);// ���������Դ���ʱ��
        Task.Start();// ��������
    }
    /// <summary>
    /// ���캯��������һ���µ� Alarm ʵ����
    /// </summary>
    /// <param name="handler">��ʱ������ʱҪִ�еĲ���</param>
    /// <exception cref="ArgumentNullException">�� handler Ϊ null ʱ�׳�</exception>
    public Alarm(Action handler)
	{
		Node = new LinkedListNode<Alarm>(this);// ��������ڵ�
        Handler = handler ?? throw new ArgumentNullException("handler");// ȷ�� handler ��Ϊ null
	}
    /// <summary>
    /// �ͷ���Դ��
    /// </summary>
    public void Dispose()
	{
		lock (Alarms)
		{
			IsDisposed = true;// ���Ϊ���ͷ�
        }
	}
    /// <summary>
    /// ���ö�ʱ�����ӳ�ʱ�䡣
    /// </summary>
    /// <param name="delay">�ӳ�ʱ�䣨����Ϊ��λ��</param>
    /// <exception cref="ObjectDisposedException">�� Alarm �ѱ��ͷ�ʱ�׳�</exception>
    /// <exception cref="ArgumentOutOfRangeException">���ӳ�ʱ��С��0ʱ�׳�</exception>
    public void Set(double delay)
	{
        // ��� Alarm �Ƿ��ѱ��ͷ�
        lock (Alarms)
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException("Alarm");
			}
            // ����ӳ�ʱ���Ƿ�Ϸ�
            if (delay < 0.0)
			{
				throw new ArgumentOutOfRangeException("delay");
			}
            // ����ӳ�ʱ��Ϊ0��������ִ�в���
            if (delay == 0.0)
			{
				if (Node.List != null)
				{
					Alarms.Remove(Node);// ���������Ƴ�
                }
				Task.Run((Action)AlarmFunction);// �������в���
                return;
			}
            // ���㵽��ʱ��
            DueTime = Comm.GetTime() + delay;// ���õ���ʱ��
            if (Node.List == null)// �����ǰ�ڵ㲻��������
            {
				if (DueTime != double.PositiveInfinity)
				{
					Alarms.AddLast(Node);// ���ڵ���ӵ�����
                }
			}
			else if (DueTime == double.PositiveInfinity)
			{
				Alarms.Remove(Node);// �������Ϊ������Ƴ��ڵ�
            }
			WaitEvent.Set();// ֪ͨ�ȴ����߳�
        }
	}
    /// <summary>
    /// ��ʱ������ʱִ�еĲ�����
    /// </summary>
    private void AlarmFunction()
	{
		try
		{
			Handler();// ִ�в���
        }
		catch (Exception obj)
		{
            // ����������󣬴��� Error �¼�
            this.Error?.Invoke(obj);
		}
	}
    /// <summary>
    /// �������ж�ʱ���ľ�̬������
    /// </summary>
    private static void TaskFunction()
	{
		Thread.CurrentThread.Name = "Alarm";// �����߳�����
        while (true)
		{
			double num = double.MaxValue;// ��ʼ����С�ӳ�
            lock (Alarms)
			{
				double time = Comm.GetTime();// ��ȡ��ǰʱ��
                LinkedListNode<Alarm> linkedListNode = Alarms.First;// �������л�ȡ��һ���ڵ�
                while (linkedListNode != null)
				{
					LinkedListNode<Alarm> next = linkedListNode.Next;// ��ȡ��һ���ڵ�
                    Alarm value = linkedListNode.Value;// ��ȡ��ǰ Alarm ʵ��
                    // ��� Alarm �Ƿ��ѱ��ͷ�
                    if (value.IsDisposed)
					{
						Alarms.Remove(linkedListNode);// ���������Ƴ�
                    }
					else
					{
						double num2 = value.DueTime - time;// ���㵽��ʱ���뵱ǰʱ��Ĳ�ֵ
                        if (num2 <= 0.0)
						{
							Alarms.Remove(linkedListNode);// ���������Ƴ�
                            Task.Run((Action)value.AlarmFunction);// ִ�е��ڲ���
                        }
						else
						{
							num = Math.Min(num, num2);// ������С�ӳ�
                        }
					}
					linkedListNode = next;// �ƶ�����һ���ڵ�
                }
			}
			if (num < double.MaxValue)// �ȴ���С�ӳٻ�ֱ�����µĶ�ʱ������
            {
				WaitEvent.WaitOne((int)(Math.Min(num, 60.0) * 1000.0));// ת��Ϊ���벢�ȴ�
            }
			else
			{
				WaitEvent.WaitOne();// �ȴ�֪ͨ
            }
		}
	}
}
