using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Comms.Drt;
/// <summary>
///MessageSerializer 类用于序列化和反序列化消息（Message）。
///它通过消息的类型 ID 和类型对象之间的映射，实现消息的读取与写入。
/// </summary>
internal class MessageSerializer
{
	// 保存消息 ID 与消息类型 (Type) 的映射，消息 ID 是整数。
    private static Dictionary<int, Type> MessageTypesById;
    // 保存消息类型 (Type) 与消息 ID 的映射，用于根据消息类型查找对应的 ID。
    private static Dictionary<Type, int> MessageIdsByType;
    // 当前游戏类型的唯一标识符，用于区分不同的游戏类型。
    public int GameTypeID { get; }
    // 静态构造函数，用于初始化消息类型与消息 ID 的双向映射。
    // 该映射将程序集中定义的所有非抽象 Message 类按名称排序并依次分配 ID。
    static MessageSerializer()
	{
		MessageTypesById = new Dictionary<int, Type>();
		MessageIdsByType = new Dictionary<Type, int>();
        // 从当前程序集中查找所有派生自 Message 类并且不是抽象类的类型，按名称排序。
        TypeInfo[] array = (from t in typeof(Message).Assembly.DefinedTypes
			where typeof(Message).IsAssignableFrom(t) && !t.IsAbstract
			orderby t.Name
			select t).ToArray();
        // 为每个 Message 类型分配一个唯一的整数 ID，并建立双向映射。
        for (int i = 0; i < array.Length; i++)
		{
			MessageTypesById[i] = array[i];
			MessageIdsByType[array[i]] = i;
		}
	}
    // 构造函数，接收当前游戏类型的 ID。
    // GameTypeID 用于标识与该序列化器相关的游戏类型。
    public MessageSerializer(int gameTypeID)
	{
		GameTypeID = gameTypeID;
	}
    // Read 方法用于反序列化消息。它接收字节数组和发送者的网络地址作为输入，并返回反序列化后的 Message 对象。
    public Message Read(byte[] bytes, IPEndPoint senderAddress)
	{
		try
		{
            // 使用 Reader 来读取字节数据。
            Reader reader = new Reader(bytes);
            // 读取消息中的游戏类型 ID，并与当前 GameTypeID 进行匹配检查。
            int num = reader.ReadInt32();
			if (num != GameTypeID)
			{
				throw new ProtocolViolationException($"Message has invalid game type ID 0x{num:X}, expected 0x{GameTypeID:X}.");
            }
			// 读取消息的类型 ID，根据类型 ID 查找对应的消息类型，创建消息实例。

            Message obj = (Message)Activator.CreateInstance(MessageTypesById[reader.ReadPackedInt32()]);
            // 读取消息内容并将数据填充到消息实例中。
            obj.Read(reader);
			return obj;
		}
		catch (Exception ex)
		{
            // 如果反序列化过程中出现错误，抛出 MalformedMessageException，并附带发送者地址。
            throw new MalformedMessageException(ex.Message, senderAddress);
		}
	}
    // Write 方法用于将消息对象序列化为字节数组。
    // 它将消息的类型 ID 和内容写入字节数组，并返回该数组。
    public byte[] Write(Message message)
    {// 创建一个 Writer，用于将消息内容写入字节数组。
        Writer writer = new Writer();
        // 写入游戏类型 ID，确保消息属于正确的游戏类型。
        writer.WriteInt32(GameTypeID);
        // 写入消息类型 ID（通过消息的类型来获取 ID）。
        writer.WritePackedInt32(MessageIdsByType[message.GetType()]);
        // 调用消息的 Write 方法，将消息内容写入 Writer。
        message.Write(writer);
        // 返回包含序列化数据的字节数组。
        return writer.GetBytes();
	}
}
