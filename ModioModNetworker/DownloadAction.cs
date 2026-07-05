using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using BoneLib;
using MelonLoader;
using ModioModNetworker;
using Newtonsoft.Json;

public class DownloadAction
{
	public int delayFrames;

	public DownloadAction(int delayFrames)
	{
		this.delayFrames = delayFrames;
	}

	public bool Check()
	{
		if (delayFrames > 0)
		{
			delayFrames--;
			return false;
		}
		return true;
	}

	public void Handle()
	{
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				string text = Path.Combine(ModFileManager.MOD_FOLDER_PATH, "tempfolder");
				if (Directory.Exists(text))
				{
					Directory.Delete(text, recursive: true);
				}
				Directory.CreateDirectory(text);
				MelonLogger.Msg("Extracting " + ModFileManager.downloadPath + " to " + text);
				using (ZipArchive zipArchive = ZipFile.OpenRead(ModFileManager.downloadPath))
				{
					foreach (ZipArchiveEntry entry in zipArchive.Entries)
					{
						string text2 = Path.Combine(text, entry.FullName);
						if (entry.FullName.EndsWith("/"))
						{
							text2 = text2.Substring(0, text2.Length - 1);
							Directory.CreateDirectory(text2);
						}
						else
						{
							Directory.CreateDirectory(Path.GetDirectoryName(text2));
							string fileName = Path.GetFileName(text2);
							string text3 = Path.Combine(Path.GetDirectoryName(text2), "tempExtractedFile.temp");
							entry.ExtractToFile(text3, overwrite: true);
							File.Move(text3, Path.Combine(Path.GetDirectoryName(text3), fileName));
						}
					}
					zipArchive.Dispose();
				}
				MelonLogger.Msg("Extracted " + ModFileManager.downloadPath + " to " + text);
				string text4 = ModFileManager.FindFile(text, "pallet.json");
				while (text4 != "")
				{
					string fullName = Directory.GetParent(text4).FullName;
					MelonLogger.Msg("Mod folder is: " + fullName);
					string text5 = (HelperMethods.IsAndroid() ? fullName.Split('/') : fullName.Split('\\'))[^1];
					string text6 = ModFileManager.MOD_FOLDER_PATH + "/" + text5;
					bool flag = false;
					MelonLogger.Msg("Checking directory if it exists: " + text6);
					if (Directory.Exists(text6))
					{
						MelonLogger.Msg("Directory exists: " + text6);
						flag = true;
						string text7 = ModFileManager.FindFile(text6, "pallet.json");
						if (text7 != "")
						{
							string text8 = File.ReadAllText(text7);
							dynamic val = JsonConvert.DeserializeObject<object>(text8);
							string item = (string)val["objects"]["1"]["barcode"];
							MainClass.warehousePalletReloadTargets.Add(item);
						}
						else
						{
							flag = false;
						}
						Directory.Delete(text6, recursive: true);
					}
					Directory.Move(fullName, text6);
					string path = text6 + "/modinfo.json";
					string contents = JsonConvert.SerializeObject((object)ModlistMenu.activeDownloadModInfo);
					File.WriteAllText(path, contents);
					if (!flag)
					{
						MainClass.warehouseReloadFolders.Add(ModFileManager.FindFile(text6, "pallet.json"));
					}
					text4 = ModFileManager.FindFile(text, "pallet.json");
					MelonLogger.Msg(fullName + " Downloaded and extracted!");
				}
				File.Delete(ModFileManager.downloadPath);
				Directory.Delete(text, recursive: true);
				MainClass.warehouseReloadRequested = true;
				MainClass.RequestInstallCheck();
				MainClass.subsChanged = true;
			}
			catch (Exception ex)
			{
				MelonLogger.Error("Error while downloading mod " + ModlistMenu.activeDownloadModInfo.modId + ": " + ex);
				ModFileManager.isDownloading = false;
				ModFileManager.activeDownloadQueueElement = null;
				ModFileManager.activeDownloadWebRequest = null;
				ModlistMenu.activeDownloadModInfo = null;
			}
		});
		thread.Start();
	}
}
