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
		public AutopilotScript(IMyGridTerminalSystem GridTerminalSystem, IMyTerminalBlock Me, LogDelegate Echo):base (GridTerminalSystem, Me, Echo){
			Initialize ();
		}

		public override void Update (string argument)
		{
			if (Initialize ()) {
				TraitArgument (argument);
				UpdateMovement ();
				ShowInfos ();
				ship.Save ();
			}
		}

		private void MoveShip (Vector3D localVector)
		{
			shipDeplacement = localVector;
			bool isGravity;
			Vector3D gravity = GetGravity (out isGravity);
			Vector3D localGravity = isGravity ? TransformVectorToShipBase (gravity) : new Vector3D (0, 0, 0);

			double speed = Vector3D.Distance (lastLocalVector, localVector) / deltaMs * 1000;
			for (int i = 0; i<6; i++) {

				speeds [i] = Math.Pow (-1, i) * (lastLocalVector.GetDim (i / 2) - localVector.GetDim (i / 2)) / deltaMs * 1000;
				accelerations [i] = (speeds [i] - lastSpeeds [i]) / deltaMs * 1000;
				lastSpeeds [i] = speeds [i];
				List<IMyTerminalBlock> thrusters = GetBlocksWithName (TNAME [i]);


				double distance = localVector.GetDim (i / 2) * Math.Pow (-1, i);
					double power = thrustersPower [i] + Math.Pow (-1, i) * localGravity.GetDim (i / 2) * shipMasse;
					double v = speeds [i];
					double a = accelerations [i];


					double maxAcceleration = power / shipMasse;
					double timeToStop = v / maxAcceleration;
					double distanceToStop = v * timeToStop + (maxAcceleration * timeToStop * timeToStop) / 2;
					distancesToStop [i / 2] = distanceToStop;

			}

			lastLocalVector = localVector;
		}

		//------------------------------------------------------------------------
		//--------------------SHIP PARAMS --------------------
		//------------------------------------------------------------------------

		string infosScreen = "PPilot";
		string logScreenName = "PLogPilot";
		string shipRemoteControlBlockName; //needed if no cockpit
		double shipMasse = 35000;


		//------------------------------------------------------------------------
		//--------------------COMMAND --------------------
		//------------------------------------------------------------------------
		//   on (start autopilot)
		//   off (stop autopilot)


		bool autopilotEnable = false;

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
				List<string> functionArgs = ExtractFunctionParameters (arg, out functionName);
				if (functionName.StartsWith ("go") || functionName.StartsWith ("goDelta")) {

					var destination = DEFAULT_VECTOR_3D;
					if (functionArgs.Count > 0)
						destination = CastString<Vector3D> (functionArgs [0]);



					if (functionName.EndsWith ("Delta")) {
						destination = ship.VectorPosition + ship.VectorForward * destination.GetDim (0) + ship.VectorLeft * destination.GetDim (1) + ship.VectorUp * destination.GetDim (2);
					}

					SetDestination (destination);


				} else if (functionName.StartsWith ("lookAt") || functionName.StartsWith ("lookDir")) {

					double rollAngle = 10000;
					var destination = CastString<Vector3D> (functionArgs [0]);
					if (functionArgs.Count > 1) {
						rollAngle = CastString<double> (functionArgs [1]);
					}

					SetLookingDir (destination, rollAngle, functionName.StartsWith ("lookDir"), false);

				} else if (functionName.StartsWith ("maxSpeed")) {
					if (functionArgs.Count > 0) {

						double maxVal = CastString<double> (functionArgs [0]);

						ship.map.SetValue ("maxSpeed", maxVal + "");
					} else {
						ship.map.Remove ("maxSpeed");

					}

				} else if (functionName.StartsWith ("lockLook")) {
					bool v = false;
					if (functionArgs.Count > 0) {
						v = CastString<bool> (functionArgs [0]);
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

		public void ShowInfos ()
		{

			if (infosScreen.Length > 0) {
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

				var panel = (IMyTextPanel)GetBlocksWithName (infosScreen, "Can't find panel " + infosScreen) [0];
				panel.GetProperty ("FontColor").AsColor ().SetValue (panel, autopilotEnable ? Color.Green : Color.Red);
				WriteOnScreen (panel, t);

			}
		}

		//------------------------------------------------------------------------
		//--------------------MOVEMENT--------------------
		//------------------------------------------------------------------------


		public void UpdateMovement ()
		{

			bool InTheDirection;
			bool lookingDir = IsLookingDir (autopilotEnable, out InTheDirection);
			bool isArrived = IsArrived ();
			ship.map.SetValue ("IsLookingDir", "" + lookingDir);
			ship.map.SetValue ("IsArrived", "" + isArrived);
			bool lockDir = ship.map.Contains ("lockLook");

			Vector3D dest;
			Vector3D.TryParse (ship.map.GetValue ("dest"), out dest);
			Vector3D localDest = TransformVectorToShipBase (dest - ship.VectorPosition);
			MoveShip (localDest);


		}

		public bool IsArrived ()
		{
			if (ship.map.Contains ("dest")) {
				Vector3D dest;
				Vector3D.TryParse (ship.map.GetValue ("dest"), out dest);
				Vector3D localDest = TransformVectorToShipBase (dest - ship.VectorPosition);
				shipDistanceTarget = localDest.Length ();
				double distTarg = 0.5;
				if (shipDistanceTarget < distTarg) {
					return true;
				}
			} else {
				return true;
			}
			return false;
		}

		public void SetDestination (Vector3D destination)
		{
			if (destination.Equals (DEFAULT_VECTOR_3D)) {
				ship.map.Remove ("dest");
				if (!ship.map.Contains ("lockLook")) {
					SetLookingDir (ship.VectorPosition + ship.VectorForward, 100000, false, true);
				}
			} else {
				ship.map.SetValue ("dest", destination.ToString ());
				if (!ship.map.Contains ("lockLook")) {
					SetLookingDir (destination, 100000, false, true);
				}
			}
		}

		void SetLookingDir (Vector3D destination, double rollAngle, bool isRelative, bool isGotoDir)
		{
			bool isGravity;
			GetGravity (out isGravity);
			if (isGravity && isGotoDir && !isRelative) {
				isRelative = true;
				destination = destination - ship.VectorPosition;
			}
			ship.map.SetValue ("rollAngle", "" + rollAngle);
			ship.map.SetValue ("look", destination.ToString ());
			ship.map.SetValue ("relative", "" + isRelative);
		}

		public void LockLook (bool lockLook)
		{
			if (lockLook)
				ship.map.SetValue ("lockLook", "true");
			else if (ship.map.Contains ("lockLook")) {
				ship.map.Remove ("lockLook");
			}
		}

		Vector3D GetGravity (out bool isGravity)
		{
			Vector3D gravity = GetBlock<IMyRemoteControl> ().GetNaturalGravity ();
			isGravity = gravity.Length () > 0.1;

			if (!isGravity)
				gravity = new Vector3D (0, 0, -1);
			return gravity;

		}

		bool IsLookingDir (bool applyRotation, out bool  InTheDirection)
		{
			Vector3D dest = getLookingDir ();

			bool isGravity;
			Vector3D gravity = GetGravity (out isGravity);

			bool relative = false;
			if (ship.map.Contains ("relative")) {
				relative = bool.Parse (ship.map.GetValue ("relative"));
			}
			Vector3D localDest = TransformVectorToShipBase (dest - ship.VectorPosition);
			if (relative) {
				localDest = TransformVectorToShipBase (dest);
			}
			double rollAngle = 2000;
			if (ship.map.Contains ("rollAngle")) {
				rollAngle = double.Parse (ship.map.GetValue ("rollAngle"));
			} 

			if (isGravity && Math.Abs (rollAngle) > 180) {
				rollAngle = 0;
			}

			if (applyRotation && !relative && !ship.map.Contains ("lockLook") && localDest.Length () < 20) {
				Log ("Don't rotate, position too close", LOG_VERBOSE);
				return RotateShip (new Vector3D (1, 0, 0), gravity, rollAngle, !applyRotation, out InTheDirection);
			}
			return RotateShip (localDest, gravity, rollAngle, !applyRotation, out InTheDirection);
		}

		public Vector3D getLookingDir ()
		{
			if (ship.map.Contains ("look")) {
				Vector3D dir;
				Vector3D.TryParse (ship.map.GetValue ("look"), out dir);
				if (ship.map.Contains ("relative")) {
					return dir;
				}
				return dir;
			}
			return default(Vector3D);
		}

		Vector3D TransformVectorToShipBase (Vector3D vect)
		{
			Vector3D forward = ship.VectorForward;
			Vector3D left = ship.VectorLeft;
			Vector3D up = ship.VectorUp;
			MatrixD P = new MatrixD (forward.GetDim (0), forward.GetDim (1), forward.GetDim (2),
				left.GetDim (0), left.GetDim (1), left.GetDim (2),
				up.GetDim (0), up.GetDim (1), up.GetDim (2));
			MatrixD Pinv = MatrixD.Invert (P);
			return Vector3D.Transform (vect, Pinv);
		}

		const int DISTANCE_PRECISION = 10;
		const string YAW = "Yaw";
		const string PITCH = "Pitch";
		const string ROLL = "Roll";
		double shipRollAngle = 0;
		double shipYawAngle = 0;
		double shipPitchAngle = 0;
		double shipDistanceTarget = 0;
		double speedRollAngle = 0, speedYawAngle = 0, speedPitchAngle = 0;
		float lastSpeedRollAngle = 0, lastSpeedYawAngle = 0, lastSpeedPitchAngle = 0;

		public void ApplyOverrideThrusters (List<IMyTerminalBlock> thrusters, float value)
		{
			float v = 0;
			for (int i = 0; i< thrusters.Count; i++) {
				var prop = thrusters [i].GetProperty ("Override").AsFloat ();
				float max = (float)GetThrusterPower (thrusters [i]);
				float rapport = prop.GetMaximum (thrusters [i]) / max;
				float newVal = 0;
				if (value - v > max) {
					newVal = max;
				} else if (value > v) {
					newVal = value - v;
				}
				prop.SetValue (thrusters [i], newVal * rapport);
				v += newVal;
			}
		}

		public void ApplyOverride (List<IMyTerminalBlock> blocks, string action, float value)
		{
			for (int i = 0; i< blocks.Count; i++) {
				var block = blocks [i];
				block.SetValue (action, value);
			}
		}


		//------------------------------------------------------------------------
		//--------------------GYRO MOVEMENT--------------------
		//------------------------------------------------------------------------

		public void setGyrosOverride (bool overrideGyros)
		{
			var gyros = GetBlocks<IMyGyro> ();
			for (int i = 0; i<gyros.Count; i++) {
				gyros [i].GetProperty ("Override").AsBool ().SetValue (gyros [i], overrideGyros);
			}
		}

		public bool RotateShip (Vector3D localDest, Vector3D gravityVector, double rotationRoll, bool checkAlign, out bool inTheDirection)
		{	
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

			gravityVector = -gravityVector;
			gravityVector.Normalize ();
			Vector3D localUp = TransformVectorToShipBase (gravityVector);

			Vector3D rollVect = Vector3D.Multiply (localUp, new Vector3D (0, 1, 1));
			rollVect.Normalize ();
			double rollAngle = Math.Acos (rollVect.GetDim (2)) * 180 / Math.PI;
			if (rollVect.GetDim (1) > 0)
				rollAngle = -rollAngle;

			speedRollAngle = (shipRollAngle - rollAngle) / deltaMs * 1000;
			speedPitchAngle = (shipPitchAngle - pitchAngle) / deltaMs * 1000;
			speedYawAngle = (shipYawAngle - yawAngle) / deltaMs * 1000;

			this.shipRollAngle = rollAngle;
			this.shipPitchAngle = pitchAngle;
			this.shipYawAngle = yawAngle;

			ship.map.SetValue ("pitch", shipPitchAngle + "");
			ship.map.SetValue ("yaw", shipYawAngle + "");
			ship.map.SetValue ("roll", shipRollAngle + "");


			bool yawStop = IsValueSmaller (yawAngle, 4);
			bool pitchStop = IsValueSmaller (pitchAngle, 4);
			//bool rollStop = IsValueSmaller (rollAngle, 2);

			inTheDirection = yawStop && pitchStop;


			bool preciseYawStop = IsValueSmaller (yawAngle, 0.4);
			bool precisePitchStop = IsValueSmaller (pitchAngle, 0.4);
			bool preciseRollStop = true;

			bool isLookingDir = preciseYawStop && precisePitchStop && preciseRollStop;
			return isLookingDir;
		}

		//GPS:NAME:X:Y:Z: 
		public float GetGyroSpeed (double angle, double currentSpeed, double lastValue) 
		{  
			int sign = angle < 0 ? -1 : 1;  

			if (IsValueSmaller(angle,0.03)) { 
				return 0; 
			} 

			double value = (angle) / 100 + 0.2/(angle+1*sign);  
			if (IsValueSmaller (currentSpeed, 0.3) && !IsValueSmaller (lastValue, value)) {  
				value = lastValue + sign * 0.02;  
			} else if (!IsValueSmaller (currentSpeed, 10+angle/3)) {  

				value =value/3;  

			}  
			return (float)value;  
		} 

		public bool IsValueSmaller (double value, double objValue)
		{
			return Math.Abs (value) < Math.Abs (objValue);
		}


		//------------------------------------------------------------------------
		//--------------------THRUSTER MOVEMENT--------------------
		//------------------------------------------------------------------------

		const int IFRONT = 0;
		const int IBACK = 1;
		const int ILEFT = 2;
		const int IRIGHT = 3;
		const int ITOP = 4;
		const int IBOTTOM = 5;
		const int SLOW_ACC_THRUSTER = 10;
		const int FAST_ACC_THRUSTER = 21;
		Vector3D lastLocalVector;
		double[] lastSpeeds = new double[6];
		Vector3D shipDeplacement;
		double[] speeds = new double[6];
		double[] accelerations = new double[6];
		double[] distancesToStop = new double[3];
		double[] lastNeededPower = new double[6];




		//Small Thruster	Small Ship	12,110 N
		//	Large Thruster	Small Ship	100,440 N
		//		Small Thruster	Large Ship	145,500 N 
		//		Large Thruster	Large Ship	1,210,000 N





		double[] thrustersPower;

		string[] FTT = {
			"Forward",
			"En avant",
			"Vpřed",
			"Vorwärts",
			"Adelante",
			"Avanti",
			"Voorwaards",
			"Do przodu",
			"Para frente"
		};
		string[] BKT = {
			"Backward",
			"En arrière",
			"Zpět",
			"Rückwärts",
			"Hacia atrás",
			"Indietro",
			"Achteruit",
			"Do tyłu",
			"Para trás"
		};
		string[] LTT = {
			"Left",
			"Gauche",
			"Vlevo",
			"Links",
			"Izquierda",
			"Sinistra",
			"Links",
			"W lewo",
			"Esquerda"
		};
		string[] RTT = {
			"Right",
			"Droite",
			"Vpravo",
			"Rechts",
			"Derecha",
			"Diestra",
			"Rechts",
			"W prawo",
			"Direita"
		};
		string[] TPT = {
			"Up",
			"Haut",
			"Nahoru",
			"Aufwärts",
			"Arriba",
			"Sù",
			"Omhoog",
			"W górę",
			"Acima"
		};
		string[] BMT = {
			"Down",
			"Bas",
			"Dolů",
			"Abwärts",
			"Abajo",
			"Giù",
			"Omlaag",
			"W dół",
			"Abaixo"
		};

		public bool resetThrusters ()
		{

			List<IMyTerminalBlock> T = new List<IMyTerminalBlock> ();
			GridTerminalSystem.GetBlocksOfType<IMyThrust> (T);
			thrustersPower = new double[6];
			for (int i=0; i<T.Count; i++) {
				string n = T [i].CustomName;
				int indexName = -1;

					if (n.Split (new char[] {
						'(',
						')'
					}).Length < 2) {
						Log ("can't initialize please rename Thruster " + n);
						return false;
					} 
					if (0 <= IndexOfName (FTT, n.Split (new char[] {
						'(',
						')'
					}) [1]))
						indexName = IFRONT;
					else if (0 <= IndexOfName (BKT, n.Split (new char[] {
						'(',
						')'
					}) [1]))
						indexName = IBACK;
					else if (0 <= IndexOfName (LTT, n.Split (new char[] {
						'(',
						')'
					}) [1]))
						indexName = ILEFT;
					else if (0 <= IndexOfName (RTT, n.Split (new char[] {
						'(',
						')'
					}) [1]))
						indexName = IRIGHT;
					else if (0 <= IndexOfName (TPT, n.Split (new char[] {
						'(',
						')'
					}) [1]))
						indexName = ITOP;
					else if (0 <= IndexOfName (BMT, n.Split (new char[] {
						'(',
						')'
					}) [1]))
						indexName = IBOTTOM;

				thrustersPower [indexName] += GetThrusterPower (T [i]);


				}
			return true;
		}

		public double GetThrusterPower (IMyTerminalBlock block)
		{
			String Definition = block.BlockDefinition.ToString ();
			String[] DefinitionFragments = Definition.Split ('/');	
			// MyObjectBuilder_MyProgrammableBlock/LargeProgrammableBlock
			// Get the position of the word "Block"
			int BlockStrPos = DefinitionFragments [1].IndexOf ("Block");
			// Get our size (before block)
			String BlockSize = DefinitionFragments [1].Substring (0, BlockStrPos);
			// Get our subtype
			String SubType = DefinitionFragments [1].Substring (BlockStrPos + 5);

			String BlockType = DefinitionFragments [0].Substring (
				DefinitionFragments [0].IndexOf ("_") + 1);

			bool largeGrid = BlockSize.Contains ("Large");
			bool largeThruster = SubType.Contains ("Large");

			if (BlockType.Contains ("Hydrogen")) {
				if (largeThruster)
					return largeGrid ? 6000000 : 400000;
				else
					return largeGrid ? 900000 : 82000;
			} else if (BlockType.Contains ("Atmospheric")) {
				if (largeThruster)
					return largeGrid ? 5400000 : 408000;
				else
					return largeGrid ? 420000 : 80000;
			} else {
				if (largeThruster)
					return largeGrid ? 3600000 : 144000;
				else
					return largeGrid ? 288000 : 12000;
			}
		}

		public int IndexOfName (string[] a, string val)
		{
			for (int i = 0; i < a.Length; i ++) {
				if (val.StartsWith (a [i]))
					return i;
			}
			return -1;
		}





		public static List<string> Regex (string Pattern, string t)
		{
			string text = t.Trim ();
			System.Text.RegularExpressions.Match m = (new System.Text.RegularExpressions.Regex (Pattern)).Match (text);
			List<string> Matches = new List<string> ();
			while (m.Success) {
				if (m.Groups.Count > 1) {
					Matches.Add (m.Groups [1].Value);
				}		
				m = m.NextMatch ();
			}

			return Matches;
		}

		public static List<string> RegexGroup (string Pattern, string t)
		{
			string text = t.Trim ();
			System.Text.RegularExpressions.Match m = (new System.Text.RegularExpressions.Regex (Pattern)).Match (text);
			List<string> result = new List<string> ();
			if (m.Success) {
				for (int i =1; i<m.Groups.Count; i++) {
					result.Add (m.Groups [i].Value);
				}
			}
			return result;
		}

		string FetchLine (string key, string details)
		{
			string[] comm = details.Split ('\n');
			for (int a = 0; a < comm.Length; a++) {
				if (comm [a].StartsWith (key))
					return comm [a].Split (':') [1].Trim ();
			}
			Log ("FetchLine fail: " + key + " not found in " + details); 
			return null;
		}

		List<IMyTerminalBlock> GetBlocks<T> ()  where T : IMyTerminalBlock
		{
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock> ();
			GridTerminalSystem.GetBlocksOfType<T> (blocks);
			return blocks;
		}

		List<IMyTerminalBlock> GetBlocksWithName (string name, string error = "")
		{
			List<IMyTerminalBlock> result = new List<IMyTerminalBlock> ();
			List<IMyBlockGroup> groups = new List<IMyBlockGroup> ();
			GridTerminalSystem.GetBlockGroups (groups);
			for (int i = 0; i < groups.Count; i++) {
				var group = groups [i];
				if (group.Name.StartsWith (name)) {
					result.AddList (groups [i].Blocks);
				}
			}
			if (result.Count != 0) {
				return result;
			}
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock> ();
			GridTerminalSystem.GetBlocks (blocks);
			for (int i = 0; i < blocks.Count; i ++) {
				var block = blocks [i];
				if (block.CustomName.StartsWith (name)) {
					result.Add (block);
				}
			}
			if (result.Count == 0 && error.Length > 0) {
				Log (error);
			}
			return result;

		}

		T GetBlock<T> (string error = "") where T : IMyTerminalBlock
		{
			List<IMyTerminalBlock> blocks = GetBlocks<T> ();
			if (blocks.Count == 0) {
				if (error == null || error.Length == 0)
					Echo ("Impossible de trouver le block de type " + typeof(T).Name);
				else
					Echo (error);
				return default(T);
			} else {
				return (T)blocks [0];
			}

		}

		public T GetNearest<T> (IMyTerminalBlock from = null, string error="") where T: IMyTerminalBlock
		{
			if (from == null)
				from = Me;
			List<IMyTerminalBlock> blocks = GetBlocks<T> ();
			T best = default(T);
			if (blocks.Count > 0) {
				best = (T)blocks [0];
			}
			for (int a = 1; a < blocks.Count; a++) {
				IMyTerminalBlock b = blocks [a];
				if (Vector3D.Distance (from.GetPosition (), b.GetPosition ()) < Vector3D.Distance (from.GetPosition (), best.GetPosition ()))
					best = (T)blocks [a];
			}
			if (best == null) {
				if (error == null || error.Length == 0)
					Log ("Can't find closest block of type " + typeof(T).Name);
				else
					Log (error);
			}
			return best;
		}

		string log = "";
		IMyTextPanel logScreenBlock = null;

		public void Log (string text, int level = 1)
		{
			if (level > logLevel)
				return;
			Echo (text);
			if (logScreenName == null || logScreenName.Length > 0) {
				if (log.Split ('\n').Length > 10)
					log = "";
				log += " " + text + "\n";

				if (logScreenBlock == null) {
					if (logScreenName != null) {
						logScreenBlock = (IMyTextPanel)GetBlocksWithName (logScreenName, error: "Can't find panel with name " + logScreenName) [0];
					} else {
						logScreenBlock = GetNearest<IMyTextPanel> (from: Me, error: "Attempted to find closest text panel block, but there are NO TEXT PANELS!");
					}
				}

				if (logScreenBlock != null) {
					WriteOnScreen (logScreenBlock, log);
				}
			}
		}

		void WriteOnScreen (IMyTextPanel panel, string txt)
		{
			panel.WritePublicText (txt);
			panel.ShowTextureOnScreen ();
			panel.ShowPublicTextOnScreen ();


		}



		BlockWrapper ship = null;
		long clockStart = 0;
		long deltaMs = 0;
		long deltaMsUpdate = 0;
		static double DEFAULT_DOUBLE = -999999999;
		static Vector3D DEFAULT_VECTOR_3D = new Vector3D (DEFAULT_DOUBLE, DEFAULT_DOUBLE, DEFAULT_DOUBLE);

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

