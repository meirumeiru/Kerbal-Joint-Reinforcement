using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using UnityEngine;
using KSP;
using KSP.IO;

namespace KerbalJointReinforcement
{
	public static class KJRJointUtils
	{
		public static bool loaded = false;

		public static bool reinforceAttachNodes = true;
		public static bool reinforceInversions = true;
		public static bool addExtraStabilityJoints = false;
		public static bool reinforceLaunchClamps = false;

		public static bool useVolumeNotArea = true;
		public static float massForAdjustment = 0.01f;

		// reinforcement settings
		public static float angularDriveSpring = 5e12f;
		public static float angularDriveDamper = 25f;

		public static float breakForceMultiplier = 1f;
		public static float breakTorqueMultiplier = 1f;
		public static float breakStrengthPerArea = 1500f;
		public static float breakTorquePerMOI = 6000f;

		// inversion settings
		public static float inversionMassFactor = 2f;
		public static float solutionMassFactor = 2f;
		public static float jointMassFactor = 8f;

		// extra joint settings
		public static int extraLevel = 0;

		public static float extraLinearForce = PhysicsGlobals.JointForce;
		public static float extraLinearSpring = PhysicsGlobals.JointForce;
		public static float extraLinearDamper = 0f;

		public static float extraAngularForce = PhysicsGlobals.JointForce;
		public static float extraAngularSpring = 60000f;
		public static float extraAngularDamper = 0f;

		public static float extraBreakingForce = float.MaxValue;
		public static float extraBreakingTorque = float.MaxValue;


		public static bool debug = false;


		public static List<Part> tempPartList;
		public static int jc; // FEHLER, temp


		public static void LoadConstants()
		{
			PluginConfiguration config = PluginConfiguration.CreateForType<KJRManager>();
			config.load();

			reinforceAttachNodes = config.GetValue<bool>("reinforceAttachNodes", true);
			reinforceInversions = config.GetValue<bool>("reinforceInversions", true);
			addExtraStabilityJoints = config.GetValue<bool>("addExtraStabilityJoints", false);
			reinforceLaunchClamps = config.GetValue<bool>("reinforceLaunchClamps", false);

			useVolumeNotArea = config.GetValue<bool>("useVolumeNotArea", true);
			massForAdjustment = (float)config.GetValue<double>("massForAdjustment", 0.01f);

			angularDriveSpring = (float)config.GetValue<double>("angularDriveSpring", 5e12f);
			angularDriveDamper = (float)config.GetValue<double>("angularDriveDamper", 25f);

			breakForceMultiplier = (float)config.GetValue<double>("breakForceMultiplier", 1f);
			breakTorqueMultiplier = (float)config.GetValue<double>("breakTorqueMultiplier", 1f);

			breakStrengthPerArea = (float)config.GetValue<double>("breakStrengthPerArea", 1500f);
			breakTorquePerMOI = (float)config.GetValue<double>("breakTorquePerMOI", 6000f);

			inversionMassFactor = (float)config.GetValue<double>("inversionMassFactor", 2f);
			solutionMassFactor = (float)config.GetValue<double>("solutionMassFactor", 2f);
			jointMassFactor = (float)config.GetValue<double>("jointMassFactor", 8f);

			extraLevel = config.GetValue<int>("extraLevel", 0);

			extraLinearForce = (float)config.GetValue<double>("extraLinearForce", PhysicsGlobals.JointForce);
			extraLinearSpring = (float)config.GetValue<double>("extraLinearSpring", PhysicsGlobals.JointForce);
			extraLinearDamper = (float)config.GetValue<double>("extraLinearDamper", 0f);

			extraAngularForce = (float)config.GetValue<double>("extraAngularForce", PhysicsGlobals.JointForce);
			extraAngularSpring = (float)config.GetValue<double>("extraAngularSpring", 60000f);
			extraAngularDamper = (float)config.GetValue<double>("extraAngularDamper", 0f);

			extraBreakingForce = (float)config.GetValue<double>("extraBreakingForce", float.MaxValue);
			extraBreakingTorque = (float)config.GetValue<double>("extraBreakingTorque", float.MaxValue);

			debug = config.GetValue<bool>("debug", false);

#if IncludeAnalyzer

			reinforceAttachNodes = true;
			reinforceInversions = true;
			addExtraStabilityJoints = true;
			reinforceLaunchClamps = true;

#endif

			loaded = true;
		}

		////////////////////////////////////////
		// find part information

		public static float MaximumPossiblePartMass(Part part)
		{
			float maxMass = part.mass;
			foreach(PartResource r in part.Resources)
				maxMass += (float)(r.info.density * r.maxAmount);

			if(debug)
				Debug.Log("KJR: maximum mass for part " + part.partInfo.title + " is " + maxMass);

			return maxMass;
		}

		public static Vector3 CalculateExtents(Part part, Quaternion alignment)
		{
			Vector3 maxBounds = new Vector3(-100, -100, -100);
			Vector3 minBounds = new Vector3(100, 100, 100);

			Matrix4x4 base_matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(alignment), Vector3.one) * part.transform.worldToLocalMatrix;

			// get the max boundaries of the part
			foreach (Transform t in part.FindModelComponents<Transform>())
			{
				MeshFilter mf = t.GetComponent<MeshFilter>();
				if((mf == null) || (mf.sharedMesh == null))
					continue;

				Matrix4x4 matrix = base_matrix * t.transform.localToWorldMatrix;

				foreach(Vector3 vertex in mf.sharedMesh.vertices)
				{
					Vector3 v = matrix.MultiplyPoint3x4(vertex);

					maxBounds.x = Mathf.Max(maxBounds.x, v.x);
					minBounds.x = Mathf.Min(minBounds.x, v.x);
					maxBounds.y = Mathf.Max(maxBounds.y, v.y);
					minBounds.y = Mathf.Min(minBounds.y, v.y);
					maxBounds.z = Mathf.Max(maxBounds.z, v.z);
					minBounds.z = Mathf.Min(minBounds.z, v.z);
				}
			}

			if(maxBounds == new Vector3(-100, -100, -100) && minBounds == new Vector3(100, 100, 100))
			{
				Debug.LogWarning("KJR: extents could not be properly built for part " + part.partInfo.title);
				maxBounds = minBounds = Vector3.zero;
			}
			else if(debug)
				Debug.Log("KJR: extents > " + minBounds + " .. " + maxBounds + " = " + (maxBounds - minBounds));

			return maxBounds - minBounds;
		}

		////////////////////////////////////////
		// find joint reinforcement information

		public static bool IsJointUnlockable(Part p)
		{
			for(int i = 0; i < p.Modules.Count; i++)
			{
				if(p.Modules[i] is IJointLockState)
					return true;
			}

			return false;
		}

		public static bool IsJointAdjustmentAllowed(Part p)
		{
			IJointLockState jointLockState;

			for(int i = 0; i < p.Modules.Count; i++)
			{
				PartModule module = p.Modules[i];

				jointLockState = module as IJointLockState;
				
				if((jointLockState != null) && (jointLockState.IsJointUnlocked()))
					return false;

				if((module is KerbalEVA)
				|| (module is ModuleWheelBase)
				|| (module is ModuleGrappleNode)
				|| (module is LaunchClamp))
					return false;
			}

			if((p.parent != null) && p.parent.isRobotic())
				return false;

			return true;
		}

// default
		public static bool CalculateStrength0(Part part, Part connectedPart, out float momentOfInertia, out float linearForce, out float torqueForce)
		{
			AttachNode an = part.FindAttachNodeByPart(connectedPart);

	float stackNodeFactor = 2f;
	float srfNodeFactor = 0.8f;

	float breakingForceModifier = 1f;
	float breakingTorqueModifier = 1f;

			linearForce = Mathf.Min(part.breakingForce, connectedPart.breakingForce) *
				breakingForceModifier *
				(an.size + 1f) * (part.attachMode == AttachModes.SRF_ATTACH ? srfNodeFactor : stackNodeFactor)
				/ part.attachJoint.joints.Count;

			torqueForce = Mathf.Min(part.breakingTorque, connectedPart.breakingTorque) *
				breakingTorqueModifier *
				(an.size + 1f) * (part.attachMode == AttachModes.SRF_ATTACH ? srfNodeFactor : stackNodeFactor)
				/ part.attachJoint.joints.Count;

			momentOfInertia = 0f; // gibt's nicht hier

			return true;
		}

// Ferram, old KJR
		public static bool CalculateStrength(Part part, Part connectedPart, out float momentOfInertia, out float linearForce, out float torqueForce)
		{
			float partMass = part.mass + part.GetResourceMass();

			float parentMass = connectedPart.mass + connectedPart.GetResourceMass();

			if(partMass < KJRJointUtils.massForAdjustment || parentMass < KJRJointUtils.massForAdjustment)
			{
				if(KJRJointUtils.debug)
					Debug.Log("KJR: part mass too low, skipping: " + part.partInfo.name + " (" + part.flightID + ")");

				momentOfInertia = 0f;
				linearForce = 0f;
				torqueForce = 0f;

				return false;
			}				
			
	// default für stack ist 4, für srf 1.6 -> ich nehm mal fix 4			
KJRJointUtils.breakForceMultiplier = 4f;
KJRJointUtils.breakTorqueMultiplier = 4f;

			float breakForce = Math.Min(part.breakingForce, connectedPart.breakingForce) * KJRJointUtils.breakForceMultiplier;
			float breakTorque = Math.Min(part.breakingTorque, connectedPart.breakingTorque) * KJRJointUtils.breakTorqueMultiplier;

			Quaternion up;
			Vector3 anchor = part.attachJoint.joints[0].anchor;
			for(int i = 1; i < part.attachJoint.joints.Count; i++) anchor += part.attachJoint.joints[i].anchor;

			if(anchor.magnitude > 0.05f)
				up = Quaternion.FromToRotation(Vector3.up, anchor.normalized);
			else if((connectedPart.transform.position - part.transform.position).magnitude > 0.05f)
				up = Quaternion.FromToRotation(Vector3.up, part.transform.InverseTransformDirection((connectedPart.transform.position - part.transform.position)).normalized);
			else
				up = Quaternion.identity;

			Vector3 partExtents = CalculateExtents(part, up);


			Quaternion connectedUp;
			Vector3 connectedAnchor = part.attachJoint.joints[0].connectedAnchor;
			for(int i = 1; i < part.attachJoint.joints.Count; i++) connectedAnchor += part.attachJoint.joints[i].connectedAnchor;

			if(connectedAnchor.magnitude > 0.05f)
				connectedUp = Quaternion.FromToRotation(Vector3.up, connectedAnchor.normalized);
			else if((part.transform.position - connectedPart.transform.position).magnitude > 0.05f)
				connectedUp = Quaternion.FromToRotation(Vector3.up, connectedPart.transform.InverseTransformDirection((part.transform.position - connectedPart.transform.position)).normalized);
			else
				connectedUp = Quaternion.identity;

			Vector3 connectedPartExtents = CalculateExtents(connectedPart, connectedUp);


			float radius = Mathf.Sqrt(partExtents.x * partExtents.z) / 2;

			float connectedRadius = Mathf.Sqrt(connectedPartExtents.x * connectedPartExtents.z) / 2;


			float usedRadius = Mathf.Min(radius, connectedRadius);

			if(usedRadius < 0.001f)
				usedRadius = 0.001f;

			float area = Mathf.PI * usedRadius * usedRadius;			// area of cylinder
			momentOfInertia = area * usedRadius * usedRadius / 4;		// moment of inertia of cylinder

			// if using volume, raise al stiffness-affecting parameters to the 1.5 power
			if(KJRJointUtils.useVolumeNotArea)
			{
				area = Mathf.Pow(area, 1.5f);
				momentOfInertia = Mathf.Pow(momentOfInertia, 1.5f);
			}

			linearForce = Mathf.Max(KJRJointUtils.breakStrengthPerArea * area, breakForce);
			torqueForce = Mathf.Max(KJRJointUtils.breakTorquePerMOI * momentOfInertia, breakTorque);

			return true;
		}

// contactArea
		public static bool CalculateStrength2(Part part, Part connectedPart, out float momentOfInertia, out float linearForce, out float torqueForce)
		{
			AttachNode an2 = part.FindAttachNodeByPart(connectedPart);

			float area = an2.contactArea;
			momentOfInertia = area * (area / Mathf.PI) / 4;		// moment of inertia of cylinder

			// if using volume, raise al stiffness-affecting parameters to the 1.5 power
			if(KJRJointUtils.useVolumeNotArea)
			{
				area = Mathf.Pow(area, 1.5f);
				momentOfInertia = Mathf.Pow(momentOfInertia, 1.5f);
			}


	// default für stack ist 4, für srf 1.6 -> ich nehm mal fix 4			
KJRJointUtils.breakForceMultiplier = 4f;
KJRJointUtils.breakTorqueMultiplier = 4f;

			float breakForce = Math.Min(part.breakingForce, connectedPart.breakingForce) * KJRJointUtils.breakForceMultiplier;
			float breakTorque = Math.Min(part.breakingTorque, connectedPart.breakingTorque) * KJRJointUtils.breakTorqueMultiplier;


			linearForce = Mathf.Max(KJRJointUtils.breakStrengthPerArea * area, breakForce);
			torqueForce = Mathf.Max(KJRJointUtils.breakTorquePerMOI * momentOfInertia, breakTorque);


// shadow -> never go below stock

	float stackNodeFactor = 2f;
	float srfNodeFactor = 0.8f;

	float breakingForceModifier = 1f;
	float breakingTorqueModifier = 1f;

			float defaultLinearForce = Mathf.Min(part.breakingForce, connectedPart.breakingForce) *
				breakingForceModifier *
				(an2.size + 1f) * (part.attachMode == AttachModes.SRF_ATTACH ? srfNodeFactor : stackNodeFactor)
				/ part.attachJoint.joints.Count;

			float defaultTorqueForce = Mathf.Min(part.breakingTorque, connectedPart.breakingTorque) *
				breakingTorqueModifier *
				(an2.size + 1f) * (part.attachMode == AttachModes.SRF_ATTACH ? srfNodeFactor : stackNodeFactor)
				/ part.attachJoint.joints.Count;

			linearForce = Mathf.Max(linearForce, defaultLinearForce);
			torqueForce = Mathf.Max(torqueForce, defaultTorqueForce);

			return true;
		}

		public static void CalculateOverallStrength(Part part, Part linkPart,
			ref float ang_maximumForce, ref float ang_positionSpring, ref float ang_positionDamper, ref float lin_maximumForce, ref float lin_positionSpring, ref float lin_positionDamper, ref float breakForce, ref float breakTorque)
		{
			if(part == null)
			{
				Debug.LogError("KJR: CalculateOverallStrength -> part chain not found");
				return;
			}

			ConfigurableJoint j = part.attachJoint.joints[0];

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
//			do { set.Add(part); part = part.parent; } while(part != root);
//			set.Add(part);

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

			result = new List<Part>(i + 1 + j);
			root = i;

			for(int _i = 1; _i <= i + 1; _i++)
				result.Add(set1[_i]);

			for(int _j = j; _j >= 1; _j--)
				result.Add(set1[_j]);

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

		public class Sol2
		{
			public Sol2(Part _part, Part _linkPart)
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

		static int compareSol(Sol2 left, Sol2 right)
		{
			return left.set.Count - right.set.Count;
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
						return true; // good enough
				}
			}

			return inversionResolution.mass * solutionMassFactor >= part.mass;
		}

		public static bool FindChildInversionResolutions(Part part, Part current, ref List<Part> set)
		{
			if(current == part)
				return set.Count > 0; // ich selber und alles was an mir hängt ist keine Lösung um meine Verbindung zum Schiff stabiler zu machen

			if(!IsJointAdjustmentAllowed(current))
				return set.Count > 0; // weil, weiter geht's nicht

			if((current.rb != null) // only when physical significant
			&& (current.mass * solutionMassFactor >= part.mass))
			{
				set.Add(current);
			}

			for(int i = 0; i < current.children.Count; i++)
				FindChildInversionResolutions(part, current.children[i], ref set);

			return set.Count > 0;
		}

		public static void FindInversionAndResolutions(Part part, ref List<KJRJointUtils.Sol2> sols, ref List<Part> unresolved)
		{
			Part parent;

			if((part.rb != null) // only when physical significant
			&& (part.mass + part.GetResourceMass() >= KJRJointUtils.massForAdjustment)
			&& IsInversion(part, out parent))
			{
				Part linkPart;

				if(FindInversionResolution(part, parent, out linkPart))
				{
					Sol2 sol = new Sol2(part, linkPart);

					sol.set = new List<Part>();
					BuildLinkSetDirect(part.parent, linkPart, ref sol.set);

					// hab jetzt das, jetzt bau ich mir die Stärke davon

					CalculateOverallStrength(sol.part, sol.linkPart,
						ref sol.angularForce, ref sol.angularSpring, ref sol.angularDamper, ref sol.linearForce, ref sol.linearSpring, ref sol.linearDamper, ref sol.breakingForce, ref sol.breakingTorque);

					sols.Add(sol);
				}
				else
					unresolved.Add(part);
			}

			foreach(Part child in part.children)
				FindInversionAndResolutions(child, ref sols, ref unresolved);
		}

		public static void FindChildInversionResolution(Part part, ref List<KJRJointUtils.Sol2> sols, ref List<Part> unresolved)
		{
			Part root = part;
			while(root.parent && IsJointAdjustmentAllowed(root))
				root = root.parent;

			if(!FindChildInversionResolutions(part, root, ref tempPartList))
				return; // hat keinen Sinn

			List<Part> set1 = new List<Part>();
			BuildLinkSet(part, root, ref set1);

			List<Part> set2 = new List<Part>();

			List<Sol2> allSols = new List<Sol2>();

			bool onlyResolved = true;
		retry:

			for(int i = 0; i < tempPartList.Count; i++)
			{
				if(onlyResolved &&
					unresolved.Contains(tempPartList[i]))
					continue;

				set2.Clear();
				BuildLinkSet(tempPartList[i], root, ref set2);

				Sol2 sol = new Sol2(part, tempPartList[i]);

				sol.set = null;
				sol.ridx = 0;
				if(!BuildLinkSetDifference(ref sol.set, ref sol.ridx, ref set1, ref set2))
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

			// habe alle sol's... jetzt rechnen, welche ich nehme

			allSols.Sort(compareSol);

			int idx = 0;
			for(int i = 1; i < allSols.Count; i++)
			{
				if(allSols[idx].breakingForce < allSols[i].breakingForce)	// FEHLER, oder nach Länge beurteilen? oder die anderen Lösungen nicht nach Gewicht, sondern auch nach force? oder hier noch nach Gewicht urteilen?
					idx = i;
			}

			sols.Add(allSols[idx]);
		}

		////////////////////////////////////////
		// build joints

		public static ConfigurableJoint BuildJoint(Sol2 s)
		{
			++jc;

			ConfigurableJoint newJoint;

			// remark: do not reverse the joint / it is essential for a correct handling of breaking joints

			newJoint = s.part.gameObject.AddComponent<ConfigurableJoint>();
			newJoint.connectedBody = s.linkPart.Rigidbody;

			newJoint.anchor = Vector3.zero;

			newJoint.autoConfigureConnectedAnchor = false;
			newJoint.connectedAnchor = Quaternion.Inverse(s.linkPart.orgRot) * (s.part.orgPos - s.linkPart.orgPos);
			newJoint.SetTargetRotationLocal((Quaternion.Inverse(s.part.transform.rotation) * s.linkPart.transform.rotation * (Quaternion.Inverse(s.linkPart.orgRot) * s.part.orgRot)).normalized, Quaternion.identity);

			newJoint.xMotion = newJoint.yMotion = newJoint.zMotion = ConfigurableJointMotion.Limited;
			newJoint.angularYMotion = newJoint.angularZMotion = newJoint.angularXMotion = ConfigurableJointMotion.Limited;

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
			++jc;

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
			newJoint.angularYMotion = newJoint.angularZMotion = newJoint.angularXMotion = ConfigurableJointMotion.Free;

			JointDrive angularDrive = new JointDrive { maximumForce = (extraLevel == 0) ? 10f : extraAngularForce, positionSpring = extraAngularSpring, positionDamper = extraAngularDamper };
			newJoint.angularXDrive = newJoint.angularYZDrive = angularDrive; 

			JointDrive linearDrive = new JointDrive { maximumForce = (extraLevel == 0) ? 10f : extraLinearForce, positionSpring = extraLinearSpring, positionDamper = extraLinearDamper };
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




















		public static Vector3 GuessUpVector(Part part)
		{
			// for intakes, use the intake vector
			if(part.Modules.Contains<ModuleResourceIntake>())
			{
				ModuleResourceIntake i = part.Modules.GetModule<ModuleResourceIntake>();
				Transform intakeTrans = part.FindModelTransform(i.intakeTransformName);
				return part.transform.InverseTransformDirection(intakeTrans.forward);
			}

			// if surface attachable, and node normal is up, check stack nodes or use forward
			else if(part.srfAttachNode != null &&
					 part.attachRules.srfAttach &&
					 Mathf.Abs(part.srfAttachNode.orientation.normalized.y) > 0.9f)
			{
				// when the node normal is exactly Vector3.up, the editor orients forward along the craft axis
				Vector3 dir = Vector3.forward;
				bool first = true;

				foreach(AttachNode node in part.attachNodes)
				{
					// doesn't seem to ever happen, but anyway
					if(node.nodeType == AttachNode.NodeType.Surface)
						continue;

					// if all node orientations agree, use that axis
					if(first)
					{
						first = false;
						dir = node.orientation.normalized;
					}
					// conflicting node directions - bail out
					else if(Mathf.Abs(Vector3.Dot(dir, node.orientation.normalized)) < 0.9f)
						return Vector3.up;
				}

				if(debug)
					MonoBehaviour.print(part.partInfo.title + ": Choosing axis " + dir + " for KJR surface attach" + (first ? "" : " from node") + ".");

				return dir;
			}
			else
				return Vector3.up;
		}

		public static Vector3 CalculateExtents(Part p, Vector3 up)
		{
			up = up.normalized;

			// align y axis of the result to the 'up' vector in local coordinate space
			if(Mathf.Abs(up.y) < 0.9f)
				return CalculateExtents(p, Quaternion.FromToRotation(Vector3.up, up));

			return CalculateExtents(p, Quaternion.identity);
		}

		public static Vector3 CalculateExtents(Part p, Vector3 up, Vector3 forward)
		{
			// adjust forward to be orthogonal to up; LookRotation might do the opposite
			Vector3.OrthoNormalize(ref up, ref forward);

			// align y to up and z to forward in local coordinate space
			return CalculateExtents(p, Quaternion.LookRotation(forward, up));
		}

		public static float CalculateRadius(Part p, Vector3 attachNodeLoc)
		{
			// y along attachNodeLoc; x,z orthogonal
			Vector3 maxExtents = CalculateExtents(p, attachNodeLoc);

			// equivalent radius of an ellipse painted into the rectangle
			return Mathf.Sqrt(maxExtents.x * maxExtents.z) / 2;
		}

		public static float CalculateSideArea(Part p, Vector3 attachNodeLoc)
		{
			Vector3 maxExtents = CalculateExtents(p, attachNodeLoc);
			// maxExtents = Vector3.Exclude(maxExtents, Vector3.up);

			return maxExtents.x * maxExtents.z;
		}



	}
}
