namespace Comms.Drt;
/// <summary>
///DesyncDetectionMode ��һ��ö�٣�����ָ����ͬ�Ĳ�ͬ�����ģʽ��
///������ͨ�Ż���Ϸͬ��ϵͳ�У���ͬ�����ܻᵼ�¿ͻ��˺ͷ�������״̬��һ�¡�
///���ö���ṩ�˼���ģʽ�������Ƿ��Լ���μ�ⲻͬ�����⡣
/// </summary>
public enum DesyncDetectionMode
{
    /// <summary>
    /// None ��ʾ�������κβ�ͬ����⡣
    /// ������ģʽ�£�ϵͳ������ͻ��˺ͷ�����֮���״̬�Ƿ�ͬ����
    /// </summary>
    None,
    /// <summary>
    ///Detect ��ʾ���в�ͬ����⣬�������嶨λ��ͬ�������ĵط���
    ///  ϵͳֻ�����Ƿ���ڲ�ͬ�����⣬�������ṩ����Ĵ�����Ϣ��ϸ�ڡ�
    /// </summary>
	Detect,
    /// <summary>
    /// Locate ��ʾ������ⲻͬ�������᳢�Զ�λ��ͬ�������ľ���λ�û�ԭ��
    /// ������ģʽ�£�ϵͳ�������ϸ�ط�����ͬ�����⣬���ṩ����ĵ�����Ϣ��
    /// </summary>
	Locate
}