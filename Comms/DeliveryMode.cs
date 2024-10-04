namespace Comms;
/// <summary>
///DeliveryMode ö������ָ�����ݰ�������ͨ���еĴ���ģʽ��
///��ͬ��ģʽ�ṩ�˲�ͬ�Ŀɿ��Ժ�˳���ϣ������㲻ͬ�����紫������
/// </summary>
public enum DeliveryMode
{
    /// <summary>
    /// Raw ģʽ��ʾԭʼ���䣬���ṩ�κοɿ��Ա��ϣ����ݰ����ܶ�ʧ������
    /// </summary>
    Raw,
    /// <summary>
    /// Unreliable ģʽ��ʾ���ɿ����䣬���ݰ����ܶ�ʧ�������豣֤���ݰ�������˳�򵽴
    /// </summary>
	Unreliable,
    /// <summary>
    /// UnreliableSequenced ģʽ��ʾ���ɿ��������䣬���ݰ����ܶ�ʧ�������յ������ݰ��ᰴ��˳�����У���ʧ�����ݰ��������ԡ�
    /// </summary>
	UnreliableSequenced,
    /// <summary>
    /// Reliable ģʽ��ʾ�ɿ����䣬��֤���ݰ������ɹ����գ������谴˳�����С�
    /// </summary>
	Reliable,
    /// <summary>
    /// ReliableSequenced ģʽ��ʾ�ɿ��������䣬���ݰ�������֤�ɹ����գ�����֤������˳�����С�
    /// </summary>
	ReliableSequenced
}
