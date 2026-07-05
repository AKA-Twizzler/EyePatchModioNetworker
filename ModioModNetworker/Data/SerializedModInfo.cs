using System.Collections.Generic;
using LabFusion.Network.Serialization;

namespace ModioModNetworker.Data;

public class SerializedModInfo : INetSerializable
{
	public bool valid = false;

	public bool mature = false;

	public ModInfo modInfo;

	private float fileSize;

	private string windowsDownloadLink;

	private string androidDownloadLink;

	private string thumbnailUrl;

	private string summary;

	private string modName;

	private string numericalId;

	private string versionNumber;

	private string modId;

	private List<string> tags;

	public static SerializedModInfo Create(ModInfo info)
	{
		return new SerializedModInfo
		{
			valid = info.isValidMod,
			mature = info.mature,
			modId = info.modId,
			fileSize = info.fileSizeKB,
			windowsDownloadLink = info.windowsDownloadLink,
			androidDownloadLink = info.androidDownloadLink,
			numericalId = info.numericalId,
			versionNumber = info.version,
			tags = info.tags,
			modName = info.modName,
			thumbnailUrl = info.thumbnailLink,
			summary = info.modSummary
		};
	}

	public void Serialize(INetSerializer serializer)
	{
		if (serializer.IsReader)
		{
			serializer.SerializeValue(ref valid);
			serializer.SerializeValue(ref mature);
			serializer.SerializeValue(ref fileSize);
			serializer.SerializeValue(ref numericalId);
			string text = "";
			serializer.SerializeValue(ref text);
			string[] array = text.Split('¬');
			versionNumber = array[0];
			windowsDownloadLink = array[1];
			androidDownloadLink = array[2];
			modId = array[3];
			thumbnailUrl = array[4];
			summary = array[5];
			modName = array[6];
			int num = int.Parse(array[7]);
			List<string> list = new List<string>();
			int num2 = 8;
			for (int i = 0; i < num; i++)
			{
				list.Add(array[i + num2]);
			}
			modInfo = new ModInfo
			{
				structureVersion = ModInfo.globalStructureVersion,
				isValidMod = valid,
				numericalId = numericalId,
				modId = modId,
				mature = mature,
				version = versionNumber,
				windowsDownloadLink = windowsDownloadLink,
				androidDownloadLink = androidDownloadLink,
				fileSizeKB = fileSize,
				fileName = modId + ".zip",
				modSummary = summary,
				thumbnailLink = thumbnailUrl,
				modName = modName,
				tags = list
			};
			return;
		}
		serializer.SerializeValue(ref valid);
		serializer.SerializeValue(ref mature);
		serializer.SerializeValue(ref fileSize);
		serializer.SerializeValue(ref numericalId);
		string text2 = "";
		text2 = text2 + versionNumber + "¬";
		text2 = text2 + windowsDownloadLink + "¬";
		text2 = text2 + androidDownloadLink + "¬";
		text2 = text2 + modId + "¬";
		text2 = text2 + thumbnailUrl + "¬";
		text2 = text2 + summary + "¬";
		text2 = text2 + modName + "¬";
		text2 += tags.Count;
		foreach (string tag in tags)
		{
			text2 = text2 + "¬" + tag;
		}
		serializer.SerializeValue(ref text2);
	}

	public string ToDebugString()
	{
		string text = "";
		text = text + versionNumber + "¬";
		text = text + windowsDownloadLink + "¬";
		text = text + androidDownloadLink + "¬";
		text = text + modId + "¬";
		text = text + thumbnailUrl + "¬";
		text = text + summary + "¬";
		text = text + modName + "¬";
		text += tags.Count;
		foreach (string tag in tags)
		{
			text = text + "¬" + tag;
		}
		return $"Serialized mod: {text} is valid {valid}, mature {mature}, numerical {numericalId}";
	}
}
