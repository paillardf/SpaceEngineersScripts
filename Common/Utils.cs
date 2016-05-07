using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;
using Sandbox.ModAPI.Ingame;

namespace SpaceEngineersScripts
{
	public class Utils
	{

		public static SpaceEngineersScripts.Script.LogDelegate Echo;


		public class RegexResult
		{

			public RegexResult (string v, int index)
			{
				this.Index = index;
				this.Value = v;
			}

			public string Value;
			public int Index;
		}


		static string REGEX_PARAM_EXTRACTOR = @"^([\w]+)?(?:\(((?>[^()]+|\((?<open>)|\)(?<-open>))*(?(open)(?!)))\))*$";

		private static List<KeyValuePair<System.Text.RegularExpressions.Capture, RegexResult>> ExtractParameters (string cmd, string separators, out string methodName, out string parameters)
		{
			List<string> tmp = RegexGroup (REGEX_PARAM_EXTRACTOR, cmd);

			parameters = cmd;
			methodName = "";

			if (tmp.Count != 0) {
				parameters = tmp [1];
				methodName = tmp [0];
			}

			string REGEX_PARAM_SPLITER = @"([" + separators + @"]+)?([^" + separators + @"()]*((?:\((?>[^()]+|\((?<open>)|\)(?<-open>))*(?(open)(?!))\)))*)+";
			System.Text.RegularExpressions.Match m = (new System.Text.RegularExpressions.Regex (REGEX_PARAM_SPLITER)).Match (parameters);
			List<KeyValuePair<System.Text.RegularExpressions.Capture, RegexResult>> Matches = new List<KeyValuePair<System.Text.RegularExpressions.Capture, RegexResult>> ();
			while (m.Success) {
				if (m.Value.Length > 0) {
					RegexResult result = new RegexResult (m.Value.Substring (m.Groups [1].Value.Length), m.Groups [2].Captures [0].Index);
					Matches.Add (new KeyValuePair<System.Text.RegularExpressions.Capture, RegexResult> (m.Groups [1], result));
				}
				m = m.NextMatch ();
			}

			if (Matches.Count == 0 && !cmd.Contains ("()") && cmd.Contains ("(")) {
				Echo ("Command can't be parse : " + cmd + " with : " + separators); 
			}
			return Matches;
		}

		public static List<string> ExtractFunctionParameters (string cmd, out string functionName)
		{
			string parametersString;
			List<KeyValuePair<System.Text.RegularExpressions.Capture, RegexResult>> matches = ExtractParameters (cmd, ",", out functionName, out parametersString);
			List<string> result = new List<string> ();
			for (int i = 0; i < matches.Count; i++) {
				result.Add (matches [i].Value.Value.Trim ());
			}
			return result;
		}


		public static List<string> Regex (string Pattern, string t)
		{
			string text = t.Trim ();
			System.Text.RegularExpressions.Match m = (new System.Text.RegularExpressions.Regex (Pattern)).Match (text);
			List<string> Matches = new List<string> ();
			while (m.Success) {
				if (m.Groups.Count > 1) {
					Matches.Add (m.Groups [1].Value);
				}		
				m = m.NextMatch ();
			}

			return Matches;
		}

		public static List<string> RegexGroup (string Pattern, string t)
		{
			string text = t.Trim ();
			System.Text.RegularExpressions.Match m = (new System.Text.RegularExpressions.Regex (Pattern)).Match (text);
			List<string> result = new List<string> ();
			if (m.Success) {
				for (int i =1; i<m.Groups.Count; i++) {
					result.Add (m.Groups [i].Value);
				}
			}
			return result;
		}





		public static string VectorToString (Vector3D vector)
		{
			return "v(" + vector.GetDim (0) + "," + vector.GetDim (1) + "," + vector.GetDim (2) + ")";
		}

		public static string VectorToStringRound (Vector3D vector)
		{
			return "v(" + RoundD (vector.GetDim (0)) + "," + RoundD (vector.GetDim (1)) + "," + RoundD (vector.GetDim (2)) + ")";
		}

		public static double RoundD (double d)
		{
			return Math.Round (d, 2);
		}


		public static double DEFAULT_DOUBLE = -999999999;
		public static Vector3D DEFAULT_VECTOR_3D = new Vector3D (DEFAULT_DOUBLE, DEFAULT_DOUBLE, DEFAULT_DOUBLE);


		public static T CastString<T> (string operation)
		{


			if (typeof(T) == typeof(bool)) {
				bool vbool;
				if (bool.TryParse (operation, out vbool)) {
					return (T)(object)vbool;
				} else {
					return default(T);
				}
			} else if (typeof(T) == typeof(float)) {
				float vfloat;
				if (float.TryParse (operation, out vfloat)) {
					return (T)(object)vfloat;
				} else {
					return (T)(object)default(T);
				}
			} else if (typeof(T) == typeof(double)) {

				double vdouble;
				if (double.TryParse (operation, out vdouble)) {
					return (T)(object)vdouble;
				} else {
					return (T)(object)DEFAULT_DOUBLE;
				}
			} else if (typeof(T) == typeof(int)) {
				int vint;
				if (int.TryParse (operation, out vint)) {
					return (T)(object)vint;
				} else {
					return (T)(object)default(T);
				}
			} else if (typeof(T) == typeof(string)) {
				return (T)(object)operation;
			} else if (typeof(T) == typeof(Vector3D)) {
				string methodName, parametersString;
				var parameters = ExtractFunctionParameters (operation, out methodName);
				if (methodName.Equals ("v") && parameters.Count == 3) {
					Vector3D vect = new Vector3D (CastString<double> (parameters [0]), CastString<double> (parameters [1]), CastString<double> (parameters [2]));
					return (T)(object)vect;
				} else {
					return (T)(object)DEFAULT_VECTOR_3D;
				}
			} else {
				Echo ("can't convert value " + operation + " to type " + typeof(T));
				return (T)(object)default(T);
			}

		}

	}
}

