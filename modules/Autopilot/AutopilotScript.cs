using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.ModAPI.Interfaces;


using SpaceEngineersScripts;

namespace SpaceEngineersScripts.Autopilot
{

	// tag::content[]
	public class AutopilotScript : Script
	{

		string timerblockName = "Timer Block Clock";
		string shipRemoteControlBlockName = "Remote Control Front";
		Ship ship;

		public AutopilotScript(IMyGridTerminalSystem GridTerminalSystem, IMyTerminalBlock Me):base (new GridWrapper(GridTerminalSystem, Me)){
			Initialize ();
		}

		public override void Update (string argument) 
		{
			base.Update (argument);
			Logger.Log ("delta:"+this.deltaMs);

			if (Initialize ()) {
				if(argument!=null && argument.Length>0)
					TraitArgument (argument);

				bool needFastUpdate = ship.Update ();
				if (needFastUpdate) {
					Heartbeat ();
				}
				ship.Save ();
			}
		}

		void TraitArgument (string arg)
		{
			if (arg == null || arg.Length < 1) {
				return;
			} else if (arg.Equals ("on")) {
				ship.AutopilotEnable = true;
			} else if (arg.Equals ("off")) {
				ship.AutopilotEnable = false;
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

					ship.MoveTo (new Vector3D[]{destination}, 0);


				} else if (functionName.StartsWith ("lookAt") || functionName.StartsWith ("lookDir")) {

					double rollAngle = Utils.DEFAULT_DOUBLE;
					var destination = Utils.CastString<Vector3D> (functionArgs [0]);
					if (functionArgs.Count > 1) {
						rollAngle = Utils.CastString<double> (functionArgs [1]);
					}

					ship.LookingAt (destination, rollAngle,functionName.StartsWith ("lookDir"));

				} else if (functionName.StartsWith ("dock")) {
					ship.Dock ();

				}  else if (functionName.StartsWith ("undock")) {
					
					ship.UnDock ();

				}else if (functionName.StartsWith ("precise")) {
					if (functionArgs.Count > 0) {

						bool precise = Utils.CastString<bool> (functionArgs [0]);

						ship.PreciseMode = precise;
					} else {
						ship.PreciseMode = true;

					}

				} 
			}

		}


	

		bool Initialize ()
		{

			if (ship == null) {
				IMyRemoteControl terminalBlock;
				if (shipRemoteControlBlockName != null) {
					terminalBlock = (IMyRemoteControl) GridWrapper.GetBlocksWithName (shipRemoteControlBlockName, "Can 't find "+shipRemoteControlBlockName) [0];

				} else {
					terminalBlock = GridWrapper.GetNearest<IMyRemoteControl> (GridWrapper.Terminal);
				}


				if (terminalBlock == null) {
					Logger.Log ("Can't get the ship remote control block. Please correct the shipRemoteControlBlockName");
					return false;
				}

				ship = new Ship (this, terminalBlock);
				ship.Load ();
			}

			if (timerBlock == null) {
				if (timerblockName != null) {
					timerBlock = GridWrapper.GetBlocksWithName (timerblockName, "Can't find a timer named " + timerblockName) [0];
				}

				if (timerBlock == null) {
					Logger.Log ("Can't get the program timer block. Please correct the timerblockName");
					return false;
				}
			}
			return true;
		}



		IMyTerminalBlock timerBlock;
		void Heartbeat ()
		{

			timerBlock.ApplyAction ("TriggerNow");

		}
	}
	// end::content[]


}

