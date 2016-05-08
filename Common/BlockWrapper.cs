using System;
using SpaceEngineersScripts.Tests;

using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRageMath;
using VRage;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;

namespace SpaceEngineersScripts
{
	// tag::content[]

	public class BlockWrapper
	{

		public IMyTerminalBlock block = null;
		public StringMap map = null;
		IMyCubeGrid RefGrid;
		Vector3I IndexOffset;
		public Quaternion OrientOffset;

		public BlockWrapper (IMyTerminalBlock block)
		{
			this.block = block;
			RefGrid = block.CubeGrid;
			IndexOffset = block.Position;
			block.Orientation.GetQuaternion (out OrientOffset);

		}

		// Direction Vectors
		private Vector3D TransformedDirVectorToGlobalBase (Vector3I dirIndexVect)
		{
			Vector3D fromPoint = RefGrid.GridIntegerToWorld (IndexOffset);
			Vector3D toPoint = RefGrid.GridIntegerToWorld (IndexOffset - Vector3I.Transform (dirIndexVect, OrientOffset));
			Vector3D transformedVector = fromPoint - toPoint;
			transformedVector.Normalize ();
			return transformedVector;
		}

		public Vector3D TransformVectorToShipBase (Vector3D vect)
		{
			Vector3D forward = VectorForward;
			Vector3D left = VectorLeft;
			Vector3D up = VectorUp;
			MatrixD P = new MatrixD (forward.GetDim (0), forward.GetDim (1), forward.GetDim (2),
				left.GetDim (0), left.GetDim (1), left.GetDim (2),
				up.GetDim (0), up.GetDim (1), up.GetDim (2));
			MatrixD Pinv = MatrixD.Invert (P);
			return Vector3D.Transform (vect, Pinv);
		}

		public Vector3D VectorPosition     { get { return block.GetPosition (); } }

		public Vector3D VectorRight     { get { return TransformedDirVectorToGlobalBase (new Vector3I (1, 0, 0)); } }

		public Vector3D VectorUp        { get { return TransformedDirVectorToGlobalBase (new Vector3I (0, 1, 0)); } }

		public Vector3D VectorBackward  { get { return TransformedDirVectorToGlobalBase (new Vector3I (0, 0, 1)); } }

		public Vector3D VectorLeft      { get { return TransformedDirVectorToGlobalBase (new Vector3I (-1, 0, 0)); } }

		public Vector3D VectorDown      { get { return TransformedDirVectorToGlobalBase (new Vector3I (0, -1, 0)); } }

		public Vector3D VectorForward   { get { return TransformedDirVectorToGlobalBase (new Vector3I (0, 0, -1)); } }

		public string GetName ()
		{
			List<string> matches = Utils.Regex (@"([^\[.]+)\b( *\[.*\])", block.CustomName);
			return matches.Count == 0 ? block.CustomName : matches [0];
		}

		public virtual void Load ()
		{
			this.map = new StringMap (block.CustomName);
		}

		public virtual void Save ()
		{
			string name = this.GetName ();
			block.SetCustomName (name + " " + map.ToText ());
		}

	}
	// end::content[]

}

