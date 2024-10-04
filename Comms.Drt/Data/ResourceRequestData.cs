using System.Net;

namespace Comms.Drt;
/// <summary>
/// ��ʾ��Դ��������ݽṹ�塣
/// �ýṹ��������ĳһ�ض���Դ��ָ����Դ�����ơ���Ͱ汾���Լ�����Ŀ�ĵ�ַ��
/// </summary>
public struct ResourceRequestData
{
    /// <summary>
    /// ����Ŀ��������ַ��
    /// ��ʾ��Դ���󽫷��͵���Ŀ�꣬ͨ����ĳ����������ͻ��˵� IP ��ַ�Ͷ˿ڡ�
    /// </summary>
    public IPEndPoint Address;
    /// <summary>
    /// �������Դ���ơ�
    /// ָ����Ҫ�������Դ�����ƣ������ļ�������Դ��ʶ���ȣ�����ʶ����Ҫ��ȡ�ľ�����Դ��
    /// </summary>
    public string Name;
    /// <summary>
    /// �������Դ��Ͱ汾�š�
    /// ָ���������Դ�����Ǹð汾����߰汾��ȷ���ͻ��˻��������ȡ�������»��ض��汾����Դ��
    /// </summary>
    public int MinimumVersion;
}
