using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

using SpaceEngineersScripts;

namespace SpaceEngineersScripts.Autopilot
{
	public class AutopilotScript : Script
	{


		string shipRemoteControlBlockName;


		//------------------------------------------------------------------------
		//--------------------COMMAND --------------------
		//------------------------------------------------------------------------
		//   on (start autopilot)
		//   off (stop autopilot)

		bool autopilotEnable = false;




		BlockWrapper ship;
		public AutopilotScript(IMyGridTerminalSystem GridTerminalSystem, IMyTerminalBlock Me):base (GridTerminalSystem, Me){
			Initialize ();
		}

		public override void Update (string argument)
		{
			if (Initialize ()) {
				if(argument!=null && argument.Length>0)
					TraitArgument (argument);


				ship.Save ();
			}
		}





		//------------------------------------------------------------------------
		//--------------------START --------------------
		//------------------------------------------------------------------------




		void TraitArgument (string arg)
		{
			autopilotEnable = ship.map.Contains ("autoPilot");
			if (arg == null || arg.Length < 1) {
				return;
			} else if (arg.Equals ("on")) {
				autopilotEnable = true;
				setGyrosOverride (autopilotEnable);
			} else if (arg.Equals ("off")) {
				autopilotEnable = false;
				//TODO stopThrusters (GetBlocks<IMyThrust> ());
				setGyrosOverride (autopilotEnable);
			} else if (arg.StartsWith ("GPS:")) {
				var vals = arg.Split (':');
				String vector = Utils.VectorToString (new Vector3D (double.Parse (vals [2]), double.Parse (vals [3]), double.Parse (vals [4])));
				Update ("go(" + vector + ")");
			} else {
				String functionName;
				List<string> functionArgs = Utils.ExtractFunctionParameters (arg, out functionName);
				if (functionName.StartsWith ("go") || functionName.StartsWith ("goDelta")) {

					var destination = Utils.DEFAULT_VECTOR_3D;
					if (functionArgs.Count > 0)
						destination = Utils.CastString<Vector3D> (functionArgs [0]);



					if (functionName.EndsWith ("Delta")) {
						destination = ship.VectorPosition + ship.VectorForward * destination.GetDim (0) + ship.VectorLeft * destination.GetDim (1) + ship.VectorUp * destination.GetDim (2);
					}

					SetDestination (destination);


				} else if (functionName.StartsWith ("lookAt") || functionName.StartsWith ("lookDir")) {

					double rollAngle = 10000;
					var destination = Utils.CastString<Vector3D> (functionArgs [0]);
					if (functionArgs.Count > 1) {
						rollAngle = Utils.CastString<double> (functionArgs [1]);
					}

					SetLookingDir (destination, rollAngle, functionName.StartsWith ("lookDir"), false);

				} else if (functionName.StartsWith ("maxSpeed")) {
					if (functionArgs.Count > 0) {

						double maxVal = Utils.CastString<double> (functionArgs [0]);

						ship.map.SetValue ("maxSpeed", maxVal + "");
					} else {
						ship.map.Remove ("maxSpeed");

					}

				} else if (functionName.StartsWith ("lockLook")) {
					bool v = false;
					if (functionArgs.Count > 0) {
						v = Utils.CastString<bool> (functionArgs [0]);
					}
					LockLook (v);
				}
			}
			if (autopilotEnable) {
				ship.map.SetValue ("autoPilot", "1");
			} else {
				ship.map.Remove ("autoPilot");
			}
		}


	

		bool Initialize ()
		{
			//if (logLevel > LOG_NORMAL)
			//		log = "";

			bool success = true;
			if (ship == null) {

				IMyTerminalBlock terminalBlock;
				if (GetBlocks<IMyCockpit> ().Count != 0) {
					var shipBlocks = GetBlocks<IMyCockpit> ();
					terminalBlock = shipBlocks [0];
				} else if (shipRemoteControlBlockName != null) {
					terminalBlock = GetBlocksWithName (shipRemoteControlBlockName) [0];
				} else {
					terminalBlock = GetNearest<IMyRemoteControl> (Me);
				}
				ship = new BlockWrapper (terminalBlock);
				ship.Load ();
				ship.map.SetValue ("autopilotBlock", Me.CustomName);
			}

			success = success && resetThrusters ();

			if (!success) {
				ship = null;
			}
			return success;

		}








	}


}

