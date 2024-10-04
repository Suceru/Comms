using System;
using System.Threading;

namespace Comms;
/// <summary>
/// NetworkSimulatorStats �������ռ��͹�������ģ�����е�ͳ�����ݡ�
/// ���̳��� DiagnosticStats �࣬����չ����������ϸͳ�ƹ��ܡ�
/// </summary>
public class NetworkSimulatorStats : DiagnosticStats
{
    // LastActivityTicks �ֶΣ���ʾ���һ��������ʱ�䣨��ϵͳ�δ��ʱ����
    // ��ʼֵΪ -1����ʾ��δ�����κλ��
    internal int LastActivityTicks = -1;
    /// <summary>
    /// PacketsDropped �ֶΣ���ʾģ�������Ѷ��������ݰ�������
    /// </summary>
    public long PacketsDropped;
    /// <summary>
    /// ToString ���������ص�ǰ����ͳ����Ϣ���ַ�����ʾ��
	/// ͳ�����ݰ����ѷ��ͺͽ��յ��ֽ��������ݰ������Լ���������
    /// </summary>
    /// <returns>�ַ���</returns>
    public override string ToString()
	{
        // N0 ��ʽ���ַ���������ǧλ�ָ�����ʽ������
        return $"Sent: {BytesSent:N0} bytes ({PacketsSent:N0} packets), received {BytesReceived:N0} bytes ({PacketsReceived:N0} packets), dropped {PacketsDropped:N0} packets";
	}
    /// <summary>
    /// GetIdleTime ���������㲢�����������ϴλ��Ŀ���ʱ�䣨��λ���룩��
    /// ��� LastActivityTicks ��δ�����ã��򷵻� 0 ��ʾû�п���ʱ�䡣
    /// </summary>
    /// <returns>�����ȸ�����</returns>
    public float GetIdleTime()
	{
        // ���û�м�¼���κλ���򷵻� 0 ��Ŀ���ʱ�䡣
        if (LastActivityTicks < 0)
		{
			return 0f;
		}
        // ʹ��ϵͳ�� Environment.TickCount �������ʱ�䣬����ֵΪ������
        return (float)((Environment.TickCount & 0x7FFFFFFF) - LastActivityTicks) / 1000f;
	}
    /// <summary>
    /// WaitUntilIdle ������������ǰ�߳�ֱ������ģ�����ﵽָ���Ŀ���ʱ�䡣
    /// </summary>
    /// <param name="idleTime">ָ���ȴ��Ŀ���ʱ�䣨��λ���룩</param>
	public void WaitUntilIdle(float idleTime)
	{
        // ѭ���ȴ���ֱ�� GetIdleTime() ���ص�ֵ����ָ���� idleTime��
        while (GetIdleTime() <= idleTime)
		{
            // ÿ��ѭ��ʱ���߳���ͣ 10 �����Ա���� CPU ռ�á�
            Thread.Sleep(10);
		}
	}
}
