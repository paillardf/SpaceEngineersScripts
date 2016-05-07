using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;

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

		List<IMyTerminalBlock> gyrosBlocks;

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

			gyrosBlocks = GridWrapper.GetBlocks<IMyGyro> ();

		}
			

		public bool AutopilotEnable
		{
			get{
				return gyrosBlocks [0].GetProperty ("Override").AsBool ().GetValue(gyrosBlocks [0]);
			}
			set{
				for (int i = 0; i < gyrosBlocks.Count; i++) {
					gyrosBlocks [i].GetProperty ("Override").AsBool ().SetValue (gyrosBlocks [i], value);
				}
				rcBlocks [this.MoveOrientation].SetAutoPilotEnabled (!IsArrived() && value);

			}
		}


		public void MoveTo(Vector3D[] destination, int sens = IFRONT){
			IMyRemoteControl controlBlock = rcBlocks [sens];
			if (controlBlock == null) {
				Logger.Log("can't move no remoteControl block available in this direction");
				return;
			}
			rcBlocks [this.MoveOrientation].SetAutoPilotEnabled (false);
			controlBlock.ClearWaypoints ();
			this.MoveOrientation = sens;
			for (int i = 0; i < destination.Length; i++) {
				controlBlock.AddWaypoint (destination[i], Utils.VectorToStringRound(destination[i]));
				this.Destination = destination[i];
			}
			rcBlocks [this.MoveOrientation].SetAutoPilotEnabled (this.AutopilotEnable);

		}

		public void LookingAt (Vector3D lookAt, double rollAngle, bool relative)
		{
			throw new NotImplementedException ();
		}


		bool IsLookingDir ()
		{
			Vector3D dest = this.LookPoint;

			double rollAngle = this.RollAngle;


			if (applyRotation && !relative && !ship.map.Contains ("lockLook") && localDest.Length () < 20) {
				Log ("Don't rotate, position too close", LOG_VERBOSE);
				return RotateShip (new Vector3D (1, 0, 0), gravity, rollAngle, !applyRotation, out InTheDirection);
			}
			return RotateShip (localDest, gravity, rollAngle, !applyRotation, out InTheDirection);
		}



		private Vector3D CurrentPosition   { 
			get { return block.GetPosition(); } 
		}


		private int MoveOrientation   { 
			get {
				return int.Parse (map.GetValue ("orientation"));
			}
			set {
				map.SetValue ("orientation", value+"");
			}
		}

		private double RollAngle   { 
			get {
				double rollAngle = Utils.DEFAULT_DOUBLE;
				float.TryParse (map.GetValue ("roll"), out rollAngle);
				if (IsGravity() && Math.Abs (rollAngle) > 180) {
					rollAngle = 0;
				}
				return rollAngle;
			}
			set {
				map.SetValue ("roll", value+"");
			}
		}

		private bool LookRelative   { 
			get {
				return bool.Parse (map.GetValue ("relative"));
			}
			set {
				map.SetValue ("relative", value+"");
			}
		}

		private Vector3D LookPoint   { 
			get {
				Vector3D lookPoint;
				Vector3D.TryParse (map.GetValue ("look"), out lookPoint);
				Vector3D localDest = TransformVectorToShipBase (lookPoint - this.VectorPosition);
				if (this.LookRelative) {
					localDest = TransformVectorToShipBase (lookPoint);
				}
				return localDest;
			}
			set {
				map.SetValue ("look", Utils.VectorToString(value));
			}
		}

		private Vector3D Destination   { 
			get {
				Vector3D destination = CurrentPosition;
				Vector3D.TryParse (map.GetValue ("dest"), out destination);
				return destination;
			}
			set {
				map.SetValue ("dest", Utils.VectorToString(value));
			}
		}

		public bool IsArrived ()
		{
			return Vector3D.Distance (Destination, CurrentPosition) < 0.3f;
		}


		bool IsGravity ()
		{
			return rcBlocks [0].GetNaturalGravity ().Length () > 0.1;

		}
	}
}

