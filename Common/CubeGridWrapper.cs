using System;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace SpaceEngineersScripts
{
		// tag::content[]

	public class CubeGridWrapper : GridWrapper
	{
		IMyCubeGrid cubeGrid = null;

		public CubeGridWrapper(IMyCubeGrid gridToUse, IMyTerminalBlock sensor) : base(null, sensor)
		{
			cubeGrid = gridToUse;
		}

		public override List<IMyTerminalBlock> GetBlocks<T> ()
		{
			List<IMyTerminalBlock> rval = new List<IMyTerminalBlock>();

			if(cubeGrid == null)
				return rval;
			//iterate through block grid.
			int xmin = cubeGrid.Min.AxisValue(Base6Directions.Axis.LeftRight);
			int ymin = cubeGrid.Min.AxisValue(Base6Directions.Axis.UpDown);
			int zmin = cubeGrid.Min.AxisValue(Base6Directions.Axis.ForwardBackward);

			int xmax = cubeGrid.Max.AxisValue(Base6Directions.Axis.LeftRight);
			int ymax = cubeGrid.Max.AxisValue(Base6Directions.Axis.UpDown);
			int zmax = cubeGrid.Max.AxisValue(Base6Directions.Axis.ForwardBackward);

			for(int xc = xmin;xc<=xmax;xc++)
				for(int yc = ymin;yc<=ymax;yc++)
					for(int zc = zmin;zc<=zmax;zc++)
					{
						IMySlimBlock block = cubeGrid.GetCubeBlock(new Vector3I(xc, yc, zc));
						if(block != null && block.FatBlock is T)
						{
							//check if it's new.
							if(!rval.Contains(block.FatBlock as IMyTerminalBlock))
							{
								//if not, add it to the list.
								rval.Add(block.FatBlock as IMyTerminalBlock);
							}
						}
					}
			return rval;
		}

		protected override void GetBlockGroups (List<IMyBlockGroup> blockGroups){
			
		}

	}
		// end::content[]

}

