using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Comms;
/// <summary>
/// UdpTransmitter ��ͨ�� UDP Э�鷢�ͺͽ������ݰ���֧�� IPv4 �� IPv6��ʵ���� ITransmitter �� IDisposable �ӿڡ�
/// </summary>
public class UdpTransmitter : ITransmitter, IDisposable
{
    // ��ʾ�����Ƿ��ѱ��ͷţ�ʹ�� volatile �ؼ���ȷ�����̷߳��ʵ���ȷ��
    private volatile bool IsDisposed;
    // ���ں�̨����� Task ����
    private Task Task;
    // IPv4 �� IPv6 �� Socket ��������ͨ��
    private Socket Socket4;
	private Socket Socket6;
    /// <summary>
    /// // ��ȡ IPv4 �Ĺ㲥��ַ��255.255.255.255��
    /// </summary>
    public static IPAddress IPV4BroadcastAddress { get; } = IPAddress.Broadcast;

    /// <summary>
    /// // ��ȡ IPv6 �Ĺ㲥��ַ
    /// </summary>
    public static IPAddress IPV6BroadcastAddress { get; } = IPAddress.Parse("ff08::1");

    /// <summary>
    /// ������ݰ���С��Ĭ���� 1024 �ֽ�
    /// </summary>
    public int MaxPacketSize { get; set; } = 1024;

    /// <summary>
    /// ��ǰʹ�õ������ս���ַ
    /// </summary>
    public IPEndPoint Address { get; private set; }
    /// <summary>
    /// �����¼�����������ʱ����
    /// </summary>
    public event Action<Exception> Error;
    /// <summary>
    /// �����¼���DEBUG ģʽ���������������Ϣ
    /// </summary>
    public event Action<string> Debug;
    /// <summary>
    /// ���ݰ������¼��������յ����ݰ�ʱ����
    /// </summary>
    public event Action<Packet> PacketReceived;
    /// <summary>
    /// ���캯������ʼ�� UdpTransmitter ����ʼ��̨����
    /// </summary>
    /// <param name="localPort">���ض˿�</param>
    /// <exception cref="InvalidOperationException"></exception>
    public UdpTransmitter(int localPort = 0)
	{
		try
        { 
			// ���� IPv4 UDP Socket ���󶨵�ָ���˿ڣ��� 0 ��ʾ����˿ڣ�
            Socket4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			Socket4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, optionValue: true);
			Socket4.Bind(new IPEndPoint(IPAddress.Any, localPort)); // �󶨵����ض˿�
            Socket4.ReceiveTimeout = 1000;// ���ý��ճ�ʱʱ��Ϊ 1 ��
            // ��ȡ��ǰ����ı��� IPv4 ��ַ
            if (Address == null)
			{
				try
				{
					using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					socket.Bind(new IPEndPoint(IPAddress.Any, 0));
					socket.Connect("8.8.8.8", 12345);// ͨ�����ӹ��� DNS ��������ȡ���ص�ַ
                    Address = new IPEndPoint(((IPEndPoint)socket.LocalEndPoint).Address, ((IPEndPoint)Socket4.LocalEndPoint).Port);
				}
				catch (Exception)
				{
                    // ����޷���ȡ���ص�ַ������Ϊ IPAddress.None
                    Address = new IPEndPoint(IPAddress.None, 0);
				}
			}
		}
		catch (Exception ex2)
		{
            // ���� IPv4 Socket �ĳ�ʼ������ȷ���ͷ���Դ
            Socket4?.Dispose();
			Socket4 = null;
            // ����Ƕ˿��ѱ�ռ�õ��쳣��ֱ���׳�
            if (ex2 is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse } ex3)
			{
				throw ex3;
			}
		}
		try
		{
            // ���� IPv6 UDP Socket ���󶨵�ָ���˿�
            Socket6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
			Socket6.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, optionValue: true);
			Socket6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, optionValue: true);
			Socket6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(IPV6BroadcastAddress));
			Socket6.Bind(new IPEndPoint(IPAddress.IPv6Any, localPort));// �󶨵����ض˿�
            Socket6.ReceiveTimeout = 1000;
            // ��ȡ��ǰ����ı��� IPv6 ��ַ
            if (Address == null)
			{
				try
				{
					using Socket socket2 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
					socket2.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, optionValue: true);
					socket2.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
					socket2.Connect("2001:4860:4860::8888", 12345);// ͨ�����ӹ��� DNS ��������ȡ���ص�ַ
                    Address = new IPEndPoint(((IPEndPoint)socket2.LocalEndPoint).Address, ((IPEndPoint)Socket6.LocalEndPoint).Port);
				}
				catch (Exception)
				{
                    // ����޷���ȡ���ص�ַ������Ϊ IPv6 None ��ַ
                    Address = new IPEndPoint(IPAddress.IPv6None, 0);
				}
			}
		}
		catch (Exception ex5)
		{
            // ���� IPv6 Socket �ĳ�ʼ������ȷ���ͷ���Դ
            Socket6?.Dispose();
			Socket6 = null;
            // ����Ƕ˿��ѱ�ռ�õ��쳣��ֱ���׳�
            if (ex5 is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse } ex6)
			{
				throw ex6;
			}
		}
        // ��� IPv4 �� IPv6 Socket ��δ�ܴ������׳��쳣
        if (Socket4 == null && Socket6 == null)
		{
			throw new InvalidOperationException("No network connectivity.");
		}

        // ������������̨����
        Task = new Task(TaskFunction, TaskCreationOptions.LongRunning);
		Task.Start();
	}
    /// <summary>
    ///  ʵ�� IDisposable �ӿڣ������ͷ���Դ
    /// </summary>
    public void Dispose()
	{
		if (!IsDisposed)
		{
			IsDisposed = true;
			Task.Wait();// �ȴ���̨�������
            Socket4?.Dispose();// �ͷ� IPv4 Socket
            Socket6?.Dispose();// �ͷ� IPv6 Socket
        }
	}
    /// <summary>
    /// �������ݰ���ͨ�� IPv4 �� IPv6 Socket ��������
    /// </summary>
    /// <param name="packet">��</param>
    public void SendPacket(Packet packet)
	{
		CheckNotDisposed(); // ȷ������δ���ͷ�
                            // �������ݰ��ĵ�ַ����ѡ����ʵ� Socket
        if (packet.Address.AddressFamily == AddressFamily.InterNetwork && Socket4 != null)
		{
			Socket4.SendTo(packet.Bytes, packet.Address);
		}
		else if (packet.Address.AddressFamily == AddressFamily.InterNetworkV6 && Socket6 != null)
		{
			Socket6.SendTo(packet.Bytes, packet.Address);
		}
	}
    // ��̨������������������ݰ�
    private void TaskFunction()
	{
        // �����߳�����
        Thread.CurrentThread.Name = "UdpTransmitter";
		List<Socket> list = new List<Socket>();// �洢Ҫ������ Socket �б�
        byte[] array = new byte[65536];// ���ջ�����
        while (!IsDisposed)// �������δ���ͷţ�ѭ������
        {
			try
			{
				list.Clear();// ��� Socket �б�
                if (Socket4 != null)
				{
					list.Add(Socket4);// ��� IPv4 Socket
                }
				if (Socket6 != null)
				{
					list.Add(Socket6);// ��� IPv6 Socket
                }
                // ʹ�� Select ����������� Socket �Ŀɶ��¼�
                Socket.Select(list, null, null, 1000000);
                // �������������ݵ� Socket
                foreach (Socket item in list)
				{
					EndPoint remoteEP = ((item.AddressFamily != AddressFamily.InterNetwork) ? new IPEndPoint(IPAddress.IPv6Any, 0) : new IPEndPoint(IPAddress.Any, 0));
					int num = item.ReceiveFrom(array, ref remoteEP);// ��������
                    byte[] array2 = new byte[num];
					Array.Copy(array, 0, array2, 0, num);// ���ƽ��յ�������
                    InvokePacketReceived((IPEndPoint)remoteEP, array2); // �������ݰ������¼�
                }
			}
			catch (SocketException ex)
			{
                // �����ض��� Socket ����
                if (ex.SocketErrorCode != SocketError.TimedOut && ex.SocketErrorCode != SocketError.Interrupted && ex.SocketErrorCode != SocketError.ConnectionReset)
				{
					InvokeError(ex);// ���������¼�
                }
			}
		}
	}
    // ȷ������δ���ͷţ������׳� ObjectDisposedException �쳣
    private void CheckNotDisposed()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("UdpTransmitter");
		}
	}
    // ���� PacketReceived �¼�
    private void InvokePacketReceived(IPEndPoint address, byte[] bytes)
	{
		this.PacketReceived?.Invoke(new Packet(address, bytes));
	}
    // ���� Error �¼�
    private void InvokeError(Exception error)
	{
		this.Error?.Invoke(error);
	}
    // ���� Debug �¼������� DEBUG ģʽ����Ч
    [Conditional("DEBUG")]
	private void InvokeDebug(string format, params object[] args)
	{
		if (this.Debug != null)
		{
			this.Debug?.Invoke(string.Format(format, args));
		}
	}
}
