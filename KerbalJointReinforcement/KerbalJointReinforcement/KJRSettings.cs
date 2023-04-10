using System;
using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;

using KSP.IO;
using UnityEngine;
/*
namespace KerbalJointReinforcement
{
	public class KJRSettings : GameParameters.CustomParameterNode
	{
		public override string Title
		{ get { return "General Options"; } }

		public override string DisplaySection
		{ get { return "Joint Reinforcement (KJR next)"; } }

		public override string Section
		{ get { return "Joint Reinforcement (KJR next)"; } }

		public override int SectionOrder
		{ get { return 1; } }

		public override GameParameters.GameMode GameMode
		{ get { return GameParameters.GameMode.ANY; } }

		public override bool HasPresets
		{ get { return false; } }


		public enum StrengthLevel
		{
			Realistic,
			Strong,
			Strongest,
			Indistructible
		};

		[GameParameters.CustomParameterUI("Strength and stiffness level", autoPersistance = false)]
		public StrengthLevel level = StrengthLevel.Realistic;


		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			if(!KJRJointUtils.loaded)
				KJRJointUtils.LoadConstants();

			level = KJRJointUtils.addExtraStabilityJoints ? StrengthLevel.Realistic :
				(StrengthLevel)(KJRJointUtils.extraLevel + 1);
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			KJRJointUtils.addExtraStabilityJoints = (level > StrengthLevel.Realistic);
			KJRJointUtils.extraLevel = ((int)level) - 1;

			PluginConfiguration config = PluginConfiguration.CreateForType<KJRManager>();
			config.load();

			config.SetValue("addExtraStabilityJoints", KJRJointUtils.addExtraStabilityJoints);
			config.SetValue("extraLevel", KJRJointUtils.extraLevel);

			config.save();
		}
	}
}
*/
