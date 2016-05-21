using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;
using Sandbox.ModAPI.Interfaces;

namespace SpaceEngineersScripts.Autopilot
{
	// tag::content[]

	public class ShipGyros
	{
		List<IMyTerminalBlock> gyrosBlocks;

		Ship ship;

		public ShipGyros (GridWrapper gridWrapper, Ship ship)
		{
			this.ship = ship;
			gyrosBlocks = gridWrapper.GetBlocks<IMyGyro> ();

		}


		//------------------------------------------------------------------------
		//--------------------GYRO MOVEMENT--------------------
		//------------------------------------------------------------------------

		const string YAW = "Yaw";
		const string PITCH = "Pitch";
		const string ROLL = "Roll";

		public double shipRollAngle = 0;
		public double shipYawAngle = 0;
		public double shipPitchAngle = 0;
		public double speedRollAngle = 0, speedYawAngle = 0, speedPitchAngle = 0;
		public float lastSpeedRollAngle = 0, lastSpeedYawAngle = 0, lastSpeedPitchAngle = 0;


		public bool CalculateLookingDir (){
			Vector3D localDest = ship.LookPoint;
			if (localDest.Equals (Utils.DEFAULT_VECTOR_3D)) {
				ship.LookingDir = true;
				return true;
			}
			double rotationRoll = ship.RollAngle;

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
			var gravityVector = - ship.GetGravity (out isGravity);
			gravityVector.Normalize ();
			Vector3D localUp = ship.TransformVectorToShipBase (gravityVector);

			Vector3D rollVect = Vector3D.Multiply (localUp, new Vector3D (0, 1, 1));
			rollVect.Normalize ();
			double rollAngle = Math.Acos (rollVect.GetDim (2)) * 180 / Math.PI;
			if (rollVect.GetDim (1) > 0)
				rollAngle = -rollAngle;

			speedRollAngle = (shipRollAngle - rollAngle) / ship.script.deltaMs * 1000;
			speedPitchAngle = (shipPitchAngle - pitchAngle) / ship.script.deltaMs * 1000;
			speedYawAngle = (shipYawAngle - yawAngle) / ship.script.deltaMs * 1000;


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
				ship.LookPoint = Utils.DEFAULT_VECTOR_3D;
				shipYawAngle = 0;
				shipRollAngle = 0;
				shipPitchAngle = 0;
			}
			ship.LookingDir = isLookingDir;

			return isLookingDir;
		}


		public bool Override{
			get{
				return gyrosBlocks [0].GetProperty ("Override").AsBool ().GetValue(gyrosBlocks [0]);
			}
			set{
				for (int i = 0; i < gyrosBlocks.Count; i++) {
					gyrosBlocks [i].GetProperty ("Override").AsBool ().SetValue (gyrosBlocks [i], value);
				}

			}
		}

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

