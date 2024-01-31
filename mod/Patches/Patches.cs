using HarmonyLib;
using Game.UI.InGame;
using Game.Tools;
using Game.Prefabs;
using Unity.Entities;
using Game.SceneFlow;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Game.UI;
using System.Linq;
using Game;
using System;
using ExtraLandscapingTools;
using System.IO.Compression;
using UnityEngine;
using Game.Net;
using Colossal.IO.AssetDatabase;

namespace ELT_Assets
{

	[HarmonyPatch(typeof(GameManager), "Awake")]
	internal class GameManager_Awake
	{

		static readonly string pathToZip = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+"\\resources.zip";
		static internal readonly string resources = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "resources");
		static internal readonly string resourcesIcons = Path.Combine(resources, "Icons");

		static void Prefix(GameManager __instance)
		{		
			ELT.RegisterELTExtension(Assembly.GetExecutingAssembly().FullName, ELT.ELT_ExtensionType.Assets);

			if(File.Exists(pathToZip)) {
				if(Directory.Exists(resources)) Directory.Delete(resources, true);
				ZipFile.ExtractToDirectory(pathToZip, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
				File.Delete(pathToZip);
			}

		}
	}

	[HarmonyPatch(typeof(GameManager), "InitializeThumbnails")]
	internal class GameManager_InitializeThumbnails
	{	
		static readonly string IconsResourceKey = $"{MyPluginInfo.PLUGIN_NAME.ToLower()}";

		public static readonly string COUIBaseLocation = $"coui://{IconsResourceKey}";

		static void Prefix(GameManager __instance)
		{
			List<string> pathToIconToLoad = [Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)];

			var gameUIResourceHandler = (GameUIResourceHandler)GameManager.instance.userInterface.view.uiSystem.resourceHandler;
			
			if (gameUIResourceHandler == null)
			{
				UnityEngine.Debug.LogError("Failed retrieving GameManager's GameUIResourceHandler instance, exiting.");
				return;
			}
			
			gameUIResourceHandler.HostLocationsMap.Add(
				IconsResourceKey, pathToIconToLoad
			);
		}
	}

	[HarmonyPatch( typeof( ToolUISystem ), "OnToolChanged", typeof(ToolBaseSystem) )]
	class ToolUISystem_OnToolChanged
	{
		internal static bool markersVisible = false;
		private static void Postfix( ToolBaseSystem tool ) {
            if (tool.GetPrefab() != null)
            {
                Entity entity = ELT.m_PrefabSystem.GetEntity(tool.GetPrefab());
				
				if (ELT.m_EntityManager.HasComponent<MarkerNetData>(entity) ||  tool.GetPrefab() is MarkerObjectPrefab) {
					if(!markersVisible)
					{	
						Plugin.Logger.LogMessage("Show Marker");
						ELT_UI.ShowMarker(true);
						markersVisible = true;
					}
				}
				else if (markersVisible)
				{	
					Plugin.Logger.LogMessage("Hide Marker");
					ELT_UI.ShowMarker(false);
					markersVisible = false;
				}
            }
		}
	}

	[HarmonyPatch( typeof( ToolUISystem ), "OnPrefabChanged", typeof(PrefabBase) )]
	class ToolUISystem_OnPrefabChanged
	{
		private static void Postfix(PrefabBase prefab) {
			Entity entity = ELT.m_PrefabSystem.GetEntity(prefab);

			if (ELT.m_EntityManager.HasComponent<MarkerNetData>(entity) ||  prefab is MarkerObjectPrefab) {
				if(!ToolUISystem_OnToolChanged.markersVisible)
				{
					Plugin.Logger.LogMessage("Show Marker");
					ELT_UI.ShowMarker(true);
					ToolUISystem_OnToolChanged.markersVisible = true;
				}
			}
			else if (ToolUISystem_OnToolChanged.markersVisible)
			{	
				Plugin.Logger.LogMessage("Hide Marker");
				ELT_UI.ShowMarker(false);
				ToolUISystem_OnToolChanged.markersVisible = false;
			}
		}
	}

	[HarmonyPatch(typeof(PrefabSystem), nameof(PrefabSystem.AddPrefab))]
	public class PrefabSystem_AddPrefab
	{
		private static readonly string[] removeTools = [];

		private static string UIAssetCategoryPrefabName = "";
		private static readonly Dictionary<string, List<PrefabBase>> failedPrefabs = [];

		public static bool Prefix( PrefabSystem __instance, PrefabBase prefab)
		{

			if (Traverse.Create(__instance).Field("m_Entities").GetValue<Dictionary<PrefabBase, Entity>>().ContainsKey(prefab)) {
				return false;
			}

			if(prefab is UIAssetMenuPrefab) {
				if(prefab.name == "Landscaping") {
					if (!__instance.TryGetPrefab(new PrefabID(nameof(UIAssetMenuPrefab), "Props"), out var p2)
					|| p2 is not UIAssetMenuPrefab plopItgMenu)
					{
						plopItgMenu = ScriptableObject.CreateInstance<UIAssetMenuPrefab>();
						plopItgMenu.name = "Props";
						var plopItgMenuUI = plopItgMenu.AddComponent<UIObject>();
						plopItgMenuUI.m_Icon = Assets.GetIcon(plopItgMenu);
						plopItgMenuUI.m_Priority = prefab.GetComponent<UIObject>().m_Priority+1;
						plopItgMenuUI.active = true;
						plopItgMenuUI.m_IsDebugObject = false;
						plopItgMenuUI.m_Group = prefab.GetComponent<UIObject>().m_Group;

						__instance.AddPrefab(plopItgMenu);
					}
				}
			}

			try {

				if(failedPrefabs.ContainsKey(UIAssetCategoryPrefabName)) {
					string cat = UIAssetCategoryPrefabName;
					UIAssetCategoryPrefabName = "";
					PrefabBase[] temp = new PrefabBase[failedPrefabs[cat].Count];
					failedPrefabs[cat].CopyTo(temp);
					foreach(PrefabBase prefabBase in temp) {
						failedPrefabs[cat].Remove(prefabBase);
						__instance.AddPrefab(prefabBase);
					}
				}

				if (removeTools.Contains(prefab.name) || 
					(
						prefab is not PathwayPrefab && 
						prefab is not TrackPrefab && 
						prefab is not StaticObjectPrefab && 
						prefab is not RoadPrefab &&
						prefab is not MarkerObjectPrefab
					) || 
					prefab is BuildingExtensionPrefab ||
					prefab is BuildingPrefab)
				{	
					if(prefab is UIAssetMenuPrefab uIAssetMenuPrefab && failedPrefabs.ContainsKey(uIAssetMenuPrefab.name)) {
						UIAssetCategoryPrefabName = uIAssetMenuPrefab.name;
					} else if(prefab is UIAssetCategoryPrefab uIAssetCategoryPrefab && failedPrefabs.ContainsKey(uIAssetCategoryPrefab.name)) {
						UIAssetCategoryPrefabName = uIAssetCategoryPrefab.name;
					}
					return true;
				}

				if(prefab is StaticObjectPrefab && (
					prefab.name.ToLower().Contains("wall") || 
					prefab.name.ToLower().Contains("random") || 
					prefab.name.ToLower().Contains("decal") || 
					prefab.name.ToLower().Contains("roadarrow") ||
					prefab.name.ToLower().Contains("poster") ||
					prefab.name.ToLower().Contains("signsideway") ||
					prefab.name.ToLower().Contains("billboardround") ||
					prefab.name.ToLower().Contains("trashbag"))) {
					return true;
				}

				var prefabUI = prefab.GetComponent<UIObject>();
				if (prefabUI == null)
				{
					prefabUI = prefab.AddComponent<UIObject>();
					prefabUI.active = true;
					prefabUI.m_IsDebugObject = false;
					prefabUI.m_Icon = Assets.GetIcon(prefab);
					prefabUI.m_Priority = 1;
				}

				if(prefab is StaticObjectPrefab) 
				{
					if(prefab.name.ToLower().Contains("fence")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Fence");
					else if(prefab.name.ToLower().Contains("bench")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Parks");
					else if(prefab.name.ToLower().Contains("chair")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Parks");
					else if(prefab.name.ToLower().Contains("fountain")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Parks");
					else if(prefab.name.ToLower().Contains("astand")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "A Stand");
					else if(prefab.name.ToLower().Contains("stand")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Stand");
					else if(prefab.name.ToLower().Contains("table")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab, "Props", "Parks");
					else if(prefab.name.ToLower().Contains("camp")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab, "Props", "Parks");
					else if(prefab.name.ToLower().Contains("construction")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Misc");
					else if(prefab.name.ToLower().Contains("billboardsmall")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Billboard Small");
					else if(prefab.name.ToLower().Contains("billboardmedium")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Billboard Medium");
					else if(prefab.name.ToLower().Contains("billboardlarge")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Billboard Large");
					else if(prefab.name.ToLower().Contains("billboardhuge")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Billboard Huge");
					else if(prefab.name.ToLower().Contains("signfrontwaylarge")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Signe Front Way Large");
					else if(prefab.name.ToLower().Contains("signfrontwaymedium")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Signe Front Way Medium");
					else if(prefab.name.ToLower().Contains("signfrontwaysmall")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Signe Front Way Small");
					else if(prefab.name.ToLower().Contains("screen")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Screen");
					else if(prefab.name.ToLower().Contains("trash")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Misc");
					else if(prefab.name.ToLower().Contains("food")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Misc");
					else if(prefab.name.ToLower().Contains("carport")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Misc");
					else if(prefab.name.ToLower().Contains("cardbox")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Misc");
					else if(prefab.name.ToLower().Contains("make")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Misc");
					else if(prefab.name.ToLower().Contains("cone")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Misc");
					else if(prefab.name.ToLower().Contains("swim")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Misc");
					else if(prefab.name.ToLower().Contains("power")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Electrical & Light");
					else if(prefab.name.ToLower().Contains("electrical")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Electrical & Light");
					else if(prefab.name.ToLower().Contains("light")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Electrical & Light");
					else if(prefab.name.ToLower().Contains("parking")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Parking");
					else if(prefab.name.ToLower().Contains("park") && !prefab.name.ToLower().Contains("parkly")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Parks");
					else if(prefab.name.ToLower().Contains("seesaw")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Parks");
					else if(prefab.name.ToLower().Contains("swing")) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab , "Props", "Parks");
					else prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab, "Props", "StaticObjectPrefab");
				}
				else if(prefab is PathwayPrefab) prefabUI.m_Group ??= GetExistingToolCategory(__instance, prefab, "Pathways");
				else if(prefab is TrackPrefab trainTrackPrefab && trainTrackPrefab.m_TrackType == TrackTypes.Train) prefabUI.m_Group ??= GetExistingToolCategory(__instance, prefab, "TransportationTrain");
				else if(prefab is TrackPrefab SubwayTrackPrefab && SubwayTrackPrefab.m_TrackType == TrackTypes.Subway) prefabUI.m_Group ??= GetExistingToolCategory(__instance, prefab, "TransportationSubway");
				else if(prefab is TrackPrefab TramTrackPrefab && TramTrackPrefab.m_TrackType == TrackTypes.Tram) prefabUI.m_Group ??= GetExistingToolCategory(__instance, prefab, "TransportationTram"); 
				else if(prefab is RoadPrefab roadPrefab) prefabUI.m_Group ??= Assets.GetCatUIForRaod(__instance , roadPrefab);
				else if(prefab is MarkerObjectPrefab) prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab, "Landscaping", "Marker Object Prefab", "Spaces");
				else prefabUI.m_Group ??= GetOrCreateNewToolCategory(__instance, prefab, "Props", "Failed Prefab, IF you this tabe, repport it, it's a bug.");

				if(prefabUI.m_Group == null) {
					return false;
				}
			} catch (Exception e) {Plugin.Logger.LogError(e);}
			return true;
		}

		internal static UIAssetCategoryPrefab GetExistingToolCategory(PrefabSystem prefabSystem, PrefabBase prefabBase ,string cat)
		{

			if (!prefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetCategoryPrefab), cat), out var p1)
				|| p1 is not UIAssetCategoryPrefab terraformingCategory)
			{	
				AddPrefabToFailedPrefabList(prefabBase, cat);
				return null;
			}

			return terraformingCategory;

		}

		internal static UIAssetCategoryPrefab GetOrCreateNewToolCategory(PrefabSystem prefabSystem, PrefabBase prefabBase, string menu , string cat, string behindcat = null)
		{

			if (prefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetCategoryPrefab), cat), out var p1)
				&& p1 is UIAssetCategoryPrefab surfaceCategory)
			{
				return surfaceCategory;
			}

			if (!prefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetMenuPrefab), menu), out var p2)
				|| p2 is not UIAssetMenuPrefab uIAssetMenuPrefab)
			{	
				AddPrefabToFailedPrefabList(prefabBase, menu);
				return null;
			}

			surfaceCategory = ScriptableObject.CreateInstance<UIAssetCategoryPrefab>();
			surfaceCategory.name = cat;
			surfaceCategory.m_Menu = uIAssetMenuPrefab;
			var surfaceCategoryUI = surfaceCategory.AddComponent<UIObject>();
			surfaceCategoryUI.m_Icon = Assets.GetIcon(surfaceCategory);
			surfaceCategoryUI.active = true;
			surfaceCategoryUI.m_IsDebugObject = false;

			if(behindcat != null) 
			{ 
				if (!prefabSystem.TryGetPrefab(new PrefabID(nameof(UIAssetCategoryPrefab), behindcat), out var p3)
					|| p3 is not UIAssetCategoryPrefab behindCategory)
				{
					AddPrefabToFailedPrefabList(prefabBase, behindcat);
					return null;
				}
				surfaceCategoryUI.m_Priority = behindCategory.GetComponent<UIObject>().m_Priority+1;

			}

			prefabSystem.AddPrefab(surfaceCategory);


			return surfaceCategory;
		}

		static void AddPrefabToFailedPrefabList(PrefabBase prefabBase, string cat) {
			if(failedPrefabs.ContainsKey(cat)) {
				failedPrefabs[cat].Add(prefabBase);
			} else {
				failedPrefabs.Add(cat, []);
				failedPrefabs[cat].Add(prefabBase);
			}
		}
	}
}
