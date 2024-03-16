using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using ExtraLandscapingTools;
using Game.Prefabs;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
namespace ELT_Network
{
	
	public class UI : UISystemBase
	{
		private static EntityQuery UnTestedPrefabEntityQuery;
        private static GetterValueBinding<bool> showOutsideConnections;
        private static GetterValueBinding<bool> showSpawners;
		protected override void OnCreate() {

			base.OnCreate();
			
			AddBinding(showOutsideConnections = new GetterValueBinding<bool>("elt_networks", "showOutsideConnections", () => Network.network.ExtensionSettings.ShowOutsideConnections));
			AddBinding(new TriggerBinding<bool>("elt_networks", "showOutsideConnections", new Action<bool>(b => ShowOutsideConnections(b))));
			AddBinding(showSpawners = new GetterValueBinding<bool>("elt_networks", "showSpawners", () => Network.network.ExtensionSettings.ShowSpawners));
			AddBinding(new TriggerBinding<bool>("elt_networks", "showSpawners", new Action<bool>(b => ShowSpawners(b))));

			UnTestedPrefabEntityQuery = GetEntityQuery(new EntityQueryDesc
			{
				All =
			   [
					ComponentType.ReadOnly<ObjectData>(),
					ComponentType.ReadOnly<PrefabData>(),
					ComponentType.ReadOnly<UIObjectData>()
			   ],
			});

		}

		private void ShowOutsideConnections(bool newValue)
		{
			Network.network.ExtensionSettings.ShowOutsideConnections = newValue;
			Network.network.SaveSettings(Network.network.ExtensionSettings);
			UpdateUI();
		}

		private void ShowSpawners(bool newValue)
		{
			Network.network.ExtensionSettings.ShowSpawners = newValue;
			Network.network.SaveSettings(Network.network.ExtensionSettings);
			UpdateUI();
		}

		private void UpdateUI() {

			NativeArray<Entity> entities =  UnTestedPrefabEntityQuery.ToEntityArray(AllocatorManager.Temp);

			foreach(Entity entity in entities) {
				if(ELT.m_PrefabSystem.TryGetPrefab(entity, out MarkerObjectPrefab markerObjectPrefab) && markerObjectPrefab is not null) {
					// Plugin.Logger.LogMessage(markerObjectPrefab.name);
					// we can check the name of the prefab we don't want and use the following code on them.
					if(!Network.network.ExtensionSettings.ShowOutsideConnections)
					{
						if (markerObjectPrefab.name.Contains("Outside Connection"))
						{
							Plugin.Logger.LogMessage(markerObjectPrefab.name + " has been removed from UI");
							ELT_UI.RemoveEntityObjectFromCategoryUI(entity);
							continue;
						}
					}
					if (!Network.network.ExtensionSettings.ShowSpawners)
					{
						if (markerObjectPrefab.name.Contains("Spawner"))
						{
							Plugin.Logger.LogMessage(markerObjectPrefab.name + " has been removed from UI");
							ELT_UI.RemoveEntityObjectFromCategoryUI(entity);
							continue;
						}
					}
					ELT_UI.AddEntityObjectToCategoryUI(entity);
				}
			}

			ExtraLandscapingTools.Patches.ToolbarUISystemPatch.UpdateMenuUI();

			showOutsideConnections.Update();
			showSpawners.Update();
		}
	}
}