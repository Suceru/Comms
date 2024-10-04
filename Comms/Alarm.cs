using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Comms;
/// <summary>
/// 表示一个可调度的定时器，用于在指定延迟后执行操作。
/// 实现了 IDisposable 接口，以便在不再需要时释放资源。
/// </summary>
public class Alarm : IDisposable
{
    // 用于执行任务的静态 Task
    private static Task Task;
    // 用于线程间同步的 AutoResetEvent
    private static AutoResetEvent WaitEvent;
    // 存储所有定时器的链表
    private static LinkedList<Alarm> Alarms;
    // 标识是否已经被释放
    private volatile bool IsDisposed;
    // 链表节点，用于在链表中跟踪该 Alarm 实例
    private LinkedListNode<Alarm> Node;
    // 处理定时器到期时要执行的操作
    private Action Handler;
    // 定时器的到期时间
    private double DueTime;
    
    /// /// <summary>
    /// 发生错误时触发的事件。
    /// </summary>
    public event Action<Exception> Error;
    /// <summary>
    /// 静态构造函数，初始化静态资源。
    /// </summary>
	static Alarm()
	{
		WaitEvent = new AutoResetEvent(initialState: false);// 初始化自动重置事件
        Alarms = new LinkedList<Alarm>();// 初始化链表
        Task = new Task(TaskFunction, TaskCreationOptions.LongRunning);// 创建任务以处理定时器
        Task.Start();// 启动任务
    }
    /// <summary>
    /// 构造函数，创建一个新的 Alarm 实例。
    /// </summary>
    /// <param name="handler">定时器到期时要执行的操作</param>
    /// <exception cref="ArgumentNullException">当 handler 为 null 时抛出</exception>
    public Alarm(Action handler)
	{
		Node = new LinkedListNode<Alarm>(this);// 创建链表节点
        Handler = handler ?? throw new ArgumentNullException("handler");// 确保 handler 不为 null
	}
    /// <summary>
    /// 释放资源。
    /// </summary>
    public void Dispose()
	{
		lock (Alarms)
		{
			IsDisposed = true;// 标记为已释放
        }
	}
    /// <summary>
    /// 设置定时器的延迟时间。
    /// </summary>
    /// <param name="delay">延迟时间（以秒为单位）</param>
    /// <exception cref="ObjectDisposedException">当 Alarm 已被释放时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当延迟时间小于0时抛出</exception>
    public void Set(double delay)
	{
        // 检查 Alarm 是否已被释放
        lock (Alarms)
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException("Alarm");
			}
            // 检查延迟时间是否合法
            if (delay < 0.0)
			{
				throw new ArgumentOutOfRangeException("delay");
			}
            // 如果延迟时间为0，则立即执行操作
            if (delay == 0.0)
			{
				if (Node.List != null)
				{
					Alarms.Remove(Node);// 从链表中移除
                }
				Task.Run((Action)AlarmFunction);// 立即运行操作
                return;
			}
            // 计算到期时间
            DueTime = Comm.GetTime() + delay;// 设置到期时间
            if (Node.List == null)// 如果当前节点不在链表中
            {
				if (DueTime != double.PositiveInfinity)
				{
					Alarms.AddLast(Node);// 将节点添加到链表
                }
			}
			else if (DueTime == double.PositiveInfinity)
			{
				Alarms.Remove(Node);// 如果设置为正无穷，移除节点
            }
			WaitEvent.Set();// 通知等待的线程
        }
	}
    /// <summary>
    /// 定时器到期时执行的操作。
    /// </summary>
    private void AlarmFunction()
	{
		try
		{
			Handler();// 执行操作
        }
		catch (Exception obj)
		{
            // 如果发生错误，触发 Error 事件
            this.Error?.Invoke(obj);
		}
	}
    /// <summary>
    /// 处理所有定时器的静态方法。
    /// </summary>
    private static void TaskFunction()
	{
		Thread.CurrentThread.Name = "Alarm";// 设置线程名称
        while (true)
		{
			double num = double.MaxValue;// 初始化最小延迟
            lock (Alarms)
			{
				double time = Comm.GetTime();// 获取当前时间
                LinkedListNode<Alarm> linkedListNode = Alarms.First;// 从链表中获取第一个节点
                while (linkedListNode != null)
				{
					LinkedListNode<Alarm> next = linkedListNode.Next;// 获取下一个节点
                    Alarm value = linkedListNode.Value;// 获取当前 Alarm 实例
                    // 检查 Alarm 是否已被释放
                    if (value.IsDisposed)
					{
						Alarms.Remove(linkedListNode);// 从链表中移除
                    }
					else
					{
						double num2 = value.DueTime - time;// 计算到期时间与当前时间的差值
                        if (num2 <= 0.0)
						{
							Alarms.Remove(linkedListNode);// 从链表中移除
                            Task.Run((Action)value.AlarmFunction);// 执行到期操作
                        }
						else
						{
							num = Math.Min(num, num2);// 更新最小延迟
                        }
					}
					linkedListNode = next;// 移动到下一个节点
                }
			}
			if (num < double.MaxValue)// 等待最小延迟或直到有新的定时器设置
            {
				WaitEvent.WaitOne((int)(Math.Min(num, 60.0) * 1000.0));// 转换为毫秒并等待
            }
			else
			{
				WaitEvent.WaitOne();// 等待通知
            }
		}
	}
}
