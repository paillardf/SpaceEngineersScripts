using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;

namespace SpaceEngineersScripts.Autopilot
{
	public class Ship : BlockWrapper
	{
		const int IFRONT = 0;
		const int IBACK = 1;
		const int ILEFT = 2;
		const int IRIGHT = 3;
		const int IUP = 4;
		const int IDOWN = 5;

		GridWrapper GridWrapper;

		LogWrapper Logger;

		List<Vector3D> orientation;
		IMyRemoteControl[] rcBlocks = new IMyRemoteControl[6];

		public Ship (AutopilotScript script,IMyRemoteControl block):base(block)
		{
			this.Logger = script.Logger;
			this.GridWrapper = script.GridWrapper;
			rcBlocks [IFRONT] = block;
			orientation = new List<Vector3D>{VectorForward, VectorBackward, VectorLeft, VectorRight, VectorUp, VectorDown};
			rcBlocks [IFRONT] = block;

			var rcs = GridWrapper.GetBlocks<IMyRemoteControl> ();
			for (int i = 0; i < rcs.Count; i++) {
				if (rcs [i].NumberInGrid != block.NumberInGrid) {
					int index = orientation.IndexOf (new BlockWrapper (rcs [i]).VectorForward);
						if(rcBlocks[index] == null){
							rcBlocks [index] = (IMyRemoteControl) rcs [i];
						}
				}
			}

		}

		public void setAutopilotEnable (bool b)
		{
			//TODO
		}


		public void moveTo(Vector3D[] destination, int sens = IFRONT){
			IMyRemoteControl controlBlock = rcBlocks [sens];
			if (controlBlock == null) {
				Logger.Log("can't move no remoteControl block available in this direction");
				return;
			}
			controlBlock.ClearWaypoints ();
			for (int i = 0; i < destination.Length; i++) {
				controlBlock.AddWaypoint (destination[i], Utils.VectorToStringRound(destination[i]));
				this.Destination = destination[i];
			}
		}

		public void SetLookingDir (Vector3D lookAt, double rollAngle)
		{
			throw new NotImplementedException ();
		}

		private Vector3D CurrentPosition   { 
			get { return block.GetPosition; } 
		}


		private Vector3D Destination   { 
			get {
				Vector3D destination = CurrentPosition;
				Vector3D.TryParse (map.GetValue ("dest"), out destination);
				return destination;
			}
			set {
				map.SetValue ("dest", value);
			}
		}

		public bool IsArrived ()
		{
			return Vector3D.Distance (Destination, CurrentPosition) < 0.3f;
		}

	}
}

