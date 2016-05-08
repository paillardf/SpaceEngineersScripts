using System;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace SpaceEngineersScripts
{
	// tag::content[]

	public abstract class Script
	{
		public GridWrapper GridWrapper;



		protected long clockStart = DateTime.Now.Ticks;

		public long deltaMs;
		public long minDelta = 100;

		public LogWrapper Logger;


		public Script (GridWrapper gridWrapper)
		{
			this.GridWrapper = gridWrapper;
			this.Logger = new LogWrapper ();
		}

		public virtual void Update (String argument){
			if (!CalculateDelta ()) {
				return;
			}
		}

		public bool CalculateDelta ()
		{

			long tmpDeltaMs = (DateTime.Now.Ticks - clockStart) / 10000;
			if (tmpDeltaMs < minDelta) {
				return false;
			}
			deltaMs = tmpDeltaMs;
			clockStart = DateTime.Now.Ticks;
			return true;
		}




	}
	// end::content[]

}

