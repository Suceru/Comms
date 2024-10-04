using System.Text;

namespace Comms;
/// <summary>
/// FourCC (Four Character Code) 是一种用4个字节表示的字符编码，通常用于文件格式、媒体类型等标识符。
/// 这个类提供了将字符串转换为 FourCC 整数表示，以及将 FourCC 整数转换回字符串的功能。
/// </summary>
public static class FourCC
{
    /// <summary>
    /// Parse 方法将一个 4 字符长度的字符串转换为对应的 FourCC 整数表示。
	/// FourCC 通常将四个字符紧凑地打包为一个 32 位的整数，其中每个字符占用 8 位。
    /// </summary>
    /// <param name="fourcc">需要转换的 4 字符长度的字符串。</param>
    /// <returns>对应的 32 位整数，其中每个字符作为整数的一部分（按低位字节到高位字节的顺序）。</returns>
    public static int Parse(string fourcc)
	{
        // 将字符串中的每个字符转为对应的无符号整数，并按位置将其移位后组合成一个 32 位整数。
        // fourcc[0] 是最低位，fourcc[3] 是最高位。
        return (int)(((uint)fourcc[3] << 24) | ((uint)fourcc[2] << 16) | ((uint)fourcc[1] << 8) | fourcc[0]);
	}
    /// <summary>
    /// Write 方法将一个整数 FourCC 转换回其对应的 4 字符串表示。
    /// </summary>
    /// <param name="fourcc">一个 32 位整数，表示 FourCC。</param>
    /// <returns>对应的 4 字符长度的字符串。</returns>
    public static string Write(int fourcc)
	{
        // 使用 StringBuilder 构建一个新的字符串，该字符串由四个字符组成，每个字符对应整数中的一个字节。
        StringBuilder stringBuilder = new StringBuilder(4);
        // 将 32 位整数的每个字节提取出来并作为字符追加到字符串中。
        // 这里使用了位移和强制转换来获取对应的字节，并转换成字符。
        stringBuilder.Append((char)(byte)fourcc);// 最低 8 位
        stringBuilder.Append((char)(byte)(fourcc >> 8));// 次低 8 位
        stringBuilder.Append((char)(byte)(fourcc >> 16));// 次高 8 位
        stringBuilder.Append((char)(byte)(fourcc >> 24));// 最高 8 位
        // 返回生成的 4 字符串表示。
        return stringBuilder.ToString();
	}
}
