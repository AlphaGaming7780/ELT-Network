using System.IO;
using System.Reflection;
using Game.Prefabs;
namespace ELT_Assets
{
	public class Assets
	{

		internal static Stream GetEmbedded(string embeddedPath) {
			return Assembly.GetExecutingAssembly().GetManifestResourceStream("ELT-Assets.embedded."+embeddedPath);
		}

		internal static string GetIcon(PrefabBase prefab) {

			if(File.Exists($"{GameManager_Awake.resourcesIcons}/{prefab.GetType().Name}/{prefab.name}.svg")) return $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/Icons/{prefab.GetType().Name}/{prefab.name}.svg";


			if(prefab is PathwayPrefab) {
				// return prefab.name switch
				// {   
				// 	_ => "Media/Game/Icons/Pathways.svg",
				// };
				return "Media/Game/Icons/Pathways.svg";
			} else if (prefab is TrackPrefab trackPrefab) {
				if(trackPrefab.m_TrackType == Game.Net.TrackTypes.Train) {
					// return prefab.name switch
					// {   
					// 	_ => "Media/Game/Icons/DoubleTrainTrack.svg",
					// };
					return "Media/Game/Icons/DoubleTrainTrack.svg";
				}
				else if(trackPrefab.m_TrackType == Game.Net.TrackTypes.Subway) {
					// return prefab.name switch
					// {   
					// 	_ => "Media/Game/Icons/DoubleTrainTrack.svg",
					// };
					return "Media/Game/Icons/DoubleTrainTrack.svg";
				}
				else if(trackPrefab.m_TrackType == Game.Net.TrackTypes.Tram) {
                    // return prefab.name switch
                    // {   
                    // 	_ => "Media/Game/Icons/OnewayTramTrack.svg",
                    // };
					return "Media/Game/Icons/OnewayTramTrack.svg";
                }
			} else if(prefab is RoadPrefab roadPrefab) {
				// return roadPrefab.name switch
				// {   
				// 	"Golden Gate Road" => "Media/Game/Icons/LargeRoad.svg",
				// 	_ => $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/Icons/Misc/placeholder.svg",
				// };
				return $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/Icons/Misc/placeholder.svg";
			} else if(prefab is UIAssetMenuPrefab) {

				// return prefab.name switch
				// {   
				// 	"Props" => $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/Icons/UIAssetMenuPrefab/{prefab.name}.svg",
				// 	_ => $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/Icons/Misc/placeholder.svg",
				// };
				return $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/Icons/Misc/placeholder.svg";
			} else if (prefab is UIAssetCategoryPrefab) {
				// return prefab.name switch
				// {   
				// 	"Electrical & Light" => $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/Icons/UIAssetCategoryPrefab/{prefab.name}.svg",
				// 	_ => $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/Icons/Misc/placeholder.svg",
				// };
				return $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/Icons/Misc/placeholder.svg";
			}

			return $"{GameManager_InitializeThumbnails.COUIBaseLocation}/resources/Icons/Misc/placeholder.svg";
		}

		internal static UIAssetCategoryPrefab GetCatUIForRaod(PrefabSystem prefabSystem, RoadPrefab roadPrefab) {
			return roadPrefab.name switch
			{   
				"Golden Gate Road" => PrefabSystem_AddPrefab.GetExistingToolCategory(prefabSystem, roadPrefab , "RoadsLargeRoads"),
				_ => PrefabSystem_AddPrefab.GetOrCreateNewToolCategory(prefabSystem, roadPrefab , "Roads", "Hidden Roads")
			};
		}
	}
}