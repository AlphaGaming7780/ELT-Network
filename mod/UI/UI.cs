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
			
			// AddBinding(showUnTestedObject = new GetterValueBinding<bool>("elt", "transformsection_getpos", () => ((NetworkSettings)Network.ExtensionSettings).ShowUnTestedObject));
			// AddBinding(new TriggerBinding<bool>("elt", "transformsection_getpos", new Action<bool>(SetPosition)));

		}

		// private void ShowUnTestedObject(bool b) {
		// 	((NetworkSettings)Network.ExtensionSettings).ShowUnTestedObject = b;
		// 	Network.Sa
		// }

	}
}