using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

namespace KerbalJointReinforcement
{
	public static class KJRJointUtils
	{
		public static bool loaded = false;

		// presets
		public struct Preset { public bool reinforceAttachNodes; public bool reinforceInversions; public int extraLevel; };

		public static Preset Default = new Preset { reinforceAttachNodes = true, reinforceInversions = true, extraLevel = 0 };
		public static Preset Easy = new Preset { reinforceAttachNodes = true, reinforceInversions = true, extraLevel = 2 };
		public static Preset Normal = new Preset { reinforceAttachNodes = true, reinforceInversions = true, extraLevel = 1 };
		public static Preset Moderate = new Preset { reinforceAttachNodes = true, reinforceInversions = true, extraLevel = 0 };
		public static Preset Hard = new Preset { reinforceAttachNodes = true, reinforceInversions = true, extraLevel = 0 };

		// selected options
		public static bool reinforceAttachNodes = true;
		public static bool reinforceInversions = true;
		public static bool reinforceLaunchClamps = false;
		public static int extraLevel = 0;
		public static bool disableAutoStruts = false;

		// reinforcement settings
		public static bool useVolumeNotArea = true;
		public static float massForAdjustment = 0.01f;

		public static float breakForceMultiplier = 1f;
		public static float breakTorqueMultiplier = 1f;
		public static float breakStrengthPerArea = 1500f;
		public static float breakTorquePerMOI = 6000f;

		// inversion settings
		public static float inversionMassFactor = 2f;
		public static float solutionMassFactor = 2f;

		// extra joint settings
		public static float extraLinearForceW = 10f;
		public static float extraLinearSpringW = PhysicsGlobals.JointForce;
		public static float extraLinearDamperW = 0f;

		public static float extraLinearForce = PhysicsGlobals.JointForce;
		public static float extraLinearSpring = PhysicsGlobals.JointForce;
		public static float extraLinearDamper = 0f;

		public static float extraAngularForceW = 10f;
		public static float extraAngularSpringW = 60000f;
		public static float extraAngularDamperW = 0f;

		public static float extraAngularForce = PhysicsGlobals.JointForce;
		public static float extraAngularSpring = 60000f;
		public static float extraAngularDamper = 0f;

		public static float extraBreakingForce = float.MaxValue;
		public static float extraBreakingTorque = float.MaxValue;


		public static bool debug = false;


		public static List<Part> tempPartList;
		public static List<Part> tempSet1;
		public static List<Part> tempSet2;


		private static void LoadPreset(string value, ref Preset preset)
		{
			if(value.Length == 0)
				return;

			string[] parts = value.Split(';');

			if(parts.Length < 3)
				return;

			preset = new Preset {
				reinforceAttachNodes = bool.Parse(parts[0]),
				reinforceInversions = bool.Parse(parts[1]),
				extraLevel = int.Parse(parts[2]) };
		}

		public static void LoadDefaults()
		{
			PluginConfiguration config = PluginConfiguration.CreateForType<KJRManager>();
			config.load();

			LoadPreset(config.GetValue<string>("Easy", ""), ref Easy);
			LoadPreset(config.GetValue<string>("Moderate", ""), ref Moderate);
			LoadPreset(config.GetValue<string>("Normal", ""), ref Normal);
			LoadPreset(config.GetValue<string>("Hard", ""), ref Hard);

			Default = new Preset {
				reinforceAttachNodes = config.GetValue<bool>("reinforceAttachNodes", true),
				reinforceInversions = config.GetValue<bool>("reinforceInversions", true),
				extraLevel = config.GetValue<int>("extraLevel", 0) };

			ApplyPresets(Default);
			reinforceLaunchClamps = config.GetValue<bool>("reinforceLaunchClamps", false);

			useVolumeNotArea = config.GetValue<bool>("useVolumeNotArea", true);
			massForAdjustment = (float)config.GetValue<double>("massForAdjustment", 0.01f);

			breakForceMultiplier = (float)config.GetValue<double>("breakForceMultiplier", 1f);
			breakTorqueMultiplier = (float)config.GetValue<double>("breakTorqueMultiplier", 1f);

			breakStrengthPerArea = (float)config.GetValue<double>("breakStrengthPerArea", 1500f);
			breakTorquePerMOI = (float)config.GetValue<double>("breakTorquePerMOI", 6000f);

			inversionMassFactor = (float)config.GetValue<double>("inversionMassFactor", 2f);
			solutionMassFactor = (float)config.GetValue<double>("solutionMassFactor", 2f);

			extraLinearForceW = (float)config.GetValue<double>("extraLinearForceW", 10f);
			extraLinearSpringW = (float)config.GetValue<double>("extraLinearSpringW", PhysicsGlobals.JointForce);
			extraLinearDamperW = (float)config.GetValue<double>("extraLinearDamperW", 0f);

			extraLinearForce = (float)config.GetValue<double>("extraLinearForce", PhysicsGlobals.JointForce);
			extraLinearSpring = (float)config.GetValue<double>("extraLinearSpring", PhysicsGlobals.JointForce);
			extraLinearDamper = (float)config.GetValue<double>("extraLinearDamper", 0f);

			extraAngularForceW = (float)config.GetValue<double>("extraAngularForceW", 10f);
			extraAngularSpringW = (float)config.GetValue<double>("extraAngularSpringW", 60000f);
			extraAngularDamperW = (float)config.GetValue<double>("extraAngularDamperW", 0f);

			extraAngularForce = (float)config.GetValue<double>("extraAngularForce", PhysicsGlobals.JointForce);
			extraAngularSpring = (float)config.GetValue<double>("extraAngularSpring", 60000f);
			extraAngularDamper = (float)config.GetValue<double>("extraAngularDamper", 0f);

			extraBreakingForce = (float)config.GetValue<double>("extraBreakingForce", float.MaxValue);
			extraBreakingTorque = (float)config.GetValue<double>("extraBreakingTorque", float.MaxValue);

			debug = config.GetValue<bool>("debug", false);

			loaded = true;
		}

		public static bool ApplyPresets(Preset preset)
		{
			bool result =
				   (reinforceAttachNodes != preset.reinforceAttachNodes)
				|| (reinforceInversions != preset.reinforceInversions)
				|| (extraLevel != preset.extraLevel)
				|| (disableAutoStruts != false);

			if(!result)
				return false;

			reinforceAttachNodes = preset.reinforceAttachNodes;
			reinforceInversions = preset.reinforceInversions;
			extraLevel = preset.extraLevel;
			disableAutoStruts = false;

			return true;
		}

		public static bool ApplyGameSettings()
		{
			KJRSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KJRSettings>();

			if(settings == null)
				return false;

			bool result =
				   (reinforceAttachNodes != settings.reinforceAttachNodes)
				|| (reinforceInversions != settings.reinforceInversions)
				|| (extraLevel != (!settings.extraJoints ? 0 : settings.extraLevel))
				|| (disableAutoStruts != settings.disableAutoStruts);

			if(!result)
				return false;

			reinforceAttachNodes = settings.reinforceAttachNodes;
			reinforceInversions = settings.reinforceInversions;
			extraLevel = !settings.extraJoints ? 0 : settings.extraLevel;
			disableAutoStruts = settings.disableAutoStruts;

			return true;
		}

		////////////////////////////////////////
		// find part information

		public static float MaximumPossiblePartMass(Part part)
		{
			float maxMass = part.mass;
			foreach(PartResource r in part.Resources)
				maxMass += (float)(r.info.density * r.maxAmount);
			return maxMass;
		}

		////////////////////////////////////////
		// find joint reinforcement information

		public static bool IsJointUnlockable(Part part)
		{
			for(int i = 0; i < part.Modules.Count; i++)
			{
				if(part.Modules[i] is IJointLockState)
					return true;
			}

			return false;
		}

		public static bool IsJointAdjustmentAllowed(Part part)
		{
			IJointLockState jointLockState;

			for(int i = 0; i < part.Modules.Count; i++)
			{
				PartModule module = part.Modules[i];

				jointLockState = module as IJointLockState;
				
				if((jointLockState != null) && (jointLockState.IsJointUnlocked()))
					return false;

				if((module is KerbalEVA)
				|| (module is ModuleWheelBase)
				|| (module is ModuleGrappleNode)
				|| (module is LaunchClamp))
					return false;
			}

			if((part.parent != null) && part.parent.isRobotic())
				return false;

			return true;
		}

		public static void CalculateShadowStrength(Part part, Part connectedPart, int attachNodeSize, out float linearForce, out float torqueForce)
		{
			// "shadow calculation" -> never go below stock

			float stackNodeFactor = 2f;
			float srfNodeFactor = 0.8f;

			float breakingForceModifier = 1f;
			float breakingTorqueModifier = 1f;

			linearForce = Mathf.Min(part.breakingForce, connectedPart.breakingForce) *
				breakingForceModifier *
				(attachNodeSize + 1f) * (part.attachMode == AttachModes.SRF_ATTACH ? srfNodeFactor : stackNodeFactor)
				/ part.attachJoint.joints.Count;

			torqueForce = Mathf.Min(part.breakingTorque, connectedPart.breakingTorque) *
				breakingTorqueModifier *
				(attachNodeSize + 1f) * (part.attachMode == AttachModes.SRF_ATTACH ? srfNodeFactor : stackNodeFactor)
				/ part.attachJoint.joints.Count;
		}

		public static bool CalculateStrength(Part part, Part connectedPart, out float momentOfInertia, out float linearForce, out float torqueForce)
		{
			if(part.attachJoint.Target.RigidBodyPart != connectedPart)
			{
				Logger.Log("CalculateStrength -> connectedPart is not what it is expected to be", Logger.Level.Error);

				momentOfInertia = linearForce = torqueForce = 0f;
				return false;
			}

			AttachNode attachNode = part.FindAttachNodeByPart(part.attachJoint.Target);

			if(attachNode == null)
			{
				Logger.Log("CalculateStrength -> AttachNode not found", Logger.Level.Error);

				momentOfInertia = linearForce = torqueForce = 0f;
				return false;
			}

			float area = attachNode.contactArea;
			momentOfInertia = area * (area / Mathf.PI) / 4; // moment of inertia of cylinder

			// if using volume, raise al stiffness-affecting parameters to the 1.5 power
			if(KJRJointUtils.useVolumeNotArea)
			{
				area = Mathf.Pow(area, 1.5f);
				momentOfInertia = Mathf.Pow(momentOfInertia, 1.5f);
			}

			float breakForce = Math.Min(part.breakingForce, connectedPart.breakingForce) * ((attachNode.nodeType == AttachNode.NodeType.Stack) ? 4f : 1.6f) * KJRJointUtils.breakForceMultiplier;
			float breakTorque = Math.Min(part.breakingTorque, connectedPart.breakingTorque) * ((attachNode.nodeType == AttachNode.NodeType.Stack) ? 4f : 1.6f) * KJRJointUtils.breakTorqueMultiplier;

			linearForce = Mathf.Max(KJRJointUtils.breakStrengthPerArea * area, breakForce);
			torqueForce = Mathf.Max(KJRJointUtils.breakTorquePerMOI * momentOfInertia, breakTorque);

			// "shadow calculation" -> never go below stock
			float defaultLinearForce; float defaultTorqueForce;
			CalculateShadowStrength(part, connectedPart, attachNode.size, out defaultLinearForce, out defaultTorqueForce);

			linearForce = Mathf.Max(linearForce, defaultLinearForce);
			torqueForce = Mathf.Max(torqueForce, defaultTorqueForce);

			return true;
		}

		public static void CalculateOverallStrength(Part part, Part linkPart,
			ref float ang_maximumForce, ref float ang_positionSpring, ref float ang_positionDamper, ref float lin_maximumForce, ref float lin_positionSpring, ref float lin_positionDamper, ref float breakForce, ref float breakTorque)
		{
			if(part == null)
			{
				Logger.Log("CalculateOverallStrength -> part chain not found", Logger.Level.Error);
				return;
			}

			ConfigurableJoint j = part.attachJoint.Joint;

			ang_maximumForce = Mathf.Min(ang_maximumForce, j.angularXDrive.maximumForce);
			ang_positionSpring = Mathf.Min(ang_positionSpring, j.angularXDrive.positionSpring);
			ang_positionDamper = Mathf.Min(ang_positionDamper, j.angularXDrive.positionDamper);
			lin_maximumForce = Mathf.Min(lin_maximumForce, j.xDrive.maximumForce);
			lin_positionSpring = Mathf.Min(lin_positionSpring, j.xDrive.positionSpring);
			lin_positionDamper = Mathf.Min(lin_positionDamper, j.xDrive.positionDamper);

			breakForce = Mathf.Min(breakForce, j.breakForce);
			breakTorque = Mathf.Min(breakTorque, j.breakTorque);

			if(part.parent.RigidBodyPart != linkPart)
				CalculateOverallStrength(part.parent.RigidBodyPart, linkPart,
					ref ang_maximumForce, ref ang_positionSpring, ref ang_positionDamper, ref lin_maximumForce, ref lin_positionSpring, ref lin_positionDamper, ref breakForce, ref breakTorque);
		}

		////////////////////////////////////////
		// find parts

		// creates a link set from the part to its root (or the first parent that cannot be used)
		// the set contains also the part and the root and is intended to be used as input for BuildLinkSetDifference

		public static void BuildLinkSetConditional(Part part, ref List<Part> set)
		{
			while(part != null)
			{
				set.Add(part);
				part = KJRJointUtils.IsJointAdjustmentAllowed(part) ? part.parent : null;
			}
		}

		// creates a link set from the part to the specified root
		// the set contains also the part and the root and is intended to be used as input for BuildLinkSetDifference

		public static void BuildLinkSet(Part part, Part root, ref List<Part> set)
		{
			set.Add(part);
			BuildLinkSetDirect(part.parent, root, ref set);
			set.Add(root);
		}

		// creates a link set from the part to the specified root
		// the result is a usable linkset

		public static void BuildLinkSetDirect(Part part, Part root, ref List<Part> set)
		{
			while(part != root)
			{ set.Add(part); part = part.parent; }
		}

		// creates a link set from two input link sets by finding the common parts
		// the result is a usable linkset

		public static bool BuildLinkSetDifference(ref List<Part> result, ref int root, ref List<Part> set1, ref List<Part> set2)
		{
			int i = set1.Count - 1;
			int j = set2.Count - 1;

			if(set1[i] != set2[j])
				return false; // not same root, so they can never be in a valid set

			while((i >= 0) && (j >= 0) && (set1[i] == set2[j]))
			{ --i; --j; }

			if(i + j < 0)
				return false; // set would be empty

			if(result == null)
				result = new List<Part>(i + 1 + j);
			root = i;

			for(int _i = 1; _i <= i + 1; _i++)
				result.Add(set1[_i]);

			for(int _j = j; _j >= 1; _j--)
				result.Add(set2[_j]);

			return true;
		}

		private static bool FindEndPoints(Part part, ref List<Part> endpoints, ref Dictionary<Part, List<Part>> childPartsToConnectByRoot)
		{
			bool bResult = false;

			foreach(Part child in part.children)
			{
				if(!IsJointAdjustmentAllowed(child))
					FindRootsAndEndPoints(child, ref childPartsToConnectByRoot);
				else
					bResult |= FindEndPoints(child, ref endpoints, ref childPartsToConnectByRoot);
			}

			if(!bResult
			&& (part.rb != null)
			&& IsJointAdjustmentAllowed(part)
			&& (MaximumPossiblePartMass(part) > massForAdjustment))
			{
				endpoints.Add(part);

				bResult = true;
			}

			return bResult;
		}

		public static void FindRootsAndEndPoints(Part part, ref Dictionary<Part, List<Part>> childPartsToConnectByRoot)
		{
			if(part.rb)
			{
				List<Part> _endpoints = new List<Part>();
				childPartsToConnectByRoot.Add(part, _endpoints);

				FindEndPoints(part, ref _endpoints, ref childPartsToConnectByRoot);
			}
			else
			{
				foreach(Part child in part.children)
					FindRootsAndEndPoints(child, ref childPartsToConnectByRoot);
			}
		}

		public class Solution
		{
			public Solution(Part _part, Part _linkPart)
			{
				part = _part;
				linkPart = _linkPart;

				angularForce = float.PositiveInfinity;
				angularSpring = float.PositiveInfinity;
				angularDamper = float.PositiveInfinity;
				linearForce = float.PositiveInfinity;
				linearSpring = float.PositiveInfinity;
				linearDamper = float.PositiveInfinity;
				breakingForce = float.PositiveInfinity;
				breakingTorque = float.PositiveInfinity;
			}

			public Part part;
			public Part linkPart;

			public List<Part> set;
			public int ridx;

			public float angularForce;
			public float angularSpring;
			public float angularDamper;
			public float linearForce;
			public float linearSpring;
			public float linearDamper;
			public float breakingForce;
			public float breakingTorque;
		};

		static int compareSolution(Solution left, Solution right)
		{
			return left.set.Count - right.set.Count;
		}

		// solutions need to be a little bit bigger to work well, because joints between
		// heavy parts tend to break easier than between light parts

		static void verifySolution(Solution s)
		{
			float mass = Mathf.Min(s.part.mass, s.linkPart.mass);
			float betweenMass = float.MaxValue;

			foreach(Part part in s.set)
				betweenMass = Mathf.Min(betweenMass, part.mass);

			float factor = Mathf.Max(mass / (5 * betweenMass), 1f);

			s.breakingForce *= factor;
			s.breakingTorque *= factor;
		}

		// search for a bad mass configuration
		// (a much lighter part on the way up to the root)

		public static bool IsInversion(Part part, out Part parent)
		{
			parent = part;

			while(parent = IsJointAdjustmentAllowed(parent) ? parent.parent : null)
			{
				if(parent.rb != null) // only when physical significant
				{
					if(part.mass > parent.mass * inversionMassFactor)
						return true; // inversion found

					if(parent.mass * solutionMassFactor >= part.mass)
						return false; // heavy parent found -> that's enough as anchor for us
				}
			}

			return false; // no inversion found, but also no realy heavy parent -> that's ok for us
		}

		// search for a part that could be used as anchor to solve the bad mass configuration
		// (a part that is heavy enough on the way up to the root)

		public static bool FindInversionResolution(Part part, Part parent, out Part inversionResolution)
		{
			inversionResolution = parent;

			while(parent = IsJointAdjustmentAllowed(parent) ? parent.parent : null)
			{
				if((parent.rb != null) // only when physical significant
				&& (parent.mass > inversionResolution.mass))
				{
					inversionResolution = parent;

					if(inversionResolution.mass >= part.mass)
						return true; // more than good enough
				}
			}

			return inversionResolution.mass * solutionMassFactor >= part.mass;
		}

		public static bool FindChildInversionResolutions(Part part, Part current, ref List<Part> set)
		{
			if(current == part) // attaching the part to the part itself or a child of it doesn't help to make something more stable
				return set.Count > 0;
			
			if(!IsJointAdjustmentAllowed(current)) // cannot go beyond this part
				return set.Count > 0;

			if((current.rb != null) // only when physical significant
			&& (current.mass * solutionMassFactor >= part.mass))
			{
				set.Add(current);
			}

			for(int i = 0; i < current.children.Count; i++)
				FindChildInversionResolutions(part, current.children[i], ref set);

			return set.Count > 0;
		}

		public static void FindInversionAndResolutions(Part part, ref List<KJRJointUtils.Solution> sols, ref List<Part> unresolved)
		{
			Part parent;

			if((part.rb != null) // only when physical significant
			&& (part.mass + part.GetResourceMass() >= KJRJointUtils.massForAdjustment)
			&& IsInversion(part, out parent))
			{
				Part linkPart;

				if(FindInversionResolution(part, parent, out linkPart))
				{
					Solution sol = new Solution(part, linkPart);

					sol.set = new List<Part>();
					BuildLinkSetDirect(part.parent, linkPart, ref sol.set);

					// calculate the strength of the solution (set of connected parts)

					CalculateOverallStrength(sol.part, sol.linkPart,
						ref sol.angularForce, ref sol.angularSpring, ref sol.angularDamper, ref sol.linearForce, ref sol.linearSpring, ref sol.linearDamper, ref sol.breakingForce, ref sol.breakingTorque);

					verifySolution(sol);

					sols.Add(sol);
				}
				else
					unresolved.Add(part);
			}

			foreach(Part child in part.children)
				FindInversionAndResolutions(child, ref sols, ref unresolved);
		}

		public static void FindChildInversionResolution(Part part, ref List<KJRJointUtils.Solution> sols, ref List<Part> unresolved)
		{
			Part root = part;
			while(root.parent && IsJointAdjustmentAllowed(root))
				root = root.parent;

			tempPartList.Clear();
			if(!FindChildInversionResolutions(part, root, ref tempPartList))
				return;

			tempSet1.Clear();
			BuildLinkSet(part, root, ref tempSet1);

			List<Solution> allSols = new List<Solution>();

			bool onlyResolved = true;
		retry:

			for(int i = 0; i < tempPartList.Count; i++)
			{
				if(onlyResolved &&
					unresolved.Contains(tempPartList[i]))
					continue;

				tempSet2.Clear();
				BuildLinkSet(tempPartList[i], root, ref tempSet2);

				Solution sol = new Solution(part, tempPartList[i]);

				sol.set = null;
				sol.ridx = 0;
				if(!BuildLinkSetDifference(ref sol.set, ref sol.ridx, ref tempSet1, ref tempSet2))
					continue;

				CalculateOverallStrength(sol.part, sol.set[sol.ridx],
					ref sol.angularForce, ref sol.angularSpring, ref sol.angularDamper, ref sol.linearForce, ref sol.linearSpring, ref sol.linearDamper, ref sol.breakingForce, ref sol.breakingTorque);

				CalculateOverallStrength(sol.linkPart, sol.set[sol.ridx],
					ref sol.angularForce, ref sol.angularSpring, ref sol.angularDamper, ref sol.linearForce, ref sol.linearSpring, ref sol.linearDamper, ref sol.breakingForce, ref sol.breakingTorque);

				allSols.Add(sol);
			}

			if(onlyResolved && (allSols.Count == 0))
			{
				onlyResolved = false;
				goto retry;
			}

			allSols.Sort(compareSolution);

			int idx = 0;
			for(int i = 1; i < allSols.Count; i++)
			{
				if(allSols[idx].breakingForce < allSols[i].breakingForce)
					idx = i;
			}

			verifySolution(allSols[idx]);

			sols.Add(allSols[idx]);
		}

		////////////////////////////////////////
		// build joints

		public static ConfigurableJoint BuildJoint(Solution s)
		{
			ConfigurableJoint newJoint;

			// remark: do not reverse the joint / it is essential for a correct handling of breaking joints

			newJoint = s.part.gameObject.AddComponent<ConfigurableJoint>();
			newJoint.connectedBody = s.linkPart.Rigidbody;

			newJoint.anchor = Vector3.zero;

			newJoint.autoConfigureConnectedAnchor = false;
			newJoint.connectedAnchor = Quaternion.Inverse(s.linkPart.orgRot) * (s.part.orgPos - s.linkPart.orgPos);
			newJoint.SetTargetRotationLocal((Quaternion.Inverse(s.part.transform.rotation) * s.linkPart.transform.rotation * (Quaternion.Inverse(s.linkPart.orgRot) * s.part.orgRot)).normalized, Quaternion.identity);

			newJoint.xMotion = newJoint.yMotion = newJoint.zMotion = ConfigurableJointMotion.Limited;
			newJoint.angularXMotion = newJoint.angularYMotion = newJoint.angularZMotion = ConfigurableJointMotion.Limited;

			JointDrive angularDrive = new JointDrive { maximumForce = s.angularForce, positionSpring = s.angularSpring, positionDamper = s.angularDamper };
			newJoint.angularXDrive = newJoint.angularYZDrive = newJoint.slerpDrive = angularDrive;

			JointDrive linearDrive = new JointDrive { maximumForce = s.linearForce, positionSpring = s.linearSpring, positionDamper = s.linearDamper };
			newJoint.xDrive = newJoint.yDrive = newJoint.zDrive = linearDrive;

			newJoint.linearLimit = newJoint.angularYLimit = newJoint.angularZLimit = newJoint.lowAngularXLimit = newJoint.highAngularXLimit
				= new SoftJointLimit { limit = 0, bounciness = 0 };
			newJoint.linearLimitSpring = newJoint.angularYZLimitSpring = newJoint.angularXLimitSpring
				= new SoftJointLimitSpring { spring = 0, damper = 0 };

			SoftJointLimit angularLimit = default(SoftJointLimit);
				angularLimit.limit = 180f;
				angularLimit.bounciness = 0f;

			SoftJointLimitSpring angularLimitSpring = default(SoftJointLimitSpring);
				angularLimitSpring.spring = 0f;
				angularLimitSpring.damper = 0f;

			SoftJointLimit linearJointLimit = default(SoftJointLimit);
				linearJointLimit.limit = 1f;
				linearJointLimit.bounciness = 0f;

			SoftJointLimitSpring linearJointLimitSpring = default(SoftJointLimitSpring);
				linearJointLimitSpring.damper = 0f;
				linearJointLimitSpring.spring = 0f;

			newJoint.rotationDriveMode = RotationDriveMode.XYAndZ;
			newJoint.highAngularXLimit = angularLimit;
			newJoint.lowAngularXLimit = angularLimit;
			newJoint.angularYLimit = angularLimit;
			newJoint.angularZLimit = angularLimit;
			newJoint.angularXLimitSpring = angularLimitSpring;
			newJoint.angularYZLimitSpring = angularLimitSpring;
			newJoint.linearLimit = linearJointLimit;
			newJoint.linearLimitSpring = linearJointLimitSpring;

			newJoint.breakForce = s.breakingForce;
			newJoint.breakTorque = s.breakingTorque;

			return newJoint;
		}

		public static ConfigurableJoint BuildExtraJoint(Part part, Part linkPart)
		{
			ConfigurableJoint newJoint;

			// reverse the joint for even better stability
			if((part.mass < linkPart.mass) && (part.rb != null))
			{ Part t = part; part = linkPart; linkPart = t; }

			newJoint = part.gameObject.AddComponent<ConfigurableJoint>();
			newJoint.connectedBody = linkPart.Rigidbody;

			newJoint.anchor = Vector3.zero;

			newJoint.autoConfigureConnectedAnchor = false;
			newJoint.connectedAnchor = Quaternion.Inverse(linkPart.orgRot) * (part.orgPos - linkPart.orgPos);
			newJoint.SetTargetRotationLocal((Quaternion.Inverse(part.transform.rotation) * linkPart.transform.rotation * (Quaternion.Inverse(linkPart.orgRot) * part.orgRot)).normalized, Quaternion.identity);

			newJoint.xMotion = newJoint.yMotion = newJoint.zMotion = ConfigurableJointMotion.Free;
			newJoint.angularXMotion = newJoint.angularYMotion = newJoint.angularZMotion = ConfigurableJointMotion.Free;

			JointDrive angularDrive = new JointDrive { maximumForce = (extraLevel == 1) ? extraAngularForceW : extraAngularForce, positionSpring = (extraLevel == 1) ? extraAngularSpringW : extraAngularSpring, positionDamper = (extraLevel == 1) ? extraAngularDamperW : extraAngularDamper };
			newJoint.angularXDrive = newJoint.angularYZDrive = angularDrive; 

			JointDrive linearDrive = new JointDrive { maximumForce = (extraLevel == 1) ? extraLinearForceW : extraLinearForce, positionSpring = (extraLevel == 1) ? extraLinearSpringW : extraLinearSpring, positionDamper = (extraLevel == 1) ? extraLinearDamperW : extraLinearDamper };
			newJoint.xDrive = newJoint.yDrive = newJoint.zDrive = linearDrive;

			newJoint.breakForce = extraBreakingForce;
			newJoint.breakTorque = extraBreakingTorque;

			return newJoint;
		}

		public static void ConnectLaunchClampToGround(Part clamp)
		{
			FixedJoint newJoint = clamp.gameObject.AddComponent<FixedJoint>();
			newJoint.connectedBody = null;
			newJoint.breakForce = float.MaxValue;
			newJoint.breakTorque = float.MaxValue;
		}
	}
}
