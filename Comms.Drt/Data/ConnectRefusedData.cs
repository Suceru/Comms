using System.Net;

namespace Comms.Drt;
/// <summary>
/// ��ʾ���ӱ��ܾ�ʱ�����ݽṹ��
/// </summary>
public struct ConnectRefusedData
{
    /// <summary>
    /// ���������Ŀ���ַ��
    /// </summary>
    public IPEndPoint Address;
    /// <summary>
    /// �ܾ����ӵ�ԭ��
    /// </summary>
    public string Reason;
}
