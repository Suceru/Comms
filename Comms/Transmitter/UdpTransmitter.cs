using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Comms;
/// <summary>
/// UdpTransmitter 类通过 UDP 协议发送和接收数据包，支持 IPv4 和 IPv6。实现了 ITransmitter 和 IDisposable 接口。
/// </summary>
public class UdpTransmitter : ITransmitter, IDisposable
{
    // 表示对象是否已被释放，使用 volatile 关键字确保多线程访问的正确性
    private volatile bool IsDisposed;
    // 用于后台任务的 Task 对象
    private Task Task;
    // IPv4 和 IPv6 的 Socket 对象，用于通信
    private Socket Socket4;
	private Socket Socket6;
    /// <summary>
    /// // 获取 IPv4 的广播地址（255.255.255.255）
    /// </summary>
    public static IPAddress IPV4BroadcastAddress { get; } = IPAddress.Broadcast;

    /// <summary>
    /// // 获取 IPv6 的广播地址
    /// </summary>
    public static IPAddress IPV6BroadcastAddress { get; } = IPAddress.Parse("ff08::1");

    /// <summary>
    /// 最大数据包大小，默认是 1024 字节
    /// </summary>
    public int MaxPacketSize { get; set; } = 1024;

    /// <summary>
    /// 当前使用的网络终结点地址
    /// </summary>
    public IPEndPoint Address { get; private set; }
    /// <summary>
    /// 错误事件，发生错误时触发
    /// </summary>
    public event Action<Exception> Error;
    /// <summary>
    /// 调试事件，DEBUG 模式下用于输出调试信息
    /// </summary>
    public event Action<string> Debug;
    /// <summary>
    /// 数据包接收事件，当接收到数据包时触发
    /// </summary>
    public event Action<Packet> PacketReceived;
    /// <summary>
    /// 构造函数，初始化 UdpTransmitter 并开始后台任务
    /// </summary>
    /// <param name="localPort">本地端口</param>
    /// <exception cref="InvalidOperationException"></exception>
    public UdpTransmitter(int localPort = 0)
	{
		try
        { 
			// 创建 IPv4 UDP Socket 并绑定到指定端口（或 0 表示随机端口）
            Socket4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			Socket4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, optionValue: true);
			Socket4.Bind(new IPEndPoint(IPAddress.Any, localPort)); // 绑定到本地端口
            Socket4.ReceiveTimeout = 1000;// 设置接收超时时间为 1 秒
            // 获取当前网络的本地 IPv4 地址
            if (Address == null)
			{
				try
				{
					using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					socket.Bind(new IPEndPoint(IPAddress.Any, 0));
					socket.Connect("8.8.8.8", 12345);// 通过连接公共 DNS 服务器获取本地地址
                    Address = new IPEndPoint(((IPEndPoint)socket.LocalEndPoint).Address, ((IPEndPoint)Socket4.LocalEndPoint).Port);
				}
				catch (Exception)
				{
                    // 如果无法获取本地地址，设置为 IPAddress.None
                    Address = new IPEndPoint(IPAddress.None, 0);
				}
			}
		}
		catch (Exception ex2)
		{
            // 处理 IPv4 Socket 的初始化错误，确保释放资源
            Socket4?.Dispose();
			Socket4 = null;
            // 如果是端口已被占用的异常，直接抛出
            if (ex2 is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse } ex3)
			{
				throw ex3;
			}
		}
		try
		{
            // 创建 IPv6 UDP Socket 并绑定到指定端口
            Socket6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
			Socket6.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, optionValue: true);
			Socket6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, optionValue: true);
			Socket6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(IPV6BroadcastAddress));
			Socket6.Bind(new IPEndPoint(IPAddress.IPv6Any, localPort));// 绑定到本地端口
            Socket6.ReceiveTimeout = 1000;
            // 获取当前网络的本地 IPv6 地址
            if (Address == null)
			{
				try
				{
					using Socket socket2 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
					socket2.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, optionValue: true);
					socket2.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
					socket2.Connect("2001:4860:4860::8888", 12345);// 通过连接公共 DNS 服务器获取本地地址
                    Address = new IPEndPoint(((IPEndPoint)socket2.LocalEndPoint).Address, ((IPEndPoint)Socket6.LocalEndPoint).Port);
				}
				catch (Exception)
				{
                    // 如果无法获取本地地址，设置为 IPv6 None 地址
                    Address = new IPEndPoint(IPAddress.IPv6None, 0);
				}
			}
		}
		catch (Exception ex5)
		{
            // 处理 IPv6 Socket 的初始化错误，确保释放资源
            Socket6?.Dispose();
			Socket6 = null;
            // 如果是端口已被占用的异常，直接抛出
            if (ex5 is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse } ex6)
			{
				throw ex6;
			}
		}
        // 如果 IPv4 和 IPv6 Socket 都未能创建，抛出异常
        if (Socket4 == null && Socket6 == null)
		{
			throw new InvalidOperationException("No network connectivity.");
		}

        // 创建并启动后台任务
        Task = new Task(TaskFunction, TaskCreationOptions.LongRunning);
		Task.Start();
	}
    /// <summary>
    ///  实现 IDisposable 接口，用于释放资源
    /// </summary>
    public void Dispose()
	{
		if (!IsDisposed)
		{
			IsDisposed = true;
			Task.Wait();// 等待后台任务完成
            Socket4?.Dispose();// 释放 IPv4 Socket
            Socket6?.Dispose();// 释放 IPv6 Socket
        }
	}
    /// <summary>
    /// 发送数据包，通过 IPv4 或 IPv6 Socket 发送数据
    /// </summary>
    /// <param name="packet">包</param>
    public void SendPacket(Packet packet)
	{
		CheckNotDisposed(); // 确保对象未被释放
                            // 根据数据包的地址类型选择合适的 Socket
        if (packet.Address.AddressFamily == AddressFamily.InterNetwork && Socket4 != null)
		{
			Socket4.SendTo(packet.Bytes, packet.Address);
		}
		else if (packet.Address.AddressFamily == AddressFamily.InterNetworkV6 && Socket6 != null)
		{
			Socket6.SendTo(packet.Bytes, packet.Address);
		}
	}
    // 后台任务函数，负责接收数据包
    private void TaskFunction()
	{
        // 设置线程名称
        Thread.CurrentThread.Name = "UdpTransmitter";
		List<Socket> list = new List<Socket>();// 存储要监听的 Socket 列表
        byte[] array = new byte[65536];// 接收缓冲区
        while (!IsDisposed)// 如果对象未被释放，循环监听
        {
			try
			{
				list.Clear();// 清空 Socket 列表
                if (Socket4 != null)
				{
					list.Add(Socket4);// 添加 IPv4 Socket
                }
				if (Socket6 != null)
				{
					list.Add(Socket6);// 添加 IPv6 Socket
                }
                // 使用 Select 方法监听多个 Socket 的可读事件
                Socket.Select(list, null, null, 1000000);
                // 处理所有有数据的 Socket
                foreach (Socket item in list)
				{
					EndPoint remoteEP = ((item.AddressFamily != AddressFamily.InterNetwork) ? new IPEndPoint(IPAddress.IPv6Any, 0) : new IPEndPoint(IPAddress.Any, 0));
					int num = item.ReceiveFrom(array, ref remoteEP);// 接收数据
                    byte[] array2 = new byte[num];
					Array.Copy(array, 0, array2, 0, num);// 复制接收到的数据
                    InvokePacketReceived((IPEndPoint)remoteEP, array2); // 触发数据包接收事件
                }
			}
			catch (SocketException ex)
			{
                // 处理特定的 Socket 错误
                if (ex.SocketErrorCode != SocketError.TimedOut && ex.SocketErrorCode != SocketError.Interrupted && ex.SocketErrorCode != SocketError.ConnectionReset)
				{
					InvokeError(ex);// 触发错误事件
                }
			}
		}
	}
    // 确保对象未被释放，否则抛出 ObjectDisposedException 异常
    private void CheckNotDisposed()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("UdpTransmitter");
		}
	}
    // 触发 PacketReceived 事件
    private void InvokePacketReceived(IPEndPoint address, byte[] bytes)
	{
		this.PacketReceived?.Invoke(new Packet(address, bytes));
	}
    // 触发 Error 事件
    private void InvokeError(Exception error)
	{
		this.Error?.Invoke(error);
	}
    // 触发 Debug 事件，仅在 DEBUG 模式下有效
    [Conditional("DEBUG")]
	private void InvokeDebug(string format, params object[] args)
	{
		if (this.Debug != null)
		{
			this.Debug?.Invoke(string.Format(format, args));
		}
	}
}
