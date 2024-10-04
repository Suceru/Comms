using System.Net;

namespace Comms.Drt;
/// <summary>
/// ServerDescription �����ڴ洢���������ص�������Ϣ����Ҫ���ڷ����֡�������Ϸ��ֲ�ʽͨ��ϵͳ�еķ�������Ϣ��
/// </summary>
public class ServerDescription
{
    /// <summary>
    /// �������������ַ��IP ��ַ�Ͷ˿ںţ���
    /// </summary>
    public IPEndPoint Address;
    /// <summary>
    /// ָʾ�������Ƿ�Ϊ���ط����������Ϊ true����ʾ�÷�����λ�ڱ��������С�
    /// </summary>
    public bool IsLocal;
    /// <summary>
    /// �����������ֵ�ʱ�����ͨ������ĳ���ο�����������������
    /// </summary>
	public double DiscoveryTime;
    /// <summary>
    /// ���������ӳ٣��Ժ���Ϊ��λ����Ping ֵԽ�ͣ�˵�������ӳ�ԽС��
    /// </summary>
	public float Ping;
    /// <summary>
    /// �����������ƣ����ڱ�ʶ���������Ѻ����ơ�
    /// </summary>
	public string Name;
    /// <summary>
    /// �����������ȼ�ֵ�����ȼ�Խ�ߵķ������������ȿ���ʹ�á�
    /// </summary>
	public int Priority;
    /// <summary>
    /// �÷����������ṩ����Ϸ�������顣ÿ�� GameDescription ������һ�����������ṩ����Ϸ��
    /// </summary>
	public GameDescription[] GameDescriptions;
}
