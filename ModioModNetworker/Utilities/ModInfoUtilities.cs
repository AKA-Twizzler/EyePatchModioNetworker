using System;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using LabFusion.Marrow;
using ModioModNetworker.Data;
using UnityEngine;

namespace ModioModNetworker.Utilities;

public class ModInfoUtilities
{
	public static ModInfo GetModInfoForPoolee(Poolee assetPoolee)
	{
		string id = ((Scannable)((Crate)assetPoolee.SpawnableCrate)._pallet)._barcode._id;
		return GetModInfoForPalletBarcode(id);
	}

	public static ModInfo GetModInfoForLevelBarcode(string barcode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		LevelCrate crate = CrateFilterer.GetCrate<LevelCrate>(new Barcode(barcode));
		if ((UnityEngine.Object)(object)crate == (UnityEngine.Object)null)
		{
			return null;
		}
		string id = ((Scannable)((Crate)crate)._pallet)._barcode._id;
		return GetModInfoForPalletBarcode(id);
	}

	public static ModInfo GetModInfoForSpawnableBarcode(string barcode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		GameObjectCrate crate = CrateFilterer.GetCrate<GameObjectCrate>(new Barcode(barcode));
		if ((UnityEngine.Object)(object)crate == (UnityEngine.Object)null)
		{
			return null;
		}
		string id = ((Scannable)((Crate)crate)._pallet)._barcode._id;
		return GetModInfoForPalletBarcode(id);
	}

	public static ModInfo GetModInfoForPalletBarcode(string barcode)
	{
		ModInfo modInfo = null;
		foreach (InstalledModInfo installedModInfo in MainClass.InstalledModInfos)
		{
			try
			{
				if (installedModInfo.palletBarcode == barcode)
				{
					modInfo = installedModInfo.ModInfo;
					break;
				}
			}
			catch (Exception)
			{
			}
		}
		if (modInfo != null)
		{
			return modInfo;
		}
		return null;
	}
}
