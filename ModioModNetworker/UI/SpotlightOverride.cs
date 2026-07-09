using System;
using ModioModNetworker.UI;
using ModioModNetworker.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace ModioModNetworker.UI;

public class SpotlightOverride
{
	public string manualDisplayId;

	public string descriptionOverride;

	public string titleOverride;

	public string subTitle;

	public ModInfo downloadedInfo;

	public Texture cachedThumbnail;

	public static void LoadFromRegularURL()
	{
		UnityWebRequest httpWebRequest = UnityWebRequest.Get("https://raw.githubusercontent.com/notnotnotswipez/ModioModNetworker/networker-spotlight/modSpotlight.txt");
		UnityWebRequestAsyncOperation val = httpWebRequest.SendWebRequest();
		((AsyncOperation)val).m_completeCallback = ((AsyncOperation)val).m_completeCallback + new Action<AsyncOperation>(delegate
		{
			string text = httpWebRequest.downloadHandler.text;
			string[] array = text.Split('\n');
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				if (text2.StartsWith("manualDisplayMod: "))
				{
					NetworkerMenuController.spotlightOverride.manualDisplayId = GetSegmentOrNull(text2, "manualDisplayMod: ");
					if (NetworkerMenuController.spotlightOverride.manualDisplayId != null)
					{
						ModInfo.RequestModInfoNumerical(NetworkerMenuController.spotlightOverride.manualDisplayId, "spotlight");
					}
				}
				if (text2.StartsWith("subTitle: "))
				{
					NetworkerMenuController.spotlightOverride.subTitle = GetSegmentOrNull(text2, "subTitle: ");
				}
				if (text2.StartsWith("titleOverride: "))
				{
					NetworkerMenuController.spotlightOverride.titleOverride = GetSegmentOrNull(text2, "titleOverride: ");
				}
				if (text2.StartsWith("descriptionOverride: "))
				{
					NetworkerMenuController.spotlightOverride.descriptionOverride = GetSegmentOrNull(text2, "descriptionOverride: ");
				}
			}
		});
	}

	private static string GetSegmentOrNull(string line, string startWith)
	{
		string text = line.Replace(startWith, "");
		if (text != "null")
		{
			return text;
		}
		return null;
	}
}
