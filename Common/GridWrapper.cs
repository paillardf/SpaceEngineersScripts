using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace SpaceEngineersScripts
{
	// tag::content[]

	public class GridWrapper
	{
		public IMyGridTerminalSystem Grid;
		public IMyTerminalBlock Terminal;

		public GridWrapper (IMyGridTerminalSystem GridTerminalSystem, IMyTerminalBlock Me)
		{
			this.Grid = GridTerminalSystem;
		}


		public List<IMyTerminalBlock> GetBlocks<T> ()  where T : IMyTerminalBlock
		{
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock> ();
			Grid.GetBlocksOfType<T> (blocks);
			return blocks;
		}

		public List<IMyTerminalBlock> GetBlocksWithName (string name, string error = "")
		{
			List<IMyTerminalBlock> result = new List<IMyTerminalBlock> ();
			List<IMyBlockGroup> groups = new List<IMyBlockGroup> ();
			Grid.GetBlockGroups (groups);
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
			Grid.GetBlocks (blocks);
			for (int i = 0; i < blocks.Count; i ++) {
				var block = blocks [i];
				if (block.CustomName.StartsWith (name)) {
					result.Add (block);
				}
			}
			if (result.Count == 0 && error.Length > 0) {
				LogWrapper.Echo (error);
			}
			return result;

		}

		public T GetBlock<T> (string error = "") where T : IMyTerminalBlock
		{
			List<IMyTerminalBlock> blocks = GetBlocks<T> ();
			if (blocks.Count == 0) {
				if (error == null || error.Length == 0)
					LogWrapper.Echo ("Impossible de trouver le block de type " + typeof(T).Name);
				else
					LogWrapper.Echo (error);
				return default(T);
			} else {
				return (T)blocks [0];
			}

		}

		public T GetNearest<T> (IMyTerminalBlock from = null, string error="") where T: IMyTerminalBlock
		{
			if (from == null)
				from = Terminal;
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
					LogWrapper.Echo ("Can't find closest block of type " + typeof(T).Name);
				else
					LogWrapper.Echo (error);
			}
			return best;
		}
	}
	// end::content[]

}

