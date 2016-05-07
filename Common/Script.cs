using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace SpaceEngineersScripts
{
	public abstract class Script
	{
		protected IMyGridTerminalSystem GridTerminalSystem;
		protected IMyTerminalBlock Me;

		protected int logLevel = LOG_NORMAL;

		public String logScreenName = null;

		public static int LOG_NONE = 0;
		public static int LOG_NORMAL = 1;
		public static int LOG_DEBUG = 2;
		public static int LOG_VERBOSE = 3;


		protected long clockStart = DateTime.Now.Ticks;

		protected long deltaMs;

		public delegate void LogDelegate(string message);

		public Script (IMyGridTerminalSystem GridTerminalSystem, IMyTerminalBlock Me)
		{
			this.GridTerminalSystem = GridTerminalSystem;
			this.Me = Me;
		}

		public abstract void Update (String argument);

		public void CalculateDelta ()
		{
			deltaMs = (DateTime.Now.Ticks - clockStart) / 10000;
			clockStart = DateTime.Now.Ticks;
		}

		protected List<IMyTerminalBlock> GetBlocks<T> ()  where T : IMyTerminalBlock
		{
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock> ();
			GridTerminalSystem.GetBlocksOfType<T> (blocks);
			return blocks;
		}

		protected List<IMyTerminalBlock> GetBlocksWithName (string name, string error = "")
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

		protected T GetBlock<T> (string error = "") where T : IMyTerminalBlock
		{
			List<IMyTerminalBlock> blocks = GetBlocks<T> ();
			if (blocks.Count == 0) {
				if (error == null || error.Length == 0)
					Log ("Impossible de trouver le block de type " + typeof(T).Name);
				else
					Log (error);
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

		private string logBuffer = "";
		IMyTextPanel logScreenBlock = null;


		protected void Log (string text, int level = 1)
		{
			if (level > logLevel)
				return;
			Utils.Echo (text);
			if (logScreenName == null || logScreenName.Length > 0) {
				if (logBuffer.Split ('\n').Length > 10)
					logBuffer = "";
				logBuffer += " " + text + "\n";

				if (logScreenBlock == null) {
					if (logScreenName != null) {
						logScreenBlock = (IMyTextPanel)GetBlocksWithName (logScreenName, error: "Can't find panel with name " + logScreenName) [0];
					} else {
						logScreenBlock = GetNearest<IMyTextPanel> (from: Me, error: "Attempted to find closest text panel block, but there are NO TEXT PANELS!");
					}
				}

				if (logScreenBlock != null) {
					WriteOnScreen (logScreenBlock, logBuffer);
				}
			}
		}

		protected void WriteOnScreen (IMyTextPanel panel, string txt)
		{
			panel.WritePublicText (txt);
			panel.ShowTextureOnScreen ();
			panel.ShowPublicTextOnScreen ();


		}
	}
}

