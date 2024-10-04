using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Comms.Drt;
// DesyncDetector �����ڼ���������Ϸ�еĲ�ͬ����desync�����⡣
// ��ͨ���Ƚ���Ϸ״̬�Ĺ�ϣֵ��ʶ��������Ϳͻ���֮���״̬��һ�¡�
internal class DesyncDetector
{
    // �뵱ǰ��������Ϸ������Ķ���
    private ServerGame ServerGame;
    // ��һ�μ����ϣֵʱ����Ϸ�����š�
    private int LastHashStep;
    // �洢�����ŵ���ϣֵ��ӳ�䣬���ڼ�ⲻͬ����
    private Dictionary<int, ushort> Hashes = new Dictionary<int, ushort>();
    // ��⵽��ͬ��ʱ��ʱ���������Ϊ��λ����
    private double DesyncDetectedTime;
    // �洢��ͬ������������ص�״̬���ݡ�
    private DesyncData DesyncData;
    // �����⵽��ͬ�����򷵻ط�����ͬ������Ϸ���衣
    public int? DesyncDetectedStep => DesyncData?.Step;
    // ���캯������ʼ�� DesyncDetector ������һ����������Ϸ����
    public DesyncDetector(ServerGame serverGame)
	{
		ServerGame = serverGame;
	}
    // �÷�����ÿ����Ϸ֡�����У����ڼ��ʹ���ͬ�����⡣
    public void Run()
	{
        // ����Ѿ���⵽��ͬ�������ҷ��������� "Locate" ģʽ������λ��ͬ��ԭ�򣩣�
        // ���Ҽ�¼�˼��ʱ�䣬���������ͬ��״̬��
        if (DesyncData != null && ServerGame.DesyncDetectionMode == DesyncDetectionMode.Locate && DesyncDetectedTime != 0.0)
		{
            // ��鵱ǰʱ������ʱ��Ĳ�ֵ���ж��Ƿ�ʱ��
            bool timeoutReached = Comm.GetTime() - DesyncDetectedTime > (double)ServerGame.Server.Settings.DesyncDetectionStatesTimeout;
            // ����Ƿ��ռ������㹻�Ŀͻ���״̬����ǰ��֮ǰ��״̬����
            bool sufficientStatesCollected = DesyncData.States.Count >= DesyncData.ClientsCount && DesyncData.PriorStates.Count >= DesyncData.ClientsCount;
            // ����Ѿ���ʱ����״̬�ռ���ɣ��򴥷���ͬ���¼���
            if (timeoutReached || sufficientStatesCollected)
			{
				DesyncDetectedTime = 0.0;
				ServerGame.Server.InvokeDesync(DesyncData);
			}
		}
	}
    // ����ӿͻ��˽��յ��Ĺ�ϣֵ������Ƿ�����ͬ����
    public void HandleHashes(int firstHashStep, ushort[] hashes, ServerClient serverClient)
	{
        // �����ͬ�����ģʽΪ None�����Ѿ���⵽��ͬ������������
        if (ServerGame.DesyncDetectionMode == DesyncDetectionMode.None || DesyncData != null)
		{
			return;
		}
        // �������յ��Ĺ�ϣֵ�������������˵Ĺ�ϣֵ�Ƿ�һ�¡�
        for (int i = 0; i < hashes.Length; i++)
		{
            // ������������Ѿ����ڸò���Ĺ�ϣֵ������бȽϡ�
            if (Hashes.TryGetValue(i + firstHashStep, out var value))
			{
                // �����ϣֵ��ͬ����ʾ�����˲�ͬ����
                if (hashes[i] != value)
                {// ������ͬ�����ݲ���¼���ʱ�䡣
                    DesyncData = new DesyncData
					{
						GameID = ServerGame.GameID,
						Step = firstHashStep + i,
						ClientsCount = ServerGame.Clients.Count
					};
					DesyncDetectedTime = Comm.GetTime();
                    // ��¼��ͬ�����沢֪ͨ��������
                    ServerGame.Server.InvokeWarning($"Desync detected at step {DesyncData.Step} when comparing hashes received from client \"{serverClient.ClientName}\" at {serverClient.PeerData.Address}");
                    // ����� "Locate" ģʽ�£������пͻ�����������ͬ��ʱ����Ϸ״̬��
                    if (ServerGame.DesyncDetectionMode == DesyncDetectionMode.Locate)
					{
						ServerGame.SendDataMessageToAllClients(new ServerDesyncStateRequestMessage
						{
							Step = DesyncData.Step - 1
						});
						ServerGame.SendDataMessageToAllClients(new ServerDesyncStateRequestMessage
						{
							Step = DesyncData.Step
						});
					}
					return;
				}
			}
			else
			{
                // ����������˻�û�м�¼�ò���Ĺ�ϣֵ��������ӵ���ϣ�ֵ��С�
                Hashes.Add(i + firstHashStep, hashes[i]);
				LastHashStep = Math.Max(LastHashStep, i + firstHashStep);
			}
		}
        // ������ڵĹ�ϣֵ����ֹ��ϣ�ֵ�����������
        int expiryThreshold = ServerGame.DesyncDetectionPeriod + 20;
		List<int> list = new List<int>();
		foreach (int key in Hashes.Keys)
		{
			if (LastHashStep - key > expiryThreshold)
            {
				list.Add(key);
			}
		}
		foreach (int item in list)
		{
			Hashes.Remove(item);
		}
	}
    // ����ӿͻ��˽��յ�����Ϸ״̬���ݣ����ڽ�һ���Ĳ�ͬ����λ��
    public void HandleDesyncState(int step, byte[] stateBytes, bool isDeflated, ServerClient serverClient)
	{
        // ������������� "Locate" ģʽ�������Ѿ���⵽��ͬ����
        // ����ͻ��˷��͵�״̬���ݡ�
        if (ServerGame.DesyncDetectionMode == DesyncDetectionMode.Locate && DesyncData != null)
		{
            // ���״̬������ǰһ������ģ��洢�� PriorStates��
            if (step == DesyncData.Step - 1)
			{
				DesyncData.PriorStates[serverClient.ClientID] = ProcessState(stateBytes, isDeflated);
			}
            // ���״̬�����ǵ�ǰ����ģ��洢�� States��
            else if (step == DesyncData.Step)
			{
				DesyncData.States[serverClient.ClientID] = ProcessState(stateBytes, isDeflated);
			}
		}
	}
    // ������յ���״̬���ݡ����������ѹ���ģ����ѹ����
    private byte[] ProcessState(byte[] bytes, bool isDeflated)
	{
        // ���������ѹ���ģ�ʹ�� DeflateStream ��ѹ����
        if (isDeflated)
		{
			using (DeflateStream deflateStream = new DeflateStream(new MemoryStream(bytes), CompressionMode.Decompress))
			{
				using MemoryStream memoryStream = new MemoryStream();
				deflateStream.CopyTo(memoryStream);
				return memoryStream.ToArray();
			}
		}
		return bytes;// �������û��ѹ����ֱ�ӷ���ԭʼ�ֽ����顣
    }
}
