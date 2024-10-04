namespace Comms.Drt;
/// <summary>
/// GameDescription 类用于描述服务器上某个游戏的基本信息和当前状态。
///  它包含了游戏的 ID、参与的客户端数量、当前游戏步骤（Step）以及游戏描述的字节数据。
///  它支持将游戏描述从二进制数据读取（Read）和写入（Write）。
/// </summary>
public class GameDescription
{
    /// <summary>
    /// ServerDescription 对象，描述服务器的基本信息。
    /// </summary>
    public ServerDescription ServerDescription;
    /// <summary>
    /// 游戏的唯一标识符（ID），由服务器分配。
    /// </summary>
    public int GameID;
    /// <summary>
    /// 当前游戏中已连接的客户端数量。
    /// </summary>
    public int ClientsCount;
    /// <summary>
    /// 游戏当前所处的步骤，用于同步状态。
    /// </summary>
    public int Step;
    /// <summary>
    /// 存储游戏的描述信息（字节数组形式），具体内容根据游戏实现的不同而变化。
    /// </summary>
	public byte[] GameDescriptionBytes;

    // 从二进制流（Reader 对象）中读取游戏描述的信息并初始化当前对象的各属性。
    // 包括读取游戏 ID、客户端数量、游戏步骤及游戏描述的字节数据。
    internal void Read(Reader reader)
	{
        // 从流中读取游戏 ID（使用压缩的整型编码来减少数据量）。
        GameID = reader.ReadPackedInt32();
        // 从流中读取客户端数量（使用压缩的整型编码）。
        ClientsCount = reader.ReadPackedInt32();
        // 从流中读取游戏的当前步骤（使用压缩的整型编码）。
        Step = reader.ReadPackedInt32();
        // 从流中读取游戏描述的字节数据（读至结束）。
        GameDescriptionBytes = reader.ReadBytes();
	}

    // 将当前对象的游戏描述信息写入到二进制流（Writer 对象）中。
    // 依次写入游戏 ID、客户端数量、游戏步骤及游戏描述的字节数据。
    internal void Write(Writer writer)
	{
        // 将游戏 ID 以压缩的整型格式写入到流中，减少数据量。
        writer.WritePackedInt32(GameID);
        // 将客户端数量以压缩的整型格式写入到流中。
        writer.WritePackedInt32(ClientsCount);
        // 将游戏的当前步骤以压缩的整型格式写入到流中。
        writer.WritePackedInt32(Step);
        // 将游戏描述的字节数组写入到流中。
        writer.WriteBytes(GameDescriptionBytes);
	}
}
