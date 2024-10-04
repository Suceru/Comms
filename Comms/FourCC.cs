using System.Text;

namespace Comms;
/// <summary>
/// FourCC (Four Character Code) ��һ����4���ֽڱ�ʾ���ַ����룬ͨ�������ļ���ʽ��ý�����͵ȱ�ʶ����
/// ������ṩ�˽��ַ���ת��Ϊ FourCC ������ʾ���Լ��� FourCC ����ת�����ַ����Ĺ��ܡ�
/// </summary>
public static class FourCC
{
    /// <summary>
    /// Parse ������һ�� 4 �ַ����ȵ��ַ���ת��Ϊ��Ӧ�� FourCC ������ʾ��
	/// FourCC ͨ�����ĸ��ַ����յش��Ϊһ�� 32 λ������������ÿ���ַ�ռ�� 8 λ��
    /// </summary>
    /// <param name="fourcc">��Ҫת���� 4 �ַ����ȵ��ַ�����</param>
    /// <returns>��Ӧ�� 32 λ����������ÿ���ַ���Ϊ������һ���֣�����λ�ֽڵ���λ�ֽڵ�˳�򣩡�</returns>
    public static int Parse(string fourcc)
	{
        // ���ַ����е�ÿ���ַ�תΪ��Ӧ���޷�������������λ�ý�����λ����ϳ�һ�� 32 λ������
        // fourcc[0] �����λ��fourcc[3] �����λ��
        return (int)(((uint)fourcc[3] << 24) | ((uint)fourcc[2] << 16) | ((uint)fourcc[1] << 8) | fourcc[0]);
	}
    /// <summary>
    /// Write ������һ������ FourCC ת�������Ӧ�� 4 �ַ�����ʾ��
    /// </summary>
    /// <param name="fourcc">һ�� 32 λ��������ʾ FourCC��</param>
    /// <returns>��Ӧ�� 4 �ַ����ȵ��ַ�����</returns>
    public static string Write(int fourcc)
	{
        // ʹ�� StringBuilder ����һ���µ��ַ��������ַ������ĸ��ַ���ɣ�ÿ���ַ���Ӧ�����е�һ���ֽڡ�
        StringBuilder stringBuilder = new StringBuilder(4);
        // �� 32 λ������ÿ���ֽ���ȡ��������Ϊ�ַ�׷�ӵ��ַ����С�
        // ����ʹ����λ�ƺ�ǿ��ת������ȡ��Ӧ���ֽڣ���ת�����ַ���
        stringBuilder.Append((char)(byte)fourcc);// ��� 8 λ
        stringBuilder.Append((char)(byte)(fourcc >> 8));// �ε� 8 λ
        stringBuilder.Append((char)(byte)(fourcc >> 16));// �θ� 8 λ
        stringBuilder.Append((char)(byte)(fourcc >> 24));// ��� 8 λ
        // �������ɵ� 4 �ַ�����ʾ��
        return stringBuilder.ToString();
	}
}
