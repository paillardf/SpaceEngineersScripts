using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineersScripts;

namespace SpaceEngineersScripts.Autopilot
{
	// tag::content[]

	public class ShipThrusters
	{
		Ship ship;

		List<IMyThrust> allThrusters;

		List<IMyThrust>[] thrustsDir = new List<IMyThrust>[6];

		double[] thrustersPower = new double[6];

		public double speed;

		public ShipThrusters (GridWrapper gridWrapper, Ship ship)
		{
			this.ship = ship;
			allThrusters = gridWrapper.GetBlocks<IMyThrust> ().ConvertAll<IMyThrust>(element => element as IMyThrust);
			for (int i = 0 ; i < thrustsDir.Length; i++){
				thrustsDir[i] = new List<IMyThrust>();
			}

			for (int i = 0; i < allThrusters.Count; i++) {
				BlockWrapper b = new BlockWrapper (allThrusters [i]);
				int orientation = Utils.IndexOfVectorInList (ship.GetOrientationVectors (), b.VectorBackward);

				thrustsDir [orientation].Add(b.block as IMyThrust);
				thrustersPower [orientation] += GetThrusterPower (b.block);

			}

	
		}


		public void MoveShip (bool apply)
		{

			Vector3D dest = ship.Destination;
			Vector3D localVector = ship.TransformVectorToShipBase (dest - ship.VectorPosition);

			shipNeededDeplacement = localVector;
			bool isGravity;
			Vector3D gravity = ship.GetGravity (out isGravity);
			Vector3D localGravity = isGravity ? ship.TransformVectorToShipBase (gravity) : new Vector3D (0, 0, 0);

			double maxSpeed = ship.MaxSpeed;
			speed = Vector3D.Distance (lastLocalVector, localVector) / ship.script.deltaMs * 1000;
			for (int i = 0; i<6; i++) {

				speeds [i] = Math.Pow (-1, i) * (lastLocalVector.GetDim (i / 2) - localVector.GetDim (i / 2)) / ship.script.deltaMs * 1000;

				accelerations [i] = (speeds [i] - lastSpeeds [i]) / ship.script.deltaMs * 1000;

				lastSpeeds [i] = speeds [i];

				List<IMyThrust> thrusters = thrustsDir[i];


				double distance = localVector.GetDim (i / 2) * Math.Pow (-1, i);
				if (distance < 0.2) {

					if(apply)
						StopThrusters (thrusters);
				} else {



					double currentPower = 0;
					double power = thrustersPower [i] + Math.Pow (-1, i) * localGravity.GetDim (i / 2) * ship.Masse;
					for (int b = 0; b<thrusters.Count; b++) {
						currentPower += thrusters[b].ThrustOverride;
					}

					double v = speeds [i];
					double a = accelerations [i];



					double maxAcceleration = power / ship.Masse;
					double timeToStop = v / maxAcceleration;
					double distanceToStop = v * timeToStop + (maxAcceleration * timeToStop * timeToStop) / 2;
					distancesToStop [i / 2] = distanceToStop;

					if (distance < distanceToStop * 2 || v > maxSpeed) {
						ship.Logger.Log ("stop :" + v +"   "+ maxSpeed);

						if (apply) {
							StopThrusters (thrusters);
						}
						lastNeededPower [i] = 0;
					} else {

						double deltaV = maxSpeed - v;

						double neededAcc = Math.Max (0.1, Math.Min (deltaV, distance / 2) / 4);
						double neededPower = neededAcc * ship.Masse - Math.Pow (-1, i) * localGravity.GetDim (i / 2) * ship.Masse;
						neededPower = Math.Max (neededPower, 5000);

						if (v <= 0.1 && lastNeededPower [i] >= neededPower && a < 0) {
							neededPower = lastNeededPower [i] * 1.2;
						}
						lastNeededPower [i] = neededPower;
						if (apply) {
							ApplyOverrideThrusters (thrusters, (float)neededPower);
						}
					}

				}
				if(!apply){
					StopThrusters (thrusters);
				}
			}

			lastLocalVector = localVector;
		}

		public void ApplyOverrideThrusters (List<IMyThrust> thrusters, float value)
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
	


		//------------------------------------------------------------------------
		//--------------------THRUSTER MOVEMENT--------------------
		//------------------------------------------------------------------------
		const int SLOW_ACC_THRUSTER = 10;
		const int FAST_ACC_THRUSTER = 21;

		Vector3D lastLocalVector;
		double[] lastSpeeds = new double[6];
		public Vector3D shipNeededDeplacement;
		double[] speeds = new double[6];
		double[] accelerations = new double[6];
		public double[] distancesToStop = new double[3];
		double[] lastNeededPower = new double[6];




		//Small Thruster	Small Ship	12,110 N
		//	Large Thruster	Small Ship	100,440 N
		//		Small Thruster	Large Ship	145,500 N 
		//		Large Thruster	Large Ship	1,210,000 N








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

		public void StopThrusters (List<IMyThrust> thrusters = null)
		{
			if (thrusters == null) {
				thrusters = this.allThrusters;
			}
			for (int i = 0; i < thrusters.Count; i++) {
				var T = thrusters [i] as IMyThrust;
				T.SetValueFloat ("Override", 0);
				T.GetActionWithName ("DecreaseOverride").Apply (T);
				T.GetActionWithName ("DecreaseOverride").Apply (T);
			}
		}

	
	}
	// end::content[]

}

