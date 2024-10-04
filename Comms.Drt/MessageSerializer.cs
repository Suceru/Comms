using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Comms.Drt;
/// <summary>
///MessageSerializer ���������л��ͷ����л���Ϣ��Message����
///��ͨ����Ϣ������ ID �����Ͷ���֮���ӳ�䣬ʵ����Ϣ�Ķ�ȡ��д�롣
/// </summary>
internal class MessageSerializer
{
	// ������Ϣ ID ����Ϣ���� (Type) ��ӳ�䣬��Ϣ ID ��������
    private static Dictionary<int, Type> MessageTypesById;
    // ������Ϣ���� (Type) ����Ϣ ID ��ӳ�䣬���ڸ�����Ϣ���Ͳ��Ҷ�Ӧ�� ID��
    private static Dictionary<Type, int> MessageIdsByType;
    // ��ǰ��Ϸ���͵�Ψһ��ʶ�����������ֲ�ͬ����Ϸ���͡�
    public int GameTypeID { get; }
    // ��̬���캯�������ڳ�ʼ����Ϣ��������Ϣ ID ��˫��ӳ�䡣
    // ��ӳ�佫�����ж�������зǳ��� Message �ఴ�����������η��� ID��
    static MessageSerializer()
	{
		MessageTypesById = new Dictionary<int, Type>();
		MessageIdsByType = new Dictionary<Type, int>();
        // �ӵ�ǰ�����в������������� Message �ಢ�Ҳ��ǳ���������ͣ�����������
        TypeInfo[] array = (from t in typeof(Message).Assembly.DefinedTypes
			where typeof(Message).IsAssignableFrom(t) && !t.IsAbstract
			orderby t.Name
			select t).ToArray();
        // Ϊÿ�� Message ���ͷ���һ��Ψһ������ ID��������˫��ӳ�䡣
        for (int i = 0; i < array.Length; i++)
		{
			MessageTypesById[i] = array[i];
			MessageIdsByType[array[i]] = i;
		}
	}
    // ���캯�������յ�ǰ��Ϸ���͵� ID��
    // GameTypeID ���ڱ�ʶ������л�����ص���Ϸ���͡�
    public MessageSerializer(int gameTypeID)
	{
		GameTypeID = gameTypeID;
	}
    // Read �������ڷ����л���Ϣ���������ֽ�����ͷ����ߵ������ַ��Ϊ���룬�����ط����л���� Message ����
    public Message Read(byte[] bytes, IPEndPoint senderAddress)
	{
		try
		{
            // ʹ�� Reader ����ȡ�ֽ����ݡ�
            Reader reader = new Reader(bytes);
            // ��ȡ��Ϣ�е���Ϸ���� ID�����뵱ǰ GameTypeID ����ƥ���顣
            int num = reader.ReadInt32();
			if (num != GameTypeID)
			{
				throw new ProtocolViolationException($"Message has invalid game type ID 0x{num:X}, expected 0x{GameTypeID:X}.");
            }
			// ��ȡ��Ϣ������ ID���������� ID ���Ҷ�Ӧ����Ϣ���ͣ�������Ϣʵ����

            Message obj = (Message)Activator.CreateInstance(MessageTypesById[reader.ReadPackedInt32()]);
            // ��ȡ��Ϣ���ݲ���������䵽��Ϣʵ���С�
            obj.Read(reader);
			return obj;
		}
		catch (Exception ex)
		{
            // ��������л������г��ִ����׳� MalformedMessageException�������������ߵ�ַ��
            throw new MalformedMessageException(ex.Message, senderAddress);
		}
	}
    // Write �������ڽ���Ϣ�������л�Ϊ�ֽ����顣
    // ������Ϣ������ ID ������д���ֽ����飬�����ظ����顣
    public byte[] Write(Message message)
    {// ����һ�� Writer�����ڽ���Ϣ����д���ֽ����顣
        Writer writer = new Writer();
        // д����Ϸ���� ID��ȷ����Ϣ������ȷ����Ϸ���͡�
        writer.WriteInt32(GameTypeID);
        // д����Ϣ���� ID��ͨ����Ϣ����������ȡ ID����
        writer.WritePackedInt32(MessageIdsByType[message.GetType()]);
        // ������Ϣ�� Write ����������Ϣ����д�� Writer��
        message.Write(writer);
        // ���ذ������л����ݵ��ֽ����顣
        return writer.GetBytes();
	}
}
