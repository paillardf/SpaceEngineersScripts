using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;

namespace SpaceEngineersScripts.Autopilot
{
	// tag::content[]

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

		IMyTextPanel infosScreen;

		AutopilotScript script;

		List<IMyTerminalBlock> gyrosBlocks;

		List<IMyTerminalBlock> connectorBlocks;

		public Ship (AutopilotScript script,IMyRemoteControl block):base(block)
		{
			this.Logger = script.Logger;
			this.GridWrapper = script.GridWrapper;
			this.script = script;
			rcBlocks [IFRONT] = block;
			infosScreen = (IMyTextPanel)GridWrapper.GetBlocksWithName ("PPilot", "can't find PPilot panel") [0];

			orientation = new List<Vector3D>{VectorForward, VectorBackward, VectorLeft, VectorRight, VectorUp, VectorDown};
			var rcs = GridWrapper.GetBlocks<IMyRemoteControl> ();
			for (int i = 0; i < rcs.Count; i++) {
				if (rcs [i].NumberInGrid != block.NumberInGrid) {
					Vector3D forward = new BlockWrapper (rcs [i]).VectorForward;
					int index = Utils.indexOfVectorInList (orientation, new BlockWrapper (rcs [i]).VectorForward);
					if(rcBlocks[index] == null && i>=0){
						rcBlocks [index] = (IMyRemoteControl) rcs [i];
					}
				}
			}

			connectorBlocks = GridWrapper.GetBlocks<IMyShipConnector> ();
			for (int i = 0; i < connectorBlocks.Count; i++) {
				bool connected =((IMyShipConnector)connectorBlocks [i]).IsConnected;
				if (connected) {
					BlockWrapper wrapper = new BlockWrapper (connectorBlocks [i]);
					int index = Utils.indexOfVectorInList (orientation, new BlockWrapper (rcs [i]).VectorForward);
					if(rcBlocks[index] != null && i>=0){
						rcBlocks [index] = (IMyRemoteControl) rcs [i];

					}
				}
			}



			gyrosBlocks = GridWrapper.GetBlocks<IMyGyro> ();

		}



		public bool Update ()
		{
			bool runFast = false;
			runFast = !IsLookingDir ();
			RotateShip ();
			ShowInfos ();
			return runFast && AutopilotEnable;
		}	
		Vector3D shipDeplacement;
		double[] distancesToStop = new double[6];
		


		public void ShowInfos ()
		{

				string t =
					"   | " + Utils.RoundD (shipYawAngle) + "°\n" +
				"   | " + Utils.RoundD (shipDeplacement.GetDim (2)) + "m\n" +
					"   | " + Utils.RoundD (distancesToStop [2]) + "m\n" +
					"   |      /  " + Utils.RoundD (shipRollAngle) + "°\n" +
					"   |    /    " + Utils.RoundD (shipDeplacement.GetDim (0)) + "m\n" +
					"   |  /      " + Utils.RoundD (distancesToStop [0]) + "m\n" +
					"   |/_ _ _ _ _ _ _ _ _ _ \n" +
					"             " + Utils.RoundD (-shipPitchAngle) + "°\n" +
					"             " + Utils.RoundD (-shipDeplacement.GetDim (1)) + "m\n" +
					"             " + Utils.RoundD (distancesToStop [1]) + "m\n" +
					"\n";

			infosScreen.GetProperty ("FontColor").AsColor ().SetValue (infosScreen, AutopilotEnable ? Color.Green : Color.Red);
			LogWrapper.WriteOnScreen (infosScreen, t);


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
			this.LookPoint = lookAt;
			this.RollAngle = RollAngle;
			this.LookRelative = relative;
		}



		bool IsLookingDir ()
		{
			Vector3D localDest = this.LookPoint;
			if (localDest.Equals (Utils.DEFAULT_VECTOR_3D)) {
				
				return true;
			}
			double rotationRoll = this.RollAngle;

			Logger.Log ("localDest" + Utils.VectorToStringRound (localDest));

			Vector3D yawVect = Vector3D.Multiply (localDest, new Vector3D (1, 1, 0));
			yawVect.Normalize ();
			double yawAngle = Math.Acos (yawVect.GetDim (0)) * 180 / Math.PI;
			if (yawVect.GetDim (1) > 0)
				yawAngle = -yawAngle;
			Vector3D pitchVect = Vector3D.Multiply (localDest, new Vector3D (1, 0, 1));
			pitchVect.Normalize ();
			double pitchAngle = Math.Acos (pitchVect.GetDim (0)) * 180 / Math.PI;
			if (pitchVect.GetDim (2) < 0)
				pitchAngle = -pitchAngle;

			bool isGravity;
			var gravityVector = - GetGravity (out isGravity);
			gravityVector.Normalize ();
			Vector3D localUp = TransformVectorToShipBase (gravityVector);

			Vector3D rollVect = Vector3D.Multiply (localUp, new Vector3D (0, 1, 1));
			rollVect.Normalize ();
			double rollAngle = Math.Acos (rollVect.GetDim (2)) * 180 / Math.PI;
			if (rollVect.GetDim (1) > 0)
				rollAngle = -rollAngle;

			speedRollAngle = (shipRollAngle - rollAngle) / script.deltaMs * 1000;
			speedPitchAngle = (shipPitchAngle - pitchAngle) / script.deltaMs * 1000;
			speedYawAngle = (shipYawAngle - yawAngle) / script.deltaMs * 1000;


			bool preciseYawStop = Utils.IsValueSmaller (yawAngle, 0.4);
			bool precisePitchStop = Utils.IsValueSmaller (pitchAngle, 0.4);
			bool preciseRollStop = true;
			if (Utils.IsValueSmaller (rotationRoll, 180)) {
				rollAngle -= rotationRoll;
				rollAngle = (rollAngle + 180) % 360 - 180;
				preciseRollStop = Utils.IsValueSmaller (rollAngle, 0.4);
			} else {
				rollAngle = 0;
			}

			this.shipRollAngle = rollAngle;
			this.shipPitchAngle = pitchAngle;
			this.shipYawAngle = yawAngle;


			bool isLookingDir = preciseYawStop && precisePitchStop && preciseRollStop;
			if (isLookingDir) {
				this.LookPoint = Utils.DEFAULT_VECTOR_3D;
				shipYawAngle = 0;
				shipRollAngle = 0;
				shipPitchAngle = 0;
			}

			return isLookingDir;
		}



		private Vector3D CurrentPosition   { 
			get { return block.GetPosition(); } 
		}


		private int MoveOrientation   { 
			get {
				int moveO = 0;
				 int.TryParse (map.GetValue ("orientation"), out moveO);
				return moveO;
			}
			set {
				map.SetValue ("orientation", value+"");
			}
		}

		private double RollAngle   { 
			get {
				double rollAngle = Utils.DEFAULT_DOUBLE;
				double.TryParse (map.GetValue ("roll"), out rollAngle);
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
				bool relative = false;
				return bool.TryParse (map.GetValue ("relative"), out relative);
			}
			set {
				map.SetValue ("relative", value+"");
			}
		}

		private Vector3D LookPoint   { 
			get {
				Vector3D lookPoint = Utils.CastString<Vector3D> (map.GetValue ("look"));
				if (lookPoint.Equals (Utils.DEFAULT_VECTOR_3D))
					return lookPoint;
				if (this.LookRelative) {
					lookPoint = TransformVectorToShipBase (lookPoint);
				}
				return lookPoint;
			}
			set {
				map.SetValue ("look", Utils.VectorToString(value));
			}
		}

		private Vector3D Destination   { 
			get {
				Vector3D destination = Utils.CastString<Vector3D> (map.GetValue ("dest"));
				if (destination.Equals (Utils.DEFAULT_VECTOR_3D)) {
					destination = CurrentPosition;
				}
				return destination;
			}
			set {
				map.SetValue ("dest", Utils.VectorToString(value));
			}
		}

		public bool PreciseMode {
			get {
				bool precise = false;
				bool.TryParse (map.GetValue ("precise"), out precise);
				return precise;
			}
			set{
				map.SetValue ("precise", ""+value);
			}
		}

		public bool IsArrived ()
		{
			return Vector3D.Distance (Destination, CurrentPosition) < 0.3f;
		}


		bool IsGravity ()
		{
			bool isGravity;
			GetGravity (out isGravity);
			return isGravity;

		}

		Vector3D GetGravity (out bool isGravity)
		{
			Vector3D gravity = this.rcBlocks[0].GetNaturalGravity ();
			isGravity = gravity.Length () > 0.1;
			if (!isGravity)
				gravity = new Vector3D (0, 0, -1);
			return gravity;
		}

		//------------------------------------------------------------------------
		//--------------------GYRO MOVEMENT--------------------
		//------------------------------------------------------------------------

		const string YAW = "Yaw";
		const string PITCH = "Pitch";
		const string ROLL = "Roll";

		double shipRollAngle = 0;
		double shipYawAngle = 0;
		double shipPitchAngle = 0;
		double speedRollAngle = 0, speedYawAngle = 0, speedPitchAngle = 0;
		float lastSpeedRollAngle = 0, lastSpeedYawAngle = 0, lastSpeedPitchAngle = 0;

		public void RotateShip ()
		{	

			lastSpeedYawAngle = GetGyroSpeed (shipYawAngle, speedYawAngle, lastSpeedYawAngle);
			ApplyOverride (gyrosBlocks, YAW, lastSpeedYawAngle);

			lastSpeedPitchAngle = GetGyroSpeed (shipPitchAngle, speedPitchAngle, lastSpeedPitchAngle);
			ApplyOverride (gyrosBlocks, PITCH, lastSpeedPitchAngle);

			lastSpeedRollAngle = GetGyroSpeed (shipRollAngle, speedRollAngle, lastSpeedRollAngle);
			ApplyOverride (gyrosBlocks, ROLL, lastSpeedRollAngle);

		}

		//GPS:NAME:X:Y:Z: 
		public float GetGyroSpeed (double angle, double currentSpeed, double lastValue) 
		{  
			int sign = angle < 0 ? -1 : 1;  

			if (Utils.IsValueSmaller(angle,0.03)) { 
				return 0; 
			} 

			double value = (angle) / 100 + 0.2/(angle+1*sign);  
			if (Utils.IsValueSmaller (currentSpeed, 0.3) && !Utils.IsValueSmaller (lastValue, value)) {  
				value = lastValue + sign * 0.02;  
				} else if (!Utils.IsValueSmaller (currentSpeed, 10+angle/3)) {  

				value =value/3;  

			}  
			return (float)value;  

		} 
		public void ApplyOverride (List<IMyTerminalBlock> blocks, string action, float value)
		{
			for (int i = 0; i< blocks.Count; i++) {
				var block = blocks [i];
				block.SetValue (action, value);
			}
		}


	}
	// end::content[]

}

