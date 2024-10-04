using System;
using System.Runtime.CompilerServices;

namespace Comms;
/// <summary>
/// �ṩ���ڼ����ϣֵ�ľ�̬������
/// 
/// ����ʵ����һ��32λ��ϣ�㷨���������ֽ����顣ͨ��ʹ�ò�ͬ�����Ӻ��������ݣ������Բ�����ͬ�Ĺ�ϣֵ��
/// </summary>
public static class Hash
{
    // ����һ�����ڹ�ϣ�������������
    private const uint Prime32v1 = 2654435761u;

	private const uint Prime32v2 = 2246822519u;

	private const uint Prime32v3 = 3266489917u;

	private const uint Prime32v4 = 668265263u;

	private const uint Prime32v5 = 374761393u;
    /// <summary>
    /// ��������ֽ�����Ĺ�ϣֵ��ʹ��Ĭ�����ӡ�
    /// </summary>
    /// <param name="buffer">Ҫ�����ϣֵ���ֽ�����</param>
    /// <param name="seed">��ѡ������ֵ��Ĭ��Ϊ0</param>
    /// <returns>����õ���32λ��ϣֵ</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint Calculate(byte[] buffer, uint seed = 0u)
	{
		return Calculate(buffer, 0, buffer.Length, seed);
	}
    /// <summary>
    /// ��������ֽ�����Ĺ�ϣֵ������ָ��ƫ�����ͼ�����
    /// </summary>
    /// <param name="buffer">Ҫ�����ϣֵ���ֽ�����</param>
    /// <param name="offset">�ֽ������е���ʼƫ����</param>
    /// <param name="count">Ҫ������ֽ�����</param>
    /// <param name="seed">��ѡ������ֵ��Ĭ��Ϊ0</param>
    /// <returns>����õ���32λ��ϣֵ</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public unsafe static uint Calculate(byte[] buffer, int offset, int count, uint seed = 0u)
	{
        // ��֤��������Ч��
        if (offset < 0 || count < 0 || offset + count > buffer.Length)
		{
			throw new ArgumentOutOfRangeException();
		}
		int num = count;// ʣ����ֽ���
        uint num2;// ��ϣ���
        // �̶�ָ�룬���Ż�����
        fixed (byte* ptr = buffer)
		{
			byte* pInput = ptr + offset;// ����ָ��
            // ������ڵ���16�ֽڵ�����
            if (count >= 16)
			{
                // ��ʼ���ۼ���
                var (acc, acc2, acc3, acc4) = InitAccumulators32(seed);
				do
				{
                    // ����ÿ16�ֽڵ�����
                    num2 = ProcessStripe32(ref pInput, ref acc, ref acc2, ref acc3, ref acc4);
					num -= 16;// ����ʣ���ֽ���
                }
				while (num >= 16);// ��������ֱ�����ݲ���16�ֽ�
            }
			else
			{
                // ����ֽ�������16�ֽڣ���ʹ������ֵ
                num2 = seed + 374761393;
			}
            // �����ֽ���������ʣ����ֽ�
            num2 += (uint)count;
			num2 = ProcessRemaining32(pInput, num2, num);
		}
		return Avalanche32(num2);// �������յĹ�ϣת��
    }
    /// <summary>
    /// ��ʼ��32λ�ۼ�����
    /// </summary>
    /// <param name="seed">��ʼ����</param>
    /// <returns>��ʼ������ĸ��ۼ���ֵ</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static (uint, uint, uint, uint) InitAccumulators32(uint seed)
	{
        // �������ӳ�ʼ���ĸ��ۼ���
        return ((uint)((int)seed + -1640531535 + -2048144777), seed + 2246822519u, seed, seed - 2654435761u);
	}

    /// <summary>
    /// ����16�ֽ����ݵĵ����顣
    /// </summary>
    /// <param name="pInput">ָ�������ֽڵ�ָ��</param>
    /// <param name="acc1">��һ���ۼ���</param>
    /// <param name="acc2">�ڶ����ۼ���</param>
    /// <param name="acc3">�������ۼ���</param>
    /// <param name="acc4">���ĸ��ۼ���</param>
    /// <returns>�����Ĺ�ϣ���</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static uint ProcessStripe32(ref byte* pInput, ref uint acc1, ref uint acc2, ref uint acc3, ref uint acc4)
	{
		ProcessLane32(ref pInput, ref acc1);
		ProcessLane32(ref pInput, ref acc2);
		ProcessLane32(ref pInput, ref acc3);
		ProcessLane32(ref pInput, ref acc4);
        // ͨ��ѭ�����ƺ��ۼ������¹�ϣֵ
        return RotateLeft(acc1, 1) + RotateLeft(acc2, 7) + RotateLeft(acc3, 12) + RotateLeft(acc4, 18);
	}
    /// <summary>
    /// ���������ݿ顣
    /// </summary>
    /// <param name="pInput">ָ�������ֽڵ�ָ��</param>
    /// <param name="accn">��ǰ�ۼ�����ֵ</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void ProcessLane32(ref byte* pInput, ref uint accn)
	{
		uint lane = *(uint*)pInput;// ��ȡ4���ֽ���Ϊһ��uint
        accn = Round32(accn, lane);// �����ۼ���
        pInput += 4;// �ƶ�ָ�뵽��һ��4�ֽ�
    }

    /// <summary>
    /// ����ʣ����ֽڡ�
    /// </summary>
    /// <param name="pInput">ָ�������ֽڵ�ָ��</param>
    /// <param name="acc">��ǰ�ۼ�����ֵ</param>
    /// <param name="remainingLen">ʣ���ֽ���</param>
    /// <returns>�������ۼ���ֵ</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static uint ProcessRemaining32(byte* pInput, uint acc, int remainingLen)
	{
        // ����ʣ��4�ֽ����ϵ�����
        while (remainingLen >= 4)
		{
			uint num = *(uint*)pInput;// ��ȡ4���ֽ���Ϊһ��uint
            acc += (uint)((int)num * -1028477379);// �����ۼ���
            acc = RotateLeft(acc, 17) * 668265263;// ��ת�����Գ���
            remainingLen -= 4;// ����ʣ���ֽ���
            pInput += 4;// �ƶ�ָ��
        }
        // ����ʣ����ֽڣ�1��3���ֽڣ�
        while (remainingLen >= 1)
		{
			byte b = *pInput;// ��ȡʣ����ֽ�
            acc += (uint)(b * 374761393);// �����ۼ���
            acc = RotateLeft(acc, 11) * 2654435761u; // ��ת�����Գ���
            remainingLen--;// ����ʣ���ֽ���
            pInput++;// �ƶ�ָ��
        }
		return acc;// ���������ۼ���ֵ
    }
    /// <summary>
    /// �Ե�ǰ�ۼ���ֵ���е��δ���
    /// </summary>
    /// <param name="accn">��ǰ�ۼ�����ֵ</param>
    /// <param name="lane">��ǰ��������ݿ�</param>
    /// <returns>���º���ۼ���ֵ</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint Round32(uint accn, uint lane)
	{
		accn += (uint)((int)lane * -2048144777); // �����ۼ���
        accn = RotateLeft(accn, 13);// ��ת
        accn *= 2654435761u;// ���Գ���
        return accn;// �����ۼ���ֵ
    }
    /// <summary>
    /// �Թ�ϣ����������յı任���Լ�����ײ��
    /// </summary>
    /// <param name="acc">��ǰ�ۼ���ֵ</param>
    /// <returns>�����任�Ĺ�ϣֵ</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint Avalanche32(uint acc)
	{
		acc ^= acc >> 15;// XOR��λ
        acc *= 2246822519u;// ���Գ���
        acc ^= acc >> 13;// XOR��λ
        acc *= 3266489917u;// ���Գ���
        acc ^= acc >> 16;// XOR��λ
        return acc;// �������յĹ�ϣֵ
    }

    /// <summary>
    /// ������ֵ����ѭ�����Ʋ�����
    /// </summary>
    /// <param name="value">Ҫ��λ��ֵ</param>
    /// <param name="bits">��λ��λ��</param>
    /// <returns>ѭ�����ƺ��ֵ</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint RotateLeft(uint value, int bits)
	{
		return (value << bits) | (value >> 32 - bits);// ִ��ѭ������
    }
}
