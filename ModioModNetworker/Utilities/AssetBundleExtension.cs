using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace ModioModNetworker.Utilities;

public static class AssetBundleExtension
{
	public static T LoadPersistentAsset<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object
	{
		UnityEngine.Object val = bundle.LoadAsset(name);
		if (val != (UnityEngine.Object)null)
		{
			val.hideFlags = (HideFlags)32;
			return ((Il2CppObjectBase)val).TryCast<T>();
		}
		return default(T);
	}
}
