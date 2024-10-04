namespace Comms.Drt;
/// <summary>
/// GameDescription ������������������ĳ����Ϸ�Ļ�����Ϣ�͵�ǰ״̬��
///  ����������Ϸ�� ID������Ŀͻ�����������ǰ��Ϸ���裨Step���Լ���Ϸ�������ֽ����ݡ�
///  ��֧�ֽ���Ϸ�����Ӷ��������ݶ�ȡ��Read����д�루Write����
/// </summary>
public class GameDescription
{
    /// <summary>
    /// ServerDescription ���������������Ļ�����Ϣ��
    /// </summary>
    public ServerDescription ServerDescription;
    /// <summary>
    /// ��Ϸ��Ψһ��ʶ����ID�����ɷ��������䡣
    /// </summary>
    public int GameID;
    /// <summary>
    /// ��ǰ��Ϸ�������ӵĿͻ���������
    /// </summary>
    public int ClientsCount;
    /// <summary>
    /// ��Ϸ��ǰ�����Ĳ��裬����ͬ��״̬��
    /// </summary>
    public int Step;
    /// <summary>
    /// �洢��Ϸ��������Ϣ���ֽ�������ʽ�����������ݸ�����Ϸʵ�ֵĲ�ͬ���仯��
    /// </summary>
	public byte[] GameDescriptionBytes;

    // �Ӷ���������Reader �����ж�ȡ��Ϸ��������Ϣ����ʼ����ǰ����ĸ����ԡ�
    // ������ȡ��Ϸ ID���ͻ�����������Ϸ���輰��Ϸ�������ֽ����ݡ�
    internal void Read(Reader reader)
	{
        // �����ж�ȡ��Ϸ ID��ʹ��ѹ�������ͱ�������������������
        GameID = reader.ReadPackedInt32();
        // �����ж�ȡ�ͻ���������ʹ��ѹ�������ͱ��룩��
        ClientsCount = reader.ReadPackedInt32();
        // �����ж�ȡ��Ϸ�ĵ�ǰ���裨ʹ��ѹ�������ͱ��룩��
        Step = reader.ReadPackedInt32();
        // �����ж�ȡ��Ϸ�������ֽ����ݣ�������������
        GameDescriptionBytes = reader.ReadBytes();
	}

    // ����ǰ�������Ϸ������Ϣд�뵽����������Writer �����С�
    // ����д����Ϸ ID���ͻ�����������Ϸ���輰��Ϸ�������ֽ����ݡ�
    internal void Write(Writer writer)
	{
        // ����Ϸ ID ��ѹ�������͸�ʽд�뵽���У�������������
        writer.WritePackedInt32(GameID);
        // ���ͻ���������ѹ�������͸�ʽд�뵽���С�
        writer.WritePackedInt32(ClientsCount);
        // ����Ϸ�ĵ�ǰ������ѹ�������͸�ʽд�뵽���С�
        writer.WritePackedInt32(Step);
        // ����Ϸ�������ֽ�����д�뵽���С�
        writer.WriteBytes(GameDescriptionBytes);
	}
}
