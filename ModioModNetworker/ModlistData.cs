using System;
using LabFusion.Network.Serialization;
using LabFusion.Player;
using ModioModNetworker.Data;

namespace ModioModNetworker;

public class ModlistData : INetSerializable
{
	public enum ModType
	{
		LIST = 1,
		AVATAR,
		SPAWNABLE,
		LEVEL
	}

	public PlayerID playerId;

	public bool isFinal = false;

	public ModType modType = ModType.LIST;

	public SerializedModInfo serializedModInfo;

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	public static ModlistData Create(bool final, ModInfo info)
	{
		return new ModlistData
		{
			isFinal = final,
			playerId = PlayerIDManager.LocalID,
			modType = ModType.LIST,
			serializedModInfo = SerializedModInfo.Create(info)
		};
	}

	public static ModlistData Create(PlayerID playerId, ModInfo info, ModType infoType)
	{
		return new ModlistData
		{
			isFinal = false,
			modType = infoType,
			playerId = playerId,
			serializedModInfo = SerializedModInfo.Create(info)
		};
	}

	public void Serialize(INetSerializer serializer)
	{
		serializer.SerializeValue(ref isFinal);
		serializer.SerializeValue<ModType>(ref modType);
		NetSerializerExtensions.SerializeValue<PlayerID>(serializer, ref playerId);
		NetSerializerExtensions.SerializeValue<SerializedModInfo>(serializer, ref serializedModInfo);
	}
}
