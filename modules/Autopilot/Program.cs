using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Game.ModAPI.Ingame;

namespace SpaceEngineersScripts.Autopilot
{
	public class Program : SEScriptStruct ,  BaseScriptMethods
	{
		AutopilotScript script;

		public Program(){
			Utils.Echo = Echo;
		}

		public void Main (string argument)
		{
			if (script == null) {
				script = new AutopilotScript (GridTerminalSystem, Me);
			}
			script.CalculateDelta ();
			script.Update (argument);
		}

		public void Save ()
		{
		}

	}


}

