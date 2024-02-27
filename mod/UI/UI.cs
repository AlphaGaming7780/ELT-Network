using System;
using Colossal.UI.Binding;
using Game.UI;
using Game.UI.InGame;

namespace ELT_Network
{
	
	public class UI : UISystemBase
	{	
        private static GetterValueBinding<bool> showUnTestedObject;
		protected override void OnCreate() {

			base.OnCreate();
			
			AddBinding(showUnTestedObject = new GetterValueBinding<bool>("elt_networks", "showuntestedobject", () => Network.network.ExtensionSettings.ShowUnTestedObject));
			AddBinding(new TriggerBinding<bool>("elt_networks", "showuntestedobject", new Action<bool>(ShowUnTestedObject)));

		}

		private void ShowUnTestedObject(bool b) {
			Plugin.Logger.LogMessage(b);
			Network.network.ExtensionSettings.ShowUnTestedObject = b;
			Network.network.SaveSettings(Network.network.ExtensionSettings);
			showUnTestedObject.Update();
		}

	}
}