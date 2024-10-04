namespace Comms.Drt;
/// <summary>
/// ̽��������
/// ����������������̽����صĲ������������غͻ������ķ������ڼ��Ƴ�ʱ�䡣
/// ��Щ���ð����ͻ����������з��������豸�����
/// </summary>
public class ExplorerSettings
{
    /// <summary>
    /// ���ط������ڣ���λΪ��
    /// 
    /// �����Զ����˿ͻ����ڱ��������н����豸������ֵ�ʱ������
    /// ���磬�ͻ���ÿ�������ڼ��һ�α��������ϵ������豸�����
    /// Ĭ��ֵΪ 0.5 �룬���Ը������绷����������е�����
    /// </summary>
    public float LocalDiscoveryPeriod = 0.5f;
    /// <summary>
    /// �������������ڣ���λΪ��
    /// 
    /// �����Զ����˿ͻ����ڻ������Ͻ����豸������ֵ�ʱ������
    /// ���磬�ͻ���ÿ�������ڼ��һ�λ������Ͽ��õķ�����豸��
    /// Ĭ��ֵΪ 3 �룬�ʺϽϳ��������ӳٻ�������縺����
    /// </summary>
    public float InternetDiscoveryPeriod = 3f;
    /// <summary>
    /// �����Ƴ�ʱ�䣬��λΪ��
    /// 
    /// �����Զ����˿ͻ����ڱ����������Ƴ����ٿ��õ��豸�����ĵȴ�ʱ�䡣
    /// ���豸�ڴ�ʱ����δ������ʱ���ͻ��˻Ὣ���Ƴ���
    /// Ĭ��ֵΪ 3 �룬�ʺϽϿ�ı������绷����
    /// </summary>
    public float LocalRemoveTime = 3f;
    /// <summary>
    /// �������Ƴ�ʱ�䣬��λΪ��
    /// 
    /// �����Զ����˿ͻ����ڻ��������Ƴ����ٿ��õ��豸�����ĵȴ�ʱ�䡣
    /// ���豸�ڴ�ʱ����δ������ʱ���ͻ��˻Ὣ���Ƴ���
    /// Ĭ��ֵΪ 7 �룬�ʺϽϳ��������ӳ١�
    /// </summary>
    public float InternetRemoveTime = 7f;
}