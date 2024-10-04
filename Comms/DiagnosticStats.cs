namespace Comms;
/// <summary>
/// DiagnosticStats �������ռ��ʹ洢����ͨ�Ź����������ݰ����ֽ���ص�ͳ����Ϣ��
/// ����������ͺͽ��յ����ݰ������ֽ��������ṩ��һ����������Щͳ������ת��Ϊ�ַ�����ʽ��
/// </summary>
public class DiagnosticStats
{
    /// <summary>
    /// ��¼���յ������ݰ�����
    /// </summary>
    public long PacketsReceived;
    /// <summary>
    /// ��¼���ͳ�ȥ�����ݰ�����
    /// </summary>
    public long PacketsSent;
    /// <summary>
    /// ��¼���ͳ�ȥ�����ֽ���
    /// </summary>
	public long BytesSent;
    /// <summary>
    /// ��¼���յ������ֽ���
    /// </summary>
	public long BytesReceived;
    /// <summary>
    /// ��д ToString ���������ذ���ͳ����Ϣ���ַ�����
    /// ʹ���˸�ʽ����������ֽ��������ݰ������Ա�׼����ǧλ�ָ�����ʾ (N0)��
    /// </summary>
    /// <returns>�ַ�����ʽ��ͳ����Ϣ���������͵��ֽں����ݰ��������Լ����յ��ֽں����ݰ�������</returns>
	public override string ToString()
	{
		return $"Sent {BytesSent:N0} bytes ({PacketsSent:N0} packets), received {BytesReceived:N0} bytes ({PacketsReceived:N0} packets)";
	}
}
