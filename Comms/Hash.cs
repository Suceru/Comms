using System;
using System.Runtime.CompilerServices;

namespace Comms;
/// <summary>
/// 提供用于计算哈希值的静态方法。
/// 
/// 该类实现了一种32位哈希算法，适用于字节数组。通过使用不同的种子和输入数据，它可以产生不同的哈希值。
/// </summary>
public static class Hash
{
    // 定义一组用于哈希计算的质数常量
    private const uint Prime32v1 = 2654435761u;

	private const uint Prime32v2 = 2246822519u;

	private const uint Prime32v3 = 3266489917u;

	private const uint Prime32v4 = 668265263u;

	private const uint Prime32v5 = 374761393u;
    /// <summary>
    /// 计算给定字节数组的哈希值，使用默认种子。
    /// </summary>
    /// <param name="buffer">要计算哈希值的字节数组</param>
    /// <param name="seed">可选的种子值，默认为0</param>
    /// <returns>计算得到的32位哈希值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint Calculate(byte[] buffer, uint seed = 0u)
	{
		return Calculate(buffer, 0, buffer.Length, seed);
	}
    /// <summary>
    /// 计算给定字节数组的哈希值，允许指定偏移量和计数。
    /// </summary>
    /// <param name="buffer">要计算哈希值的字节数组</param>
    /// <param name="offset">字节数组中的起始偏移量</param>
    /// <param name="count">要计算的字节数量</param>
    /// <param name="seed">可选的种子值，默认为0</param>
    /// <returns>计算得到的32位哈希值</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public unsafe static uint Calculate(byte[] buffer, int offset, int count, uint seed = 0u)
	{
        // 验证参数的有效性
        if (offset < 0 || count < 0 || offset + count > buffer.Length)
		{
			throw new ArgumentOutOfRangeException();
		}
		int num = count;// 剩余的字节数
        uint num2;// 哈希结果
        // 固定指针，以优化性能
        fixed (byte* ptr = buffer)
		{
			byte* pInput = ptr + offset;// 输入指针
            // 处理大于等于16字节的输入
            if (count >= 16)
			{
                // 初始化累加器
                var (acc, acc2, acc3, acc4) = InitAccumulators32(seed);
				do
				{
                    // 处理每16字节的数据
                    num2 = ProcessStripe32(ref pInput, ref acc, ref acc2, ref acc3, ref acc4);
					num -= 16;// 减少剩余字节数
                }
				while (num >= 16);// 继续处理直到数据不足16字节
            }
			else
			{
                // 如果字节数不足16字节，则使用种子值
                num2 = seed + 374761393;
			}
            // 加上字节数并处理剩余的字节
            num2 += (uint)count;
			num2 = ProcessRemaining32(pInput, num2, num);
		}
		return Avalanche32(num2);// 进行最终的哈希转化
    }
    /// <summary>
    /// 初始化32位累加器。
    /// </summary>
    /// <param name="seed">初始种子</param>
    /// <returns>初始化后的四个累加器值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static (uint, uint, uint, uint) InitAccumulators32(uint seed)
	{
        // 根据种子初始化四个累加器
        return ((uint)((int)seed + -1640531535 + -2048144777), seed + 2246822519u, seed, seed - 2654435761u);
	}

    /// <summary>
    /// 处理16字节数据的单个块。
    /// </summary>
    /// <param name="pInput">指向输入字节的指针</param>
    /// <param name="acc1">第一个累加器</param>
    /// <param name="acc2">第二个累加器</param>
    /// <param name="acc3">第三个累加器</param>
    /// <param name="acc4">第四个累加器</param>
    /// <returns>处理后的哈希结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static uint ProcessStripe32(ref byte* pInput, ref uint acc1, ref uint acc2, ref uint acc3, ref uint acc4)
	{
		ProcessLane32(ref pInput, ref acc1);
		ProcessLane32(ref pInput, ref acc2);
		ProcessLane32(ref pInput, ref acc3);
		ProcessLane32(ref pInput, ref acc4);
        // 通过循环左移和累加来更新哈希值
        return RotateLeft(acc1, 1) + RotateLeft(acc2, 7) + RotateLeft(acc3, 12) + RotateLeft(acc4, 18);
	}
    /// <summary>
    /// 处理单个数据块。
    /// </summary>
    /// <param name="pInput">指向输入字节的指针</param>
    /// <param name="accn">当前累加器的值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void ProcessLane32(ref byte* pInput, ref uint accn)
	{
		uint lane = *(uint*)pInput;// 读取4个字节作为一个uint
        accn = Round32(accn, lane);// 更新累加器
        pInput += 4;// 移动指针到下一个4字节
    }

    /// <summary>
    /// 处理剩余的字节。
    /// </summary>
    /// <param name="pInput">指向输入字节的指针</param>
    /// <param name="acc">当前累加器的值</param>
    /// <param name="remainingLen">剩余字节数</param>
    /// <returns>处理后的累加器值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static uint ProcessRemaining32(byte* pInput, uint acc, int remainingLen)
	{
        // 处理剩余4字节以上的数据
        while (remainingLen >= 4)
		{
			uint num = *(uint*)pInput;// 读取4个字节作为一个uint
            acc += (uint)((int)num * -1028477379);// 更新累加器
            acc = RotateLeft(acc, 17) * 668265263;// 旋转并乘以常数
            remainingLen -= 4;// 减少剩余字节数
            pInput += 4;// 移动指针
        }
        // 处理剩余的字节（1到3个字节）
        while (remainingLen >= 1)
		{
			byte b = *pInput;// 读取剩余的字节
            acc += (uint)(b * 374761393);// 更新累加器
            acc = RotateLeft(acc, 11) * 2654435761u; // 旋转并乘以常数
            remainingLen--;// 减少剩余字节数
            pInput++;// 移动指针
        }
		return acc;// 返回最终累加器值
    }
    /// <summary>
    /// 对当前累加器值进行单次处理。
    /// </summary>
    /// <param name="accn">当前累加器的值</param>
    /// <param name="lane">当前处理的数据块</param>
    /// <returns>更新后的累加器值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint Round32(uint accn, uint lane)
	{
		accn += (uint)((int)lane * -2048144777); // 更新累加器
        accn = RotateLeft(accn, 13);// 旋转
        accn *= 2654435761u;// 乘以常数
        return accn;// 返回累加器值
    }
    /// <summary>
    /// 对哈希结果进行最终的变换，以减少碰撞。
    /// </summary>
    /// <param name="acc">当前累加器值</param>
    /// <returns>经过变换的哈希值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint Avalanche32(uint acc)
	{
		acc ^= acc >> 15;// XOR移位
        acc *= 2246822519u;// 乘以常数
        acc ^= acc >> 13;// XOR移位
        acc *= 3266489917u;// 乘以常数
        acc ^= acc >> 16;// XOR移位
        return acc;// 返回最终的哈希值
    }

    /// <summary>
    /// 将给定值进行循环左移操作。
    /// </summary>
    /// <param name="value">要移位的值</param>
    /// <param name="bits">移位的位数</param>
    /// <returns>循环左移后的值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint RotateLeft(uint value, int bits)
	{
		return (value << bits) | (value >> 32 - bits);// 执行循环左移
    }
}
