using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Comms.Drt;
// DesyncDetector 类用于检测服务器游戏中的不同步（desync）问题。
// 它通过比较游戏状态的哈希值来识别服务器和客户端之间的状态不一致。
internal class DesyncDetector
{
    // 与当前服务器游戏相关联的对象。
    private ServerGame ServerGame;
    // 上一次计算哈希值时的游戏步骤编号。
    private int LastHashStep;
    // 存储步骤编号到哈希值的映射，用于检测不同步。
    private Dictionary<int, ushort> Hashes = new Dictionary<int, ushort>();
    // 检测到不同步时的时间戳（以秒为单位）。
    private double DesyncDetectedTime;
    // 存储不同步检测过程中相关的状态数据。
    private DesyncData DesyncData;
    // 如果检测到不同步，则返回发生不同步的游戏步骤。
    public int? DesyncDetectedStep => DesyncData?.Step;
    // 构造函数，初始化 DesyncDetector 并关联一个服务器游戏对象。
    public DesyncDetector(ServerGame serverGame)
	{
		ServerGame = serverGame;
	}
    // 该方法在每个游戏帧中运行，用于检查和处理不同步问题。
    public void Run()
	{
        // 如果已经检测到不同步，并且服务器处于 "Locate" 模式（即定位不同步原因），
        // 并且记录了检测时间，则继续处理不同步状态。
        if (DesyncData != null && ServerGame.DesyncDetectionMode == DesyncDetectionMode.Locate && DesyncDetectedTime != 0.0)
		{
            // 检查当前时间与检测时间的差值，判断是否超时。
            bool timeoutReached = Comm.GetTime() - DesyncDetectedTime > (double)ServerGame.Server.Settings.DesyncDetectionStatesTimeout;
            // 检查是否收集到了足够的客户端状态（当前和之前的状态）。
            bool sufficientStatesCollected = DesyncData.States.Count >= DesyncData.ClientsCount && DesyncData.PriorStates.Count >= DesyncData.ClientsCount;
            // 如果已经超时或者状态收集完成，则触发不同步事件。
            if (timeoutReached || sufficientStatesCollected)
			{
				DesyncDetectedTime = 0.0;
				ServerGame.Server.InvokeDesync(DesyncData);
			}
		}
	}
    // 处理从客户端接收到的哈希值，检测是否发生不同步。
    public void HandleHashes(int firstHashStep, ushort[] hashes, ServerClient serverClient)
	{
        // 如果不同步检测模式为 None，或已经检测到不同步，则不做处理。
        if (ServerGame.DesyncDetectionMode == DesyncDetectionMode.None || DesyncData != null)
		{
			return;
		}
        // 遍历接收到的哈希值，检查与服务器端的哈希值是否一致。
        for (int i = 0; i < hashes.Length; i++)
		{
            // 如果服务器中已经存在该步骤的哈希值，则进行比较。
            if (Hashes.TryGetValue(i + firstHashStep, out var value))
			{
                // 如果哈希值不同，表示发生了不同步。
                if (hashes[i] != value)
                {// 创建不同步数据并记录检测时间。
                    DesyncData = new DesyncData
					{
						GameID = ServerGame.GameID,
						Step = firstHashStep + i,
						ClientsCount = ServerGame.Clients.Count
					};
					DesyncDetectedTime = Comm.GetTime();
                    // 记录不同步警告并通知服务器。
                    ServerGame.Server.InvokeWarning($"Desync detected at step {DesyncData.Step} when comparing hashes received from client \"{serverClient.ClientName}\" at {serverClient.PeerData.Address}");
                    // 如果在 "Locate" 模式下，向所有客户端请求发生不同步时的游戏状态。
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
                // 如果服务器端还没有记录该步骤的哈希值，则将其添加到哈希字典中。
                Hashes.Add(i + firstHashStep, hashes[i]);
				LastHashStep = Math.Max(LastHashStep, i + firstHashStep);
			}
		}
        // 清理过期的哈希值，防止哈希字典无限增长。
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
    // 处理从客户端接收到的游戏状态数据，用于进一步的不同步定位。
    public void HandleDesyncState(int step, byte[] stateBytes, bool isDeflated, ServerClient serverClient)
	{
        // 如果服务器处于 "Locate" 模式，并且已经检测到不同步，
        // 则处理客户端发送的状态数据。
        if (ServerGame.DesyncDetectionMode == DesyncDetectionMode.Locate && DesyncData != null)
		{
            // 如果状态数据是前一个步骤的，存储到 PriorStates。
            if (step == DesyncData.Step - 1)
			{
				DesyncData.PriorStates[serverClient.ClientID] = ProcessState(stateBytes, isDeflated);
			}
            // 如果状态数据是当前步骤的，存储到 States。
            else if (step == DesyncData.Step)
			{
				DesyncData.States[serverClient.ClientID] = ProcessState(stateBytes, isDeflated);
			}
		}
	}
    // 处理接收到的状态数据。如果数据是压缩的，则解压缩。
    private byte[] ProcessState(byte[] bytes, bool isDeflated)
	{
        // 如果数据是压缩的，使用 DeflateStream 解压缩。
        if (isDeflated)
		{
			using (DeflateStream deflateStream = new DeflateStream(new MemoryStream(bytes), CompressionMode.Decompress))
			{
				using MemoryStream memoryStream = new MemoryStream();
				deflateStream.CopyTo(memoryStream);
				return memoryStream.ToArray();
			}
		}
		return bytes;// 如果数据没有压缩，直接返回原始字节数组。
    }
}
