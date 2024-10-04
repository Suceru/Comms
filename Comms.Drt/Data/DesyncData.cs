using System.Collections.Generic;

namespace Comms.Drt;
/// <summary>
/// ��ʾ����Ϸ�з�����ͬ����Desync��ʱ��������ݡ�
/// ����ͨ�����ڼ�����ͻ���֮����Ϸ״̬�Ĳ�ͬ�����⡣
/// </summary>
public class DesyncData
{
    /// <summary>
    /// ��Ϸ��Ψһ��ʶ�� (GameID)��
    /// �������ֲ�ͬ����Ϸʵ����
    /// </summary>
    public int GameID;
    /// <summary>
    /// ��ǰ��ͬ������ʱ����Ϸ���� (Step)��
    /// ���ڱ�ʾ��Ϸ�Ľ��ȣ�ͨ������Ϸѭ���е�ĳ��ʱ��㡣
    /// </summary>
    public int Step;
    /// <summary>
    /// ������Ϸ�Ŀͻ������� (ClientsCount)��
    /// ���ڱ�ʾ��ǰ������Ϸ�Ŀͻ���������
    /// </summary>
    public int ClientsCount;
    /// <summary>
    /// ������ǰ��Ϸ״̬���ֵ� (PriorStates)��
    /// ���ǿͻ���ID��ֵ�Ǹÿͻ����ڵ�ǰ��֮ǰ����Ϸ״̬��
    /// ���ֵ������ڼ�⵽��ͬ������ʱ����״̬���ݺͱȽϡ�
    /// </summary>
    public Dictionary<int, byte[]> PriorStates = new Dictionary<int, byte[]>();
    /// <summary>
    /// ���浱ǰ��Ϸ״̬���ֵ� (States)��
    /// ���ǿͻ���ID��ֵ�Ǹÿͻ����ڵ�ǰ������Ϸ״̬��
    /// ���ֵ����ڴ洢ÿ���ͻ����ڵ�ǰ������Ϸ״̬���Խ���ͬ����֤��
    /// </summary>
    public Dictionary<int, byte[]> States = new Dictionary<int, byte[]>();
}
