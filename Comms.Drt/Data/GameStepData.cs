using System.Net;

namespace Comms.Drt;
/// <summary>
/// ��ʾ������Ϸ�е���Ϸ�������ݣ�����ͬ����Ҽ��롢�뿪�Լ��������Ϣ��
/// </summary>
public struct GameStepData
{
    /// <summary>
    /// ��ʾ��Ҽ�����Ϸ�����ݡ�
    /// </summary>
    public struct JoinData
	{
        /// <summary>
        /// �ͻ��˵�Ψһ��ʶ����
        /// </summary>
        public int ClientID;
        /// <summary>
        /// �ͻ��˵������ַ��
        /// </summary>
        public IPEndPoint Address;
        /// <summary>
        /// ���������ԭʼ�ֽ����ݡ�
        /// ͨ����������ҵ������֤�������������Ϣ��
        /// </summary>
        public byte[] JoinRequestBytes;
	}
    /// <summary>
    /// ��ʾ����뿪��Ϸ�����ݡ�
    /// </summary>
    public struct LeaveData
	{
        /// <summary>
        /// �뿪��Ϸ�Ŀͻ��˵�Ψһ��ʶ����
        /// </summary>
        public int ClientID;
	}
    /// <summary>
    /// ��ʾ����������ݵ���Ϣ��
    /// </summary>
    public struct InputData
	{
        /// <summary>
        /// �����������ݵĿͻ��˵�Ψһ��ʶ����
        /// </summary>
        public int ClientID;
        /// <summary>
        /// �����ԭʼ�ֽ����ݡ�
        /// ������ҵĲ������룬���ƶ��������ȶ����ı��롣
        /// </summary>
        public byte[] InputBytes;
	}
    /// <summary>
    /// ��ǰ��Ϸ����ı�š�
    /// ÿһ����Ӧ��Ϸ�е�һ��״̬�������ڡ�
    /// </summary>
    public int Step;
    /// <summary>
    /// ��������������ҵļ������ݼ��ϡ�
    /// </summary>
    public JoinData[] Joins;
    /// <summary>
    /// ��������������ҵ��뿪���ݼ��ϡ�
    /// </summary>
    public LeaveData[] Leaves;
    /// <summary>
    /// ��������������ҵ��������ݼ��ϡ�
    /// </summary>
    public InputData[] Inputs;
}
