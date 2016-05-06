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
		public Vector3D GetTransformedDirVector (Vector3I dirIndexVect)
		{
			Vector3D fromPoint = RefGrid.GridIntegerToWorld (IndexOffset);
			Vector3D toPoint = RefGrid.GridIntegerToWorld (IndexOffset - Vector3I.Transform (dirIndexVect, OrientOffset));
			Vector3D transformedVector = fromPoint - toPoint;
			transformedVector.Normalize ();
			return transformedVector;
		}

		public Vector3D VectorPosition     { get { return block.GetPosition (); } }

		public Vector3D VectorRight     { get { return GetTransformedDirVector (new Vector3I (1, 0, 0)); } }

		public Vector3D VectorUp        { get { return GetTransformedDirVector (new Vector3I (0, 1, 0)); } }

		public Vector3D VectorBackward  { get { return GetTransformedDirVector (new Vector3I (0, 0, 1)); } }

		public Vector3D VectorLeft      { get { return GetTransformedDirVector (new Vector3I (-1, 0, 0)); } }

		public Vector3D VectorDown      { get { return GetTransformedDirVector (new Vector3I (0, -1, 0)); } }

		public Vector3D VectorForward   { get { return GetTransformedDirVector (new Vector3I (0, 0, -1)); } }

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
}

