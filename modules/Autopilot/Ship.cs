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

		IMyRemoteControl[] rcBlocks = new IMyRemoteControl[6];

		IMyTextPanel infosScreen;

		public AutopilotScript script;

		IMyShipConnector connector;

		ShipGyros gyros;
		ShipThrusters thrusters;

		Ship neastedShip;

		public Ship (AutopilotScript script,IMyTerminalBlock block):base(block)
		{
			this.Logger = script.Logger;
			this.GridWrapper = script.GridWrapper;
			this.script = script;
			if(block is IMyRemoteControl)
				rcBlocks [IFRONT] = block as IMyRemoteControl;
			infosScreen = (IMyTextPanel)GridWrapper.GetBlocksWithName ("PPilot", "can't find PPilot panel") [0];

			gyros = new ShipGyros (GridWrapper, this);
			thrusters = new ShipThrusters (GridWrapper, this);

			Vector3D[] orientations = GetOrientationVectors ();
			var rcs = GridWrapper.GetBlocks<IMyRemoteControl> ();
			for (int i = 0; i < rcs.Count; i++) {
				if (rcs [i].NumberInGrid != block.NumberInGrid) {
					Vector3D forward = new BlockWrapper (rcs [i]).VectorForward;
					int index = Utils.IndexOfVectorInList (orientations, new BlockWrapper (rcs [i]).VectorForward);
					if(rcBlocks[index] == null && i>=0){
						rcBlocks [index] = (IMyRemoteControl) rcs [i];
					}
				}
			}

			var connectors = GridWrapper.GetBlocks<IMyShipConnector> ();
			if (connectors.Count > 0) {
				connector = (IMyShipConnector)( connectors[0]);
			}



		}

		public Vector3D[] GetOrientationVectors(){
			return new Vector3D[6]{VectorForward, VectorBackward, VectorLeft, VectorRight, VectorUp, VectorDown};
		}

		public bool Update ()
		{
			if (neastedShip != null) {
				neastedShip.Update ();
				if (neastedShip.Arrived) {
					if (neastedShip.block == connector) {
						connector.ApplyAction("SwitchLock");
					}
					neastedShip = null;
				}
				return true;
			}


			bool autoControl = rcBlocks [this.MoveOrientation].IsUnderControl;

			bool gyroUpdate = !gyros.CalculateLookingDir ();
			if (gyroUpdate && AutopilotEnable && !autoControl) {
				gyros.RotateShip ();
			}

			bool moveShip = !Arrived;
			thrusters.MoveShip (moveShip);
			ShowInfos ();
			return (gyroUpdate || moveShip) && AutopilotEnable && !autoControl;// && IsArrived();
		}	
		double[] distancesToStop = new double[6];
		


		public void ShowInfos ()
		{

				string t =
				"   | " + Utils.RoundD (gyros.shipYawAngle) + "°\n" +
				"   | " + Utils.RoundD (thrusters.shipNeededDeplacement.GetDim (2)) + "m\n" +
					"   | " + Utils.RoundD (distancesToStop [2]) + "m\n" +
				"   |      /  " + Utils.RoundD (gyros.shipRollAngle) + "°\n" +
				"   |    /    " + Utils.RoundD (thrusters.shipNeededDeplacement.GetDim (0)) + "m\n" +
					"   |  /      " + Utils.RoundD (distancesToStop [0]) + "m\n" +
					"   |/_ _ _ _ _ _ _ _ _ _ \n" +
				"             " + Utils.RoundD (-gyros.shipPitchAngle) + "°\n" +
				"             " + Utils.RoundD (-thrusters.shipNeededDeplacement.GetDim (1)) + "m\n" +
					"             " + Utils.RoundD (distancesToStop [1]) + "m\n" +
					"\n";

			infosScreen.GetProperty ("FontColor").AsColor ().SetValue (infosScreen, AutopilotEnable ? Color.Green : Color.Red);
			LogWrapper.WriteOnScreen (infosScreen, t);


		}

		Vector3D dockingPosition = Utils.DEFAULT_VECTOR_3D;

		public void UnDock(){
			if (connector == null) {
				Logger.Log ("Can't dock without any connector");
				return;
			}

			neastedShip = new Ship (script, connector);
				
			neastedShip.LookingAt (neastedShip.VectorForward, neastedShip.gyros.shipRollAngle, true);
			connector.ApplyAction("OnOff_Off");
			MoveTo (neastedShip.VectorPosition+ neastedShip.VectorForward * 8);
		}


		public void Dock(){
			if (connector == null) {
				Logger.Log ("Can't dock without any connector");
				return;
			}

			IMyShipConnector stationConnector = null;
			var sensors = GridWrapper.GetBlocks<IMySensorBlock> ();
			for (int i = 0; i < sensors.Count; i++) {
				var detected = ((IMySensorBlock)sensors [i]).LastDetectedEntity;
				if (detected != null && detected is IMyCubeGrid)  {
					CubeGridWrapper wrapper = new CubeGridWrapper (detected as IMyCubeGrid, connector);
					stationConnector = wrapper.GetNearest<IMyShipConnector> ();
					if (stationConnector != null) {
						break;
					}
				}

			}

			if (stationConnector == null) {
				Logger.Log("No station connector detected");
				return;
			}

			BlockWrapper stationConnectorWrapper = new BlockWrapper (stationConnector);
		
			Vector3D dockingPosition = stationConnectorWrapper.VectorPosition+stationConnectorWrapper.VectorForward;

			neastedShip = new Ship (script, connector);
			neastedShip.MaxSpeed = 5;
			connector.ApplyAction("OnOff_On");
			neastedShip.LookingAt (dockingPosition, -1000, false);
			neastedShip.MoveTo (dockingPosition);

		}




		public bool AutopilotEnable
		{
			get{
				return gyros.Override;
			}
			set{
				gyros.Override = value;

			}
		}

		public void MoveTo(Vector3D destination){
			this.Arrived = false;
			this.Destination = destination;
		}

		public void TravelTo(Vector3D[] destination, int sens = IFRONT){
			IMyRemoteControl controlBlock = rcBlocks [sens];
			if (controlBlock == null) {
				Logger.Log("can't move no remoteControl block available in this direction");
				return;
			}
			rcBlocks [this.MoveOrientation].SetAutoPilotEnabled (false);
			controlBlock.ClearWaypoints ();
			this.MoveOrientation = sens;
			Vector3D finalDest = VectorPosition;
			for (int i = 0; i < destination.Length; i++) {
				controlBlock.AddWaypoint (destination[i], Utils.VectorToStringRound(destination[i]));
				finalDest = destination[i];
			}
			MoveTo (finalDest);
			rcBlocks [this.MoveOrientation].SetAutoPilotEnabled (this.AutopilotEnable);

		}

		public void LookingAt (Vector3D lookAt, double rollAngle, bool relative)
		{
			this.LookPoint = lookAt;
			this.RollAngle = rollAngle;
			this.LookRelative = relative;
		}




		public bool LookingDir 
		{
			get {
				bool lookingDir = false;
				return bool.TryParse (map.GetValue ("lookingDir"), out lookingDir);
			}
			set {
				map.SetValue ("lookingDir", value+"");
			}
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

		public double RollAngle   { 
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

		public Vector3D LookPoint   { 
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

		public double MaxSpeed {
			get {
				double speed = 300;
				double.TryParse (map.GetValue ("speed"), out speed);
				return speed;
			}
			set{
				map.SetValue ("speed", ""+value);
			}
		}

		public double Masse {
			get{ return 40000; }
			set{ }
		}

		public bool Arrived
		{
			get{
				bool arrived = Vector3D.Distance (Destination, CurrentPosition) < 0.4f;
				bool wasArrive = true;
				bool.TryParse (map.GetValue ("arrived"), out wasArrive);
				this.Arrived = wasArrive || arrived; 
				return wasArrive || arrived;
			}
			set{
				neastedShip = null;
				map.SetValue ("arrived", ""+value);
			}

		}


		bool IsGravity ()
		{
			bool isGravity;
			GetGravity (out isGravity);
			return isGravity;

		}

		public Vector3D GetGravity (out bool isGravity)
		{
			Vector3D gravity = this.rcBlocks[0].GetNaturalGravity ();
			isGravity = gravity.Length () > 0.1;
			if (!isGravity)
				gravity = new Vector3D (0, 0, -1);
			return gravity;
		}



	}
	// end::content[]

}

