namespace Comms.Drt;
/// <summary>
/// ��ʾ��Դ���ݵĽṹ�塣
/// �����ڿͻ��˺ͷ�����֮�䴫����Դ��Ϣ������Ϸ�е��ļ������û�������Ҫ���ݡ�
/// </summary>
public struct ResourceData
{
    /// <summary>
    /// ��Դ�����ơ�
    /// �����ļ������ơ���Դ��ʶ���ȣ����ڱ�ʶ����Դ��
    /// </summary>
    public string Name;
    /// <summary>
    /// ��Դ�İ汾�š�
    /// ���ڱ�ʶ��Դ�İ汾����ͬ�İ汾�ſ�������������Դ�ĸ���״̬��
    /// </summary>
    public int Version;
    /// <summary>
    /// ��Դ��ԭʼ�ֽ����ݡ�
    /// ��Դ���������ֽ��������ʽ�洢���������ļ���ͼƬ�����������ݵȡ�
    /// </summary>
    public byte[] Bytes;
}
