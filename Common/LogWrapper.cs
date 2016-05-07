using System;
using Sandbox.ModAPI.Ingame;

namespace SpaceEngineersScripts
{
	public class LogWrapper
	{

		public delegate void LogDelegate(string message);

		public static LogDelegate Echo;

		protected int logLevel = LOG_NORMAL;

		public static int LOG_NONE = 0;
		public static int LOG_NORMAL = 1;
		public static int LOG_DEBUG = 2;
		public static int LOG_VERBOSE = 3;


		public LogWrapper ()
		{
		}

		public void setLevel(int level){
			this.logLevel = level;
		}

		public void setLogScreen(IMyTextPanel panel){
			logScreenBlock = panel;
		}

		public void clear(){
			logBuffer = "";
			Log ("");
		}

		private string logBuffer = "";
		IMyTextPanel logScreenBlock = null;


		public void Log (string text, int level = 1)
		{
			if (level > logLevel)
				return;
			Echo (text);
			if (logScreenBlock != null) {
				
				if (logBuffer.Split ('\n').Length > 10)
					logBuffer = "";
				logBuffer += " " + text + "\n";
				WriteOnScreen (logScreenBlock, logBuffer);

			}
		}

		public static void WriteOnScreen (IMyTextPanel panel, string txt)
		{
			panel.WritePublicText (txt);
			panel.ShowTextureOnScreen ();
			panel.ShowPublicTextOnScreen ();


		}
	}
}

