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
		Ship ship;

		public AutopilotScript(IMyGridTerminalSystem GridTerminalSystem, IMyTerminalBlock Me):base (new GridWrapper(GridTerminalSystem, Me)){
			Initialize ();
		}

		public override void Update (string argument) 
		{
			base.Update (argument);
			if (Initialize ()) {
				if(argument!=null && argument.Length>0)
					TraitArgument (argument);


				ship.Save ();
			}
		}

		void TraitArgument (string arg)
		{
			if (arg == null || arg.Length < 1) {
				return;
			} else if (arg.Equals ("on")) {
				ship.setAutopilotEnable(true);
			} else if (arg.Equals ("off")) {
				ship.setAutopilotEnable(false);
			} else if (arg.StartsWith ("GPS:")) {
				var vals = arg.Split (':');
				String vector = Utils.VectorToString (new Vector3D (double.Parse (vals [2]), double.Parse (vals [3]), double.Parse (vals [4])));
				TraitArgument ("go(" + vector + ")");
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

					ship.moveTo (new Vector3D[]{destination}, 0);


				} else if (functionName.StartsWith ("lookAt") || functionName.StartsWith ("lookDir")) {

					double rollAngle = 10000;
					var destination = Utils.CastString<Vector3D> (functionArgs [0]);
					if (functionArgs.Count > 1) {
						rollAngle = Utils.CastString<double> (functionArgs [1]);
					}

					ship.SetLookingDir (destination, rollAngle);

				} else if (functionName.StartsWith ("maxSpeed")) {
					if (functionArgs.Count > 0) {

						double maxVal = Utils.CastString<double> (functionArgs [0]);

						ship.map.SetValue ("maxSpeed", maxVal + "");
					} else {
						ship.map.Remove ("maxSpeed");

					}

				} 
			}

		}


	

		bool Initialize ()
		{
			if (ship == null) {

				IMyRemoteControl terminalBlock;
				if (shipRemoteControlBlockName != null) {
					terminalBlock = (IMyRemoteControl) GridWrapper.GetBlocksWithName (shipRemoteControlBlockName) [0];
				} else {
					terminalBlock = GridWrapper.GetNearest<IMyRemoteControl> (GridWrapper.Terminal);
				}
				ship = new Ship (this, terminalBlock);
				ship.Load ();
			}
			return true;
		}
	}


}

