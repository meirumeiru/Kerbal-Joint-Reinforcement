using System;
using System.Collections.Generic;

using KSP.IO;
using UnityEngine;

namespace KerbalJointReinforcement
{
	public class KJRSettings : GameParameters.CustomParameterNode
	{
		public override string Title
		{ get { return "Options"; } }

		public override string DisplaySection
		{ get { return "Options"; } }

		public override string Section
		{ get { return "Joint Reinforcement"; } }

		public override int SectionOrder
		{ get { return 3; } }

		public override GameParameters.GameMode GameMode
		{ get { return GameParameters.GameMode.ANY; } }

		public override bool HasPresets
		{ get { return false; } }
/*
		public enum Mode
		{ Realistic, RealisticPlus, Rigid, RigidPlus };

		[GameParameters.CustomParameterUI("Mode", autoPersistance = false)]
		public Mode mode = Mode.Realistic;
*/
		[GameParameters.CustomParameterUI("Reinforce weak attach nodes",
			toolTip = "Identifies and reinforces attach nodes that are too weak for the size of the connected parts")]
		public bool reinforceAttachNodes = true;

		[GameParameters.CustomParameterUI("Strengthen not correctly working joints",
			toolTip = "Identifies and reinforces those joints in a vessel that are too weak due to limitations of the game and the game engine")]
		public bool reinforceInversions = true;

		[GameParameters.CustomParameterUI("Stiffen vessels further",
			toolTip = "Adds additional joints depending on the selected 'Extra Joint Level'")]
		public bool extraJoints = true;

		[GameParameters.CustomIntParameterUI("Extra Joint Level", maxValue = 3, minValue = 1,
			toolTip = "1 - Adds weak joints to dampen unwanted wobbling (realistic)\n2 - Adds strong joints to dampen wobbling (less realistic)\n3 - Adds a lot of very strong joints to eliminate all movement between parts (unrealistic)")]
		public int extraLevel = 1;

		public override void SetDifficultyPreset(GameParameters.Preset preset)
		{
			switch(preset)
			{
			case GameParameters.Preset.Easy:
				reinforceAttachNodes = true;
				reinforceInversions = true;
				extraJoints = true;
				extraLevel = 2;
				break;

			case GameParameters.Preset.Normal:
				reinforceAttachNodes = true;
				reinforceInversions = true;
				extraJoints = true;
				extraLevel = 1;
				break;

			case GameParameters.Preset.Moderate:
				reinforceAttachNodes = true;
				reinforceInversions = true;
				extraJoints = false;
				extraLevel = 1;
				break;

			case GameParameters.Preset.Hard:
				reinforceAttachNodes = true;
				reinforceInversions = true;
				extraJoints = false;
				extraLevel = 1;
				break;
			}
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

// FEHLER, nur wenn sich was geändert hat eigentlich...

			KJRJointUtils.LoadConstants(false);

			if(HighLogic.LoadedSceneIsFlight)
				KJRManager.Instance.OnVesselWasModified(FlightGlobals.ActiveVessel);
		}
	}
}
