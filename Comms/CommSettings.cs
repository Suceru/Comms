namespace Comms;
/// <summary>
/// CommSettings �����ڶ�������ͨ�ŵ����ò�����
/// ���ṩ�˶�ͨ�Ź����е��ش���Ϊ���ظ�������Լ�����ʱ��Ŀ��ơ�
/// </summary>
public class CommSettings
{
    /// <summary>
    /// MaxResends ���Ա�ʾ�����ݴ���ʧ��ʱ�����������ش�������
    /// ���������ֵ��������ش�����Ϊ����ʧ�ܡ�
    /// Ĭ��ֵΪ30����ʾ��������ش�30�Ρ�
    /// </summary>
    public int MaxResends { get; set; } = 30;

    /// <summary>
    /// ResendPeriods ���Զ������ش����ڵ�ʱ������
    /// ����һ��������������ֵ�����飬��һ��ֵ��ʾ�����ش����ӳ�ʱ�䣬�ڶ���ֵ��ʾÿ�κ����ش��ļ��ʱ�䡣
    /// Ĭ��ֵΪ [0.5f, 1f]����ʾ�״��ش��� 0.5 ��󴥷��������ش�ÿ�� 1 �����һ�Ρ�
    /// </summary>
    public float[] ResendPeriods { get; set; } = new float[2] { 0.5f, 1f };

    /// <summary>
    /// DuplicatePacketsDetectionTime ���Զ�����ϵͳ�ڶ೤ʱ���ڼ�Ⲣ�����ظ����ݰ���
    /// ��ֵ�ĵ�λ���룬Ĭ��ֵΪ 20 �롣
    /// �����ʱ����ڣ�������յ���ͬ�����ݰ�����ᱻ��Ϊ�ظ����������ԡ�
    /// </summary>
    public float DuplicatePacketsDetectionTime { get; set; } = 20f;

    /// <summary>
    /// IdleTime ���Ա�ʾ��ͨ��ͨ����û���κ����ݰ��ʱ�������೤ʱ��ϵͳ�Ὣ����Ϊ���á�
    /// ��ֵ�ĵ�λ���룬Ĭ��ֵΪ 120 �롣
    /// ���������ʱ��û���κ�ͨ�Ż�����ܻᴥ����ʱ�����ӶϿ�����Ϊ��
    /// </summary>
    public float IdleTime { get; set; } = 120f;

}
