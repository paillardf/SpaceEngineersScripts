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

	public class StringMap
	{
		public Dictionary<string, string> data = new Dictionary<string, string> ();

		public StringMap ()
		{
		}

		public StringMap (string blockStr)
		{
			Parse (blockStr);
		}

		public void Clear ()
		{
			data.Clear ();
		}

		public bool Contains (string key)
		{
			return data.ContainsKey (key);
		}

		public void Remove (string key)
		{
			if (Contains (key)) {
				data.Remove (key);
			}
		}

		public void SetValue (string key, string value)
		{
			if (Contains (key)) {
				data.Remove (key);
			}
			data [key] = value;
		}

		public string GetValue (string key)
		{
			if (Contains (key)) {
				return data [key];
			}
			return "";
		}

		public StringMap GetMap (string key)
		{
			StringMap block = new StringMap ();
			block.Parse (this.GetValue (key));

			return block;
		}

		public int Count ()
		{
			return data.Count;
		}

		public void Parse (String values)
		{
			List<string> matches = Utils.Regex (@"\[(.*)\]", values);
			values = matches.Count == 0 ? values : matches [0];
			System.Text.RegularExpressions.Match m = (new System.Text.RegularExpressions.Regex (@"(([^;:\[]+):([^\[\];]+|\[.*\]));?")).Match (values);
			while (m.Success) {
				if (m.Groups.Count > 1) {
					this.SetValue (m.Groups [2].Value, m.Groups [3].Value);
				}		
				m = m.NextMatch ();
			}
		}

		public string ToText ()
		{
			string result = "";
			List<string> keys = new List<string> (data.Keys);

			for (int i = 0; i< data.Count; i++) {
				if (result.Length != 0)
					result += ";";
				result += keys [i] + ":" + data [keys [i]];
			}
			return "[" + result + "]";
		}
	}

}

