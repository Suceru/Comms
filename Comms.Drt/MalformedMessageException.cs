using System.Net;

namespace Comms.Drt;
// MalformedMessageException ����һ���Զ����쳣���̳��� ProtocolViolationException��
// �����ڱ�ʾ�ڴ�����Ϣʱ�������˲�����Ԥ�ڸ�ʽ��Э�����Ϣ��
internal class MalformedMessageException : ProtocolViolationException
{
    // SenderAddress �����˷��͸ô�����Ϣ�ķ����ߵ� IP ��ַ�Ͷ˿ڡ�
    // ����������׷���ĸ�Զ�̶˵㣨IP ��ַ�Ͷ˿ڣ���������Ч����Ϣ��
    public IPEndPoint SenderAddress;

    // ���캯�������ڳ�ʼ�� MalformedMessageException ��ʵ����
    // message ���쳣����������Ϣ��senderAddress �Ƿ��͸���Ϣ��Զ�̶˵㡣
    public MalformedMessageException(string message, IPEndPoint senderAddress)
		: base(message)// ���û��� ProtocolViolationException �Ĺ��캯�����������쳣��Ϣ��
    {
        // ����Զ�̷����ߵ� IP ��ַ�Ͷ˿ڵ� SenderAddress ���ԡ�
        SenderAddress = senderAddress;
	}
}
