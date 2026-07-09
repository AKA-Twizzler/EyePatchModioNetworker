using System.Collections.Generic;
using LabFusion.Entities;
using LabFusion.Player;

namespace ModioModNetworker.Utilities;

public static class NetworkPlayerUtilities
{
	public static List<NetworkPlayer> GetAllNetworkPlayers()
	{
		List<NetworkPlayer> list = new List<NetworkPlayer>();
		NetworkPlayer item = default(NetworkPlayer);
		foreach (PlayerID playerID in PlayerIDManager.PlayerIDs)
		{
			if (NetworkPlayerManager.TryGetPlayer((byte)playerID, out item))
			{
				list.Add(item);
			}
		}
		return list;
	}
}
