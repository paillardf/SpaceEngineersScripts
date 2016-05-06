using System;
using Sandbox.ModAPI.Ingame;

namespace SpaceEngineersScripts.Tests
{
	public class StubTextPanel : StubTerminalBlock, IMyTextPanel
	{
		public virtual bool WritePublicText (string value, bool append)
		{
			throw new NotImplementedException ();
		}

		public string GetPublicText ()
		{
			throw new NotImplementedException ();
		}

		public bool WritePublicTitle (string value, bool append)
		{
			throw new NotImplementedException ();
		}

		public string GetPublicTitle ()
		{
			throw new NotImplementedException ();
		}

		public bool WritePrivateText (string value, bool append)
		{
			throw new NotImplementedException ();
		}

		public string GetPrivateText ()
		{
			throw new NotImplementedException ();
		}

		public bool WritePrivateTitle (string value, bool append)
		{
			throw new NotImplementedException ();
		}

		public string GetPrivateTitle ()
		{
			throw new NotImplementedException ();
		}

		public void AddImageToSelection (string id, bool checkExistance)
		{
			throw new NotImplementedException ();
		}

		public void AddImagesToSelection (System.Collections.Generic.List<string> ids, bool checkExistance)
		{
			throw new NotImplementedException ();
		}

		public void RemoveImageFromSelection (string id, bool removeDuplicates)
		{
			throw new NotImplementedException ();
		}

		public void RemoveImagesFromSelection (System.Collections.Generic.List<string> ids, bool removeDuplicates)
		{
			throw new NotImplementedException ();
		}

		public void ClearImagesFromSelection ()
		{
			throw new NotImplementedException ();
		}

		public virtual void ShowPublicTextOnScreen ()
		{
			throw new NotImplementedException ();
		}

		public void ShowPrivateTextOnScreen ()
		{
			throw new NotImplementedException ();
		}

		public virtual void ShowTextureOnScreen ()
		{
			throw new NotImplementedException ();
		}

		public void RequestEnable (bool enable)
		{
			throw new NotImplementedException ();
		}

		public bool Enabled {
			get {
				throw new NotImplementedException ();
			}
		}

		public void GetSelectedImages (System.Collections.Generic.List<string> output)
		{
			throw new NotImplementedException ();
		}

		public void SetShowOnScreen (VRage.Game.GUI.TextPanel.ShowTextOnScreenFlag set)
		{
			throw new NotImplementedException ();
		}

		public string CurrentlyShownImage {
			get {
				throw new NotImplementedException ();
			}
		}

		public VRage.Game.GUI.TextPanel.ShowTextOnScreenFlag ShowOnScreen {
			get {
				throw new NotImplementedException ();
			}
		}

		public bool ShowText {
			get {
				throw new NotImplementedException ();
			}
		}
	}
}

