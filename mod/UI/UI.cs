using System;
using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using ExtraLandscapingTools;
using Game.Prefabs;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ELT_Network
{
	
	public class UI : UISystemBase
	{
		private static EntityQuery OutsideConnectionsQuery;
		private static EntityQuery SpawnersEntityQuery;
        private static GetterValueBinding<bool> showOutsideConnections;
        private static GetterValueBinding<bool> showSpawners;
		protected override void OnCreate() {

			base.OnCreate();
			
			AddBinding(showOutsideConnections = new GetterValueBinding<bool>("elt_networks", "showOutsideConnections", () => Network.network.ExtensionSettings.ShowOutsideConnections));
			AddBinding(new TriggerBinding<bool>("elt_networks", "showOutsideConnections", new Action<bool>(b => ShowOutsideConnections(b))));
			AddBinding(showSpawners = new GetterValueBinding<bool>("elt_networks", "showSpawners", () => Network.network.ExtensionSettings.ShowSpawners));
			AddBinding(new TriggerBinding<bool>("elt_networks", "showSpawners", new Action<bool>(b => ShowSpawners(b))));

			OutsideConnectionsQuery = GetEntityQuery(new EntityQueryDesc
			{
				All =
			   [
					ComponentType.ReadOnly<UIObjectData>()
			   ],
				Any =
				[
					ComponentType.ReadOnly<OutsideConnectionData>(),
				]
			});

			SpawnersEntityQuery = GetEntityQuery(new EntityQueryDesc
			{
				All =
				[
					ComponentType.ReadOnly<UIObjectData>()
				],
				Any =
				[
					ComponentType.ReadOnly<TrafficSpawnerData>(),
					ComponentType.ReadOnly<CreatureSpawnData>(),
				]
			});

		}

		protected override void OnGameLoaded(Context serializationContext)
		{
			if (serializationContext.purpose == Purpose.LoadGame)
			{
				ShowOutsideConnections(Network.network.ExtensionSettings.ShowOutsideConnections);
				ShowSpawners(Network.network.ExtensionSettings.ShowSpawners);
			}
			base.OnGameLoaded(serializationContext);
		}

		private void ShowOutsideConnections(bool newValue)
		{
			Network.network.ExtensionSettings.ShowOutsideConnections = newValue;
			Network.network.SaveSettings(Network.network.ExtensionSettings);

			NativeArray<Entity> entities =  OutsideConnectionsQuery.ToEntityArray(AllocatorManager.Temp);
			ToggleEntityInUI(entities, newValue);
			showOutsideConnections.Update();
		}

		private void ShowSpawners(bool newValue)
		{
			Network.network.ExtensionSettings.ShowSpawners = newValue;
			Network.network.SaveSettings(Network.network.ExtensionSettings);

			NativeArray<Entity> entities =  SpawnersEntityQuery.ToEntityArray(AllocatorManager.Temp);
			ToggleEntityInUI(entities, newValue);



			showSpawners.Update();
		}

		private void ToggleEntityInUI(NativeArray<Entity> entities, bool value)
		{
			foreach (Entity entity in entities)
			{
				if (!value)
				{
					ELT_UI.RemoveEntityObjectFromCategoryUI(entity);
					continue;
				}
				ELT_UI.AddEntityObjectToCategoryUI(entity);
			}
			ExtraLandscapingTools.Patches.ToolbarUISystemPatch.UpdateMenuUI();
		}
	}
}