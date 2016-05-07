using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace SpaceEngineersScripts
{
	public abstract class Script
	{
		public GridWrapper GridWrapper;



		protected long clockStart = DateTime.Now.Ticks;

		public long deltaMs;

		public LogWrapper Logger;


		public Script (GridWrapper gridWrapper)
		{
			this.GridWrapper = gridWrapper;
			this.Logger = new LogWrapper ();
		}

		public virtual void Update (String argument){
			CalculateDelta ();
		}

		public void CalculateDelta ()
		{
			deltaMs = (DateTime.Now.Ticks - clockStart) / 10000;
			clockStart = DateTime.Now.Ticks;
		}




	}
}

