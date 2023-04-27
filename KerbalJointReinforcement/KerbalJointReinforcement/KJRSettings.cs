using System;
using System.Collections.Generic;
using System.Reflection;

using KSP.IO;
using UnityEngine;

namespace KerbalJointReinforcement
{
	public class KJRSettings : GameParameters.CustomParameterNode
	{
		public override string Title
		{ get { return "Options"; } }

		public override string DisplaySection
		{ get { return "Joint Reinforcement"; } }

		public override string Section
		{ get { return "Joint Reinforcement"; } }

		public override int SectionOrder
		{ get { return 3; } }

		public override GameParameters.GameMode GameMode
		{ get { return GameParameters.GameMode.ANY; } }

		public override bool HasPresets
		{ get { return true; } }

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
			toolTip = "Select how many joints will be built and the strength of the joints.", displayFormat = " ")]
		public int extraLevel = 1;

		[GameParameters.CustomStringParameterUI("")]
		public string spacer = "";

		[GameParameters.CustomStringParameterUI("", lines = 2)]
		public string extraLevelDescription = "";

		public override bool Interactible(MemberInfo member, GameParameters parameters)
		{
			if(member.Name == "extraJoints")
				return reinforceInversions;

			if(member.Name == "extraLevel")
			{
				switch((reinforceInversions && extraJoints) ? extraLevel : 0)
				{
				case 1:
					extraLevelDescription = "Adds weak joints to dampen unwanted wobbling (realistic)"; break;
				case 2:
					extraLevelDescription = "Adds strong joints to dampen wobbling (less realistic)"; break;
				case 3:
					extraLevelDescription = "Adds a lot of very strong joints to eliminate all movement between parts (unrealistic)"; break;
				default:
					extraLevelDescription = ""; break;
				}

				return reinforceInversions && extraJoints;
			}

			return base.Interactible(member, parameters);
		}

		public override bool Enabled(MemberInfo member, GameParameters parameters)
		{
			return base.Enabled(member, parameters);
		}

		public override void SetDifficultyPreset(GameParameters.Preset preset)
		{
			switch(preset)
			{
			case GameParameters.Preset.Easy:
				reinforceAttachNodes = KJRJointUtils.Easy.reinforceAttachNodes;
				reinforceInversions = KJRJointUtils.Easy.reinforceInversions;
				extraJoints = KJRJointUtils.Easy.extraLevel > 0;
				extraLevel = KJRJointUtils.Easy.extraLevel;
				break;

			case GameParameters.Preset.Normal:
				reinforceAttachNodes = KJRJointUtils.Normal.reinforceAttachNodes;
				reinforceInversions = KJRJointUtils.Normal.reinforceInversions;
				extraJoints = KJRJointUtils.Normal.extraLevel > 0;
				extraLevel = KJRJointUtils.Normal.extraLevel;
				break;

			case GameParameters.Preset.Moderate:
				reinforceAttachNodes = KJRJointUtils.Moderate.reinforceAttachNodes;
				reinforceInversions = KJRJointUtils.Moderate.reinforceInversions;
				extraJoints = KJRJointUtils.Moderate.extraLevel > 0;
				extraLevel = KJRJointUtils.Moderate.extraLevel;
				break;

			case GameParameters.Preset.Hard:
				reinforceAttachNodes = KJRJointUtils.Hard.reinforceAttachNodes;
				reinforceInversions = KJRJointUtils.Hard.reinforceInversions;
				extraJoints = KJRJointUtils.Hard.extraLevel > 0;
				extraLevel = KJRJointUtils.Hard.extraLevel;
				break;
			}
		}
	}
}
