﻿using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace SpaceEngineersScripts.Autopilot
{
	public class Ship : BlockWrapper
	{
		IMyRemoteControl controlBlock;
		public Ship (IMyRemoteControl block):base(block)
		{
			controlBlock = block;
		}


		public void moveTo(Vector3D[] destination){
			controlBlock.ClearWaypoints ();
			for (int i = 0; i < destination.Length; i++) {
				controlBlock.AddWaypoint (destination[i], Utils.VectorToStringRound(destination[i]));
			}
			controlBlock.SetAutoPilotEnabled (true);

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



		public class DynamicRefBody : RefBody {
			internal override void Initialize() {}

			// Constructors re-directed to base (why?, because C#, that's why)
			public DynamicRefBody(IMyCubeGrid refGrid, Vector3I indexOffset, Quaternion orientOffset) : base(refGrid, indexOffset, orientOffset) {} 
			public DynamicRefBody(IMyCubeGrid refGrid, Vector3I indexOffset) : base(refGrid, indexOffset) {} 
			public DynamicRefBody(IMyCubeBlock block) : base(block) {} 
			public DynamicRefBody(IMyTerminalBlock block) : base(block) {} 

			// Direction Vectors : calculated anew each time they're requested
			public override Vector3D VectorRight     { get { return GetTransformedDirVector(new Vector3I( 1, 0, 0)); }}
			public override Vector3D VectorUp        { get { return GetTransformedDirVector(new Vector3I( 0, 1, 0)); }}
			public override Vector3D VectorBackward  { get { return GetTransformedDirVector(new Vector3I( 0, 0, 1)); }}
			public override Vector3D VectorLeft      { get { return GetTransformedDirVector(new Vector3I(-1, 0, 0)); }}
			public override Vector3D VectorDown      { get { return GetTransformedDirVector(new Vector3I( 0,-1, 0)); }}
			public override Vector3D VectorForward   { get { return GetTransformedDirVector(new Vector3I( 0, 0,-1)); }}
			public override Vector3D Position { get { return RefGrid.GridIntegerToWorld(IndexOffset); }}
		}

		public class RefBody {
			public IMyCubeGrid RefGrid;       // Reference to a "ship" as a single rigid body
			public Vector3I IndexOffset;      // Index of the block you're interested in
			public Quaternion OrientOffset;   // quaternion used to transform the ship's direction vectors, to the block
			// Reference Vectors : to be overridden
			internal Vector3D i_VectorRight, i_VectorLeft, i_VectorUp, i_VectorDown, i_VectorBackward, i_VectorForward, i_Position;
			public virtual Vector3D VectorRight     { get {return i_VectorRight;   } internal set {i_VectorRight    = value;}}
			public virtual Vector3D VectorLeft      { get {return i_VectorLeft;    } internal set {i_VectorLeft     = value;}}
			public virtual Vector3D VectorUp        { get {return i_VectorUp;      } internal set {i_VectorUp       = value;}}
			public virtual Vector3D VectorDown      { get {return i_VectorDown;    } internal set {i_VectorDown     = value;}}
			public virtual Vector3D VectorBackward  { get {return i_VectorBackward;} internal set {i_VectorBackward = value;}}
			public virtual Vector3D VectorForward   { get {return i_VectorForward; } internal set {i_VectorForward  = value;}}
			public virtual Vector3D Position        { get {return i_Position;      } internal set {i_Position       = value;}}

			// Constructor(s)
			public RefBody(IMyCubeGrid refGrid, Vector3I indexOffset, Quaternion orientOffset) {
				RefGrid = refGrid;
				IndexOffset = indexOffset;
				OrientOffset = orientOffset;
				Initialize();
			}
			public RefBody(IMyCubeGrid refGrid) : this(refGrid, Vector3I.Zero, Quaternion.Identity) {}
			public RefBody(IMyCubeGrid refGrid, Vector3I indexOffset) : this(refGrid, indexOffset, Quaternion.Identity) {}
			public RefBody(IMyCubeBlock block) {
				RefGrid = block.CubeGrid;
				IndexOffset = block.Position;
				block.Orientation.GetQuaternion(out OrientOffset);
				Initialize();
			}
			public RefBody(IMyTerminalBlock block) : this(block as IMyCubeBlock) {}

			// Direction Vectors
			internal Vector3D GetTransformedDirVector(Vector3I dirIndexVect) {
				Vector3D fromPoint = RefGrid.GridIntegerToWorld(IndexOffset);
				Vector3D toPoint   = RefGrid.GridIntegerToWorld(IndexOffset - Vector3I.Transform(dirIndexVect, OrientOffset));
				return fromPoint - toPoint;
			}

			// Abstract Functions
			internal virtual void Initialize() {}
		}


	}
}

