using System.Net;

namespace Comms.Drt;
/// <summary>
/// ��ʾ��Ϸ�����¼������ݽṹ��
/// ��һ������Ϸ������ʱ��ʹ�øýṹ��������Ϸ�����ߵĵ�ַ��Ϣ��
/// </summary>
public struct GameCreatedData
{
    /// <summary>
    /// ��ʾ��Ϸ�����ߵ������ս���ַ��
    /// ����һ������IP��ַ�Ͷ˿ںŵ� IPEndPoint ʵ����
    /// </summary>
    public IPEndPoint CreatorAddress;
}
