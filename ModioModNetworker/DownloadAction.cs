using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using BoneLib;
using MelonLoader;
using ModioModNetworker;
using ModioModNetworker.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
					WriteModListingManifest(ModlistMenu.activeDownloadModInfo, text6);
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

	private void WriteModListingManifest(ModInfo modInfo, string modFolderPath)
	{
		try
		{
			string palletPath = ModFileManager.FindFile(modFolderPath, "pallet.json");
			if (string.IsNullOrEmpty(palletPath))
			{
				MelonLogger.Error("WriteModListingManifest: Could not find pallet.json in " + modFolderPath);
				return;
			}
			string palletJson = File.ReadAllText(palletPath);
			dynamic palletData = JsonConvert.DeserializeObject<object>(palletJson);
			string barcode = (string)palletData["objects"]["1"]["barcode"];
			if (string.IsNullOrEmpty(barcode))
			{
				MelonLogger.Error("WriteModListingManifest: Could not find barcode in pallet.json");
				return;
			}
			string catalogPath = ModFileManager.FindFile(modFolderPath, "catalog.json");
			long pcModfileId = 0L;
			long androidModfileId = 0L;
			try
			{
				if (!string.IsNullOrEmpty(modInfo.windowsDownloadLink) && modInfo.windowsDownloadLink.Contains("/files/"))
				{
					pcModfileId = long.Parse(modInfo.windowsDownloadLink.Split("/files/")[1].Replace("/download", ""));
				}
			}
			catch (Exception)
			{
			}
			try
			{
				if (!string.IsNullOrEmpty(modInfo.androidDownloadLink) && modInfo.androidDownloadLink.Contains("/files/"))
				{
					androidModfileId = long.Parse(modInfo.androidDownloadLink.Split("/files/")[1].Replace("/download", ""));
				}
			}
			catch (Exception)
			{
			}
			long numericalId = long.TryParse(modInfo.numericalId, out long parsedNumId) ? parsedNumId : 0L;
			JObject manifest = new JObject();
			JObject objects = new JObject();
			JObject obj1 = new JObject();
			obj1["palletBarcode"] = barcode;
			obj1["palletPath"] = palletPath;
			obj1["catalogPath"] = ((!string.IsNullOrEmpty(catalogPath)) ? catalogPath : "");
			objects["1"] = obj1;
			JObject obj2 = new JObject();
			obj2["barcode"] = barcode;
			obj2["version"] = (modInfo.version ?? "0.0.0");
			obj2["title"] = modInfo.modId;
			obj2["description"] = (modInfo.modSummary ?? "");
			obj2["thumbnailUrl"] = (modInfo.thumbnailLink ?? "");
			obj2["author"] = "ModIoModNetworker";
			JObject targets = new JObject();
			JObject pcTarget = new JObject();
			pcTarget["ref"] = "3";
			pcTarget["type"] = "mod-target-modio#0";
			targets["pc"] = pcTarget;
			int nextRef = 4;
			if (androidModfileId > 0L)
			{
				JObject androidTarget = new JObject();
				androidTarget["ref"] = nextRef.ToString();
				androidTarget["type"] = "mod-target-modio#0";
				targets["android"] = androidTarget;
				nextRef++;
			}
			string infoString = modInfo.ToInfoString();
			if (!string.IsNullOrEmpty(infoString))
			{
				JObject infoTarget = new JObject();
				infoTarget["ref"] = "3";
				infoTarget["type"] = "mod-target-modio#0";
				targets[infoString] = infoTarget;
			}
			obj2["targets"] = targets;
			objects["2"] = obj2;
			JObject obj3 = new JObject();
			obj3["gameId"] = 3809L;
			obj3["modId"] = numericalId;
			obj3["modfileId"] = pcModfileId;
			JObject isa3 = new JObject();
			isa3["type"] = "mod-target-modio#0";
			obj3["isa"] = isa3;
			objects["3"] = obj3;
			if (androidModfileId > 0L)
			{
				JObject obj4 = new JObject();
				obj4["gameId"] = 3809L;
				obj4["modId"] = numericalId;
				obj4["modfileId"] = androidModfileId;
				JObject isa4 = new JObject();
				isa4["type"] = "mod-target-modio#0";
				obj4["isa"] = isa4;
				objects["4"] = obj4;
			}
			manifest["objects"] = objects;
			string manifestPath = Path.Combine(modFolderPath, barcode + ".manifest");
			File.WriteAllText(manifestPath, manifest.ToString(Formatting.Indented));
			MelonLogger.Msg("WriteModListingManifest: Wrote manifest for " + barcode);
		}
		catch (Exception ex)
		{
			MelonLogger.Error("WriteModListingManifest: Error writing manifest: " + ex.Message);
		}
	}
}
