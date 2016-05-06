using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using SpaceEngineersScripts.Tests;

namespace SpaceEngineersScripts.SpaceOS
{
	using NUnit.Framework;

	[TestFixture]
	public class SpaceOSTests
	{
		Program spaceOS;


		public SpaceOSTests ()
		{
		}

		[SetUp] public void Before ()
		{
			spaceOS = new Program ();
			spaceOS.initForTest ();
		}

		[Test]
		public void getDetailedInfoValueTestWithInvalidKey ()
		{
			// for an invalid key
			//Assert.AreEqual (spaceOS.getName (), "hi");
		}
	}
}

