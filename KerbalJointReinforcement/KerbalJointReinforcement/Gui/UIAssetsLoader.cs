using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace KerbalJointReinforcement
{
#if IncludeAnalyzer

	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class UIAssetsLoader : MonoBehaviour
	{
		internal static AssetBundle KJRAssetBundle;

		// windows
		internal static GameObject settingsWindowPrefab;
		internal static GameObject optionLinePrefab;
		internal static GameObject optionInputLinePrefab;
		internal static GameObject optionSliderLinePrefab;

		// images and icons
		internal static List<Texture2D> iconAssets;
		internal static List<UnityEngine.Sprite> spriteAssets;
		
		public static bool allPrefabsReady = false;

		public IEnumerator LoadBundle(string location)
		{
			while(!Caching.ready)
				yield return null;

			using(UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(location))
			{
				yield return www.SendWebRequest();

				KJRAssetBundle = DownloadHandlerAssetBundle.GetContent(www);

				LoadBundleAssets();
			}
		}
		
		private void LoadBundleAssets()
		{
			var prefabs = KJRAssetBundle.LoadAllAssets<GameObject>();
			int prefabCounter = 0;

			for(int i = 0; i < prefabs.Length; i++)
			{
				if(prefabs[i].name == "KJRSettingsWindowPrefab")
				{
					settingsWindowPrefab = prefabs[i] as GameObject;
					prefabCounter++;
					Logger.Log("Successfully loaded KJRSettingsWindowPrefab", Logger.Level.Debug);
				}

				if(prefabs[i].name == "OptionLinePrefab")
				{
					optionLinePrefab = prefabs[i] as GameObject;
					prefabCounter++;
					Logger.Log("Successfully loaded OptionLinePrefab", Logger.Level.Debug);
				}

				if(prefabs[i].name == "OptionInputLinePrefab")
				{
					optionInputLinePrefab = prefabs[i] as GameObject;
					prefabCounter++;
					Logger.Log("Successfully loaded OptionInputLinePrefab", Logger.Level.Debug);
				}

				if(prefabs[i].name == "OptionSliderLinePrefab")
				{
					optionSliderLinePrefab = prefabs[i] as GameObject;
					prefabCounter++;
					Logger.Log("Successfully loaded OptionSliderLinePrefab", Logger.Level.Debug);
				}
			}

			allPrefabsReady = (prefabCounter >= 4);

			spriteAssets = new List<UnityEngine.Sprite>();
			var sprites = KJRAssetBundle.LoadAllAssets<UnityEngine.Sprite>();

			for(int i = 0; i < sprites.Length; i++)
			{
				if(sprites[i] != null)
				{
					spriteAssets.Add(sprites[i]);
					Logger.Log("Successfully loaded Sprite " + sprites[i].name, Logger.Level.Debug);
				}
			}

			iconAssets = new List<Texture2D>();
			var icons = KJRAssetBundle.LoadAllAssets<Texture2D>();

			for(int i = 0; i < icons.Length; i++)
			{
				if(icons[i] != null)
				{
					iconAssets.Add(icons[i]);
					Logger.Log("Successfully loaded texture " + icons[i].name, Logger.Level.Debug);
				}
			}
		}

		public void Start()
		{
			if(allPrefabsReady)
				return;

			var assemblyFile = Assembly.GetExecutingAssembly().Location;
			var bundlePath = "file://" + assemblyFile.Replace(new FileInfo(assemblyFile).Name, "").Replace("\\","/") + "../AssetBundles/";

			Logger.Log("Loading bundles from BundlePath: " + bundlePath, Logger.Level.Debug);

			Caching.ClearCache();

			StartCoroutine(LoadBundle(bundlePath + "kjr_ui_objects.ksp"));
		}

		public void OnDestroy()
		{
		}
	}

#endif
}

