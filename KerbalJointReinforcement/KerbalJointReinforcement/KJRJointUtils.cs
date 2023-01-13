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
		public static bool reinforceAttachNodes = false;
		public static bool multiPartAttachNodeReinforcement = true;
		public static bool reinforceDecouplersFurther = false;
		public static bool reinforceLaunchClampsFurther = false;
		public static bool useVolumeNotArea = false;
		public static float massForAdjustment = 0.001f;
		public static float stiffeningExtensionMassRatioThreshold = 5;
		public static float decouplerAndClampJointStrength = float.PositiveInfinity;

		public static float angularDriveSpring = 0;
		public static float angularDriveDamper = 0;

		public static float breakForceMultiplier = 1;
		public static float breakTorqueMultiplier = 1;
		public static float breakStrengthPerArea = 40;
		public static float breakTorquePerMOI = 40000;

		public static bool debug = false;

		public static void LoadConstants()
		{
			PluginConfiguration config = PluginConfiguration.CreateForType<KJRManager>();
			config.load();

			reinforceAttachNodes = config.GetValue<bool>("reinforceAttachNodes", true);
			multiPartAttachNodeReinforcement = config.GetValue<bool>("multiPartAttachNodeReinforcement", true);
			reinforceDecouplersFurther = config.GetValue<bool>("reinforceDecouplersFurther", true);
			reinforceLaunchClampsFurther = config.GetValue<bool>("reinforceLaunchClampsFurther", true);
			useVolumeNotArea = config.GetValue<bool>("useVolumeNotArea", true);

			massForAdjustment = (float)config.GetValue<double>("massForAdjustment", 1);
			stiffeningExtensionMassRatioThreshold = (float)config.GetValue<double>("stiffeningExtensionMassRatioThreshold", 5);

			decouplerAndClampJointStrength = (float)config.GetValue<double>("decouplerAndClampJointStrength", float.PositiveInfinity);
			if(decouplerAndClampJointStrength < 0)
				decouplerAndClampJointStrength = float.PositiveInfinity;

			angularDriveSpring = (float)config.GetValue<double>("angularDriveSpring");
			angularDriveDamper = (float)config.GetValue<double>("angularDriveDamper");

			breakForceMultiplier = (float)config.GetValue<double>("breakForceMultiplier", 1);
			breakTorqueMultiplier = (float)config.GetValue<double>("breakTorqueMultiplier", 1);

			breakStrengthPerArea = (float)config.GetValue<double>("breakStrengthPerArea", 40);
			breakTorquePerMOI = (float)config.GetValue<double>("breakTorquePerMOI", 40000);

			debug = config.GetValue<bool>("debug", false);

			if(debug)
			{
				StringBuilder debugString = new StringBuilder();
				debugString.AppendLine("\n\rAngular Drive: \n\rSpring: " + angularDriveSpring + "\n\rDamp: " + angularDriveDamper);

				debugString.AppendLine("\n\rJoint Strength Multipliers: \n\rForce Multiplier: " + breakForceMultiplier + "\n\rTorque Multiplier: " + breakTorqueMultiplier);
				debugString.AppendLine("Joint Force Strength Per Unit Area: " + breakStrengthPerArea);
				debugString.AppendLine("Joint Torque Strength Per Unit MOI: " + breakTorquePerMOI);

				debugString.AppendLine("Strength For Additional Decoupler And Clamp Joints: " + decouplerAndClampJointStrength);

				debugString.AppendLine("\n\rDebug Output: " + debug);
				debugString.AppendLine("Reinforce Attach Nodes: " + reinforceAttachNodes);
				debugString.AppendLine("Reinforce Decouplers Further: " + reinforceDecouplersFurther);
				debugString.AppendLine("Reinforce Launch Clamps Further: " + reinforceLaunchClampsFurther);
				debugString.AppendLine("Use Volume For Calculations, Not Area: " + useVolumeNotArea);

				debugString.AppendLine("\n\rMass For Joint Adjustment: " + massForAdjustment);

				debugString.AppendLine("\n\rDecoupler Stiffening Extension Mass Ratio Threshold: " + stiffeningExtensionMassRatioThreshold);

				Debug.Log(debugString.ToString());
			}
		}

		////////////////////////////////////////
		// find part information

		public static float MaximumPossiblePartMass(Part p)
		{
			float maxMass = p.mass;
			foreach(PartResource r in p.Resources)
				maxMass += (float)(r.info.density * r.maxAmount);

			if(debug)
				Debug.Log("Maximum mass for part " + p.partInfo.title + " is " + maxMass);
			return maxMass;
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

		public static Vector3 CalculateExtents(Part p, Quaternion alignment)
		{
			Vector3 maxBounds = new Vector3(-100, -100, -100);
			Vector3 minBounds = new Vector3(100, 100, 100);

			// alignment transforms from our desired rotation to the local coordinates, so inverse needed
			Matrix4x4 rotation = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(alignment), Vector3.one);
			Matrix4x4 base_matrix = rotation * p.transform.worldToLocalMatrix;

			// get the max boundaries of the part
			foreach (Transform t in p.FindModelComponents<Transform>())
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
				Debug.LogWarning("KerbalJointReinforcement: extents could not be properly built for part " + p.partInfo.title);
				maxBounds = minBounds = Vector3.zero;
			}
			else if(debug)
				Debug.Log("Extents: " + minBounds + " .. " + maxBounds + " = " + (maxBounds - minBounds));

			// attachNodeLoc = p.transform.worldToLocalMatrix.MultiplyVector(p.parent.transform.position - p.transform.position);
			return maxBounds - minBounds;
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
				jointLockState = p.Modules[i] as IJointLockState;
				
				if((jointLockState != null) && (jointLockState.IsJointUnlocked()))
					return false;

				PartModule module = p.Modules[i];

				if((module is KerbalEVA)
				|| (module is ModuleWheelBase)
				|| (module is ModuleGrappleNode))
					return false;
			}

			if((p.parent != null) && p.parent.isRobotic())
				return false;

			return true;
		}

		public static List<Part> DecouplerPartStiffeningListParents(Part p)
		{
			List<Part> tmpPartList = new List<Part>();

			// non-physical parts are skipped over by attachJoints, so do the same
			bool extend = (p.physicalSignificance == Part.PhysicalSignificance.NONE);

			List<Part> newAdditions = new List<Part>();

			if(extend)
			{
				if(p.parent && IsJointAdjustmentAllowed(p))
					newAdditions.AddRange(DecouplerPartStiffeningListParents(p.parent));
			}
			else
			{
				float thisPartMaxMass = MaximumPossiblePartMass(p);

				if(p.parent && IsJointAdjustmentAllowed(p))
				{
					float massRatio = MaximumPossiblePartMass(p.parent) / thisPartMaxMass;
					//if(massRatio < 1)
					//	massRatio = 1 / massRatio;

					if(massRatio > stiffeningExtensionMassRatioThreshold)
					{
						newAdditions.Add(p.parent);
						if(debug)
							Debug.Log("Part " + p.parent.partInfo.title + " added to list due to mass ratio difference");
					}
				}
			}

			if(newAdditions.Count > 0)
				tmpPartList.AddRange(newAdditions);
			else
				extend = false;

			if(!extend)
				tmpPartList.Add(p);

			return tmpPartList;
		}

		public static List<Part> DecouplerPartStiffeningListChildren(Part p)
		{
			List<Part> tmpPartList = new List<Part>();

			// non-physical parts are skipped over by attachJoints, so do the same
			bool extend = (p.physicalSignificance == Part.PhysicalSignificance.NONE);

			List<Part> newAdditions = new List<Part>();

			if(extend)
			{
				if(p.children != null)
				{
					foreach(Part q in p.children)
					{
						if(q != null && q.parent == p && IsJointAdjustmentAllowed(q))
							newAdditions.AddRange(DecouplerPartStiffeningListChildren(q));
					}
				}
			}
			else
			{
				float thisPartMaxMass = MaximumPossiblePartMass(p);
				if(p.children != null)
					foreach(Part q in p.children)
					{
						if(q != null && q.parent == p && IsJointAdjustmentAllowed(q))
						{
							float massRatio = MaximumPossiblePartMass(q) / thisPartMaxMass;
							//if(massRatio < 1)
							//	massRatio = 1 / massRatio;

							if(massRatio > stiffeningExtensionMassRatioThreshold)
							{
								newAdditions.Add(q);
								if(debug)
									Debug.Log("Part " + q.partInfo.title + " added to list due to mass ratio difference");
							}
						}
					}
			}

			if(newAdditions.Count > 0)
				tmpPartList.AddRange(newAdditions);
			else
				extend = false;

			if(!extend)
				tmpPartList.Add(p);

			return tmpPartList;
		}

		////////////////////////////////////////
		// functions

		public static ConfigurableJoint BuildJoint(Part p, Part linkPart, JointDrive xDrive, JointDrive yDrive, JointDrive zDrive, JointDrive angularDrive, float linearStrength, float angularStrength)
		{
			ConfigurableJoint newJoint;

			if ((p.mass < linkPart.mass) && (p.rb != null))
			{ Part t = p; p = linkPart; linkPart = t; }

			newJoint = p.gameObject.AddComponent<ConfigurableJoint>();
			newJoint.connectedBody = linkPart.Rigidbody;

			newJoint.anchor = Vector3.zero;

			newJoint.autoConfigureConnectedAnchor = false;
			newJoint.connectedAnchor = Quaternion.Inverse(linkPart.orgRot) * (p.orgPos - linkPart.orgPos);
			newJoint.SetTargetRotationLocal(Quaternion.Inverse(p.transform.rotation) * linkPart.transform.rotation * (Quaternion.Inverse(linkPart.orgRot) * p.orgRot), Quaternion.identity);

			newJoint.xMotion = newJoint.yMotion = newJoint.zMotion = ConfigurableJointMotion.Free;
			newJoint.angularYMotion = newJoint.angularZMotion = newJoint.angularXMotion = ConfigurableJointMotion.Free;

			newJoint.xDrive = xDrive;  newJoint.yDrive = yDrive; newJoint.zDrive = zDrive; 
			newJoint.angularXDrive = newJoint.angularYZDrive = angularDrive; 

			newJoint.breakForce = linearStrength;
			newJoint.breakTorque = angularStrength;

			return newJoint;
		}

		public static ConfigurableJoint BuildJoint(Part p, Part linkPart)
		{
			JointDrive linearDrive = new JointDrive { maximumForce = PhysicsGlobals.JointForce, positionSpring = PhysicsGlobals.JointForce };

			return BuildJoint(p, linkPart,
				linearDrive, linearDrive, linearDrive,
				new JointDrive { maximumForce = PhysicsGlobals.JointForce, positionSpring = 60000f },
				KJRJointUtils.decouplerAndClampJointStrength, KJRJointUtils.decouplerAndClampJointStrength);
		}

		public static void ConnectLaunchClampToGround(Part clamp)
		{
			float breakForce = Mathf.Infinity;
			float breakTorque = Mathf.Infinity;
			FixedJoint newJoint;

			newJoint = clamp.gameObject.AddComponent<FixedJoint>();

			newJoint.connectedBody = null;
			newJoint.anchor = Vector3.zero;
			newJoint.axis = Vector3.up;
			//newJoint.secondaryAxis = Vector3.forward;
			newJoint.breakForce = breakForce;
			newJoint.breakTorque = breakTorque;

			//newJoint.xMotion = newJoint.yMotion = newJoint.zMotion = ConfigurableJointMotion.Locked;
			//newJoint.angularXMotion = newJoint.angularYMotion = newJoint.angularZMotion = ConfigurableJointMotion.Locked;
		}
	}
}
