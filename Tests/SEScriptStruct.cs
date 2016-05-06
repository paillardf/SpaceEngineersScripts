using System;
using SpaceEngineersScripts.Tests;

using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRageMath;
using VRage;

namespace SpaceEngineersScripts
{
	public abstract class SEScriptStruct
	{
		public static void Echo (string value)
		{
		}

		protected String Storage;
		protected IMyGridTerminalSystem GridTerminalSystem;
		protected IMyTerminalBlock Me;







		public void initForTest(){
			initForTest (new StubGridTerminalSystem (), new StubTerminalBlock ());
		}
		public void initForTest(IMyGridTerminalSystem grid , IMyTerminalBlock terminal){
			Me = terminal;
			GridTerminalSystem = grid;
		}

	}

	public interface BaseScriptMethods {

		void Main (string argument);


		// Called when the program needs to save its state. Use
		// this method to save your state to the Storage field
		// or some other means. 
		// 
		// This method is optional and can be removed if not
		// needed.
		void Save();


	}
}

