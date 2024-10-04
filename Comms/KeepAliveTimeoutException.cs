using System.Net;

namespace Comms;
/// <summary>
/// KeepAliveTimeoutException ��̳��� ProtocolViolationException��
/// �����ʾ������ͨ���У����� "Keep Alive" ���Ƴ�ʱ��������Э��Υ���쳣��
/// "Keep Alive" ��һ������ά���������ӻ�Ļ��ƣ�ͨ�����ڼ�ⳤʱ����е���������
/// </summary>
public class KeepAliveTimeoutException : ProtocolViolationException
{
    /// <summary>
    /// ���캯��������һ����Ϣ�ַ�������ʾ��ʱʱ�Ĵ�����Ϣ��
    /// </summary>
    /// <param name="message">�����쳣����ϸ��Ϣ���ַ�����</param>
    public KeepAliveTimeoutException(string message)
		: base(message)// ���û��� ProtocolViolationException �Ĺ��캯���������쳣��Ϣ��
    {
	}
}
