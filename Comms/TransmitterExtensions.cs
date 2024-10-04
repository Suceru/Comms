namespace Comms;
/// <summary>
/// TransmitterExtensions ��Ϊ ITransmitter �ӿ��ṩ��չ�����������������������
/// </summary>
public static class TransmitterExtensions
{
    /// <summary>
    /// RootTransmitter ��չ�������ڻ�ȡ����������ײ㣨�����Ĵ���������
	/// �������������ʵ���� IWrapperTransmitter �ӿڵĴ��������ýӿ�ͨ�����ڰ�װ��һ����������
    /// </summary>
    /// <param name="transmitter">������</param>
    /// <returns></returns>
    public static ITransmitter RootTransmitter(this ITransmitter transmitter)
	{
        // ͨ��ѭ�����ϻ�ȡ��װ���������Ļ�����������ֱ���ҵ���ײ�Ĵ�����Ϊֹ��
        while (transmitter is IWrapperTransmitter wrapperTransmitter)
        {
            // ����ǰ����������Ϊ��װ���Ļ���������
            transmitter = wrapperTransmitter.BaseTransmitter;
		}
        // ������ײ�Ĵ�����
        return transmitter;
	}
}
