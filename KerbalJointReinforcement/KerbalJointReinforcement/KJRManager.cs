using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CompoundParts;

namespace KerbalJointReinforcement
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class KJRManager : MonoBehaviour
	{
		private static KJRManager _instance;

		internal static KJRManager Instance
		{
			get { return _instance; }
		}

		private List<Vessel> updatedVessels;
		private HashSet<Vessel> easingVessels;
		private List<Vessel> updatingVessels;
		private List<Vessel> constructingVessels;

		private KJRJointTracker jointTracker;

		private List<Part> tempPartList;
		private List<Part> tempSet1;
		private List<Part> tempSet2;
		private List<Part> tempResultSet;

		internal KJRJointTracker GetJointTracker()
		{
			return jointTracker;
		}

		public void Awake()
		{
			KJRJointUtils.LoadDefaults();
			KJRJointUtils.ApplyGameSettings();

			updatedVessels = new List<Vessel>();
			easingVessels = new HashSet<Vessel>();
			updatingVessels = new List<Vessel>();
			constructingVessels = new List<Vessel>();

			jointTracker = new KJRJointTracker();

			tempPartList = new List<Part>();
			tempSet1 = new List<Part>();
			tempSet2 = new List<Part>();
			tempResultSet = new List<Part>();

			_instance = this;
		}

		public void Start()
		{
			GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);
			GameEvents.onGameUnpause.Add(OnGameUnpause);

			GameEvents.onVesselCreate.Add(OnVesselCreate);
			GameEvents.onVesselWasModified.Add(OnVesselWasModified);
			GameEvents.onVesselDestroy.Add(OnVesselDestroy); // maybe use onAboutToDestroy instead?? -> doesn't seem to have a benefit

			GameEvents.onVesselGoOffRails.Add(OnVesselOffRails);
			GameEvents.onVesselGoOnRails.Add(OnVesselOnRails);

			GameEvents.onPartDestroyed.Add(RemovePartJoints);
			GameEvents.onPartDie.Add(RemovePartJoints);
			GameEvents.onPartDeCouple.Add(RemovePartJoints);

			GameEvents.onPhysicsEaseStart.Add(OnEaseStart);
			GameEvents.onPhysicsEaseStop.Add(OnEaseStop);

			GameEvents.onRoboticPartLockChanging.Add(OnRoboticPartLockChanging);

			GameEvents.OnEVAConstructionMode.Add(OnEVAConstructionMode);
			GameEvents.OnEVAConstructionModePartAttached.Add(OnEVAConstructionModePartAttached);
			GameEvents.OnEVAConstructionModePartDetached.Add(OnEVAConstructionModePartDetached);
		}

		public void OnDestroy()
		{
			GameEvents.OnGameSettingsApplied.Remove(OnGameSettingsApplied);
			GameEvents.onGameUnpause.Remove(OnGameUnpause);

			GameEvents.onVesselCreate.Remove(OnVesselCreate);
			GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
			GameEvents.onVesselDestroy.Remove(OnVesselDestroy);

			GameEvents.onVesselGoOffRails.Remove(OnVesselOffRails);
			GameEvents.onVesselGoOnRails.Remove(OnVesselOnRails);

			GameEvents.onPartDestroyed.Remove(RemovePartJoints);
			GameEvents.onPartDie.Remove(RemovePartJoints);
			GameEvents.onPartDeCouple.Remove(RemovePartJoints);

			GameEvents.onPhysicsEaseStart.Remove(OnEaseStart);
			GameEvents.onPhysicsEaseStop.Remove(OnEaseStop);

			GameEvents.onRoboticPartLockChanging.Remove(OnRoboticPartLockChanging);

			GameEvents.OnEVAConstructionMode.Remove(OnEVAConstructionMode);
			GameEvents.OnEVAConstructionModePartAttached.Remove(OnEVAConstructionModePartAttached);
			GameEvents.OnEVAConstructionModePartDetached.Remove(OnEVAConstructionModePartDetached);
		}

		IEnumerator RunVesselJointUpdateFunctionDelayed(Vessel v)
		{
			yield return new WaitForFixedUpdate();

			if(!EVAConstructionModeController.Instance.IsOpen || (EVAConstructionModeController.Instance.panelMode != EVAConstructionModeController.PanelMode.Construction))
			{
				updatingVessels.Remove(v);

				RunVesselJointUpdateFunction(v);

			//	foreach(Part p in v.Parts)
			//		p.ReleaseAutoStruts();

#if IncludeAnalyzer
				KJRAnalyzer.WasModified(v);
#endif
			}
		}

		private void OnGameSettingsApplied()
		{
			if(!KJRJointUtils.ApplyGameSettings())
				return;

			UpdateAllVessels();
		}

		private bool runUpdateAfterPause = false;

		public void UpdateAllVessels()
		{
			if(FlightDriver.Pause)
				runUpdateAfterPause = true;
			else
			{
				runUpdateAfterPause = false;

				foreach(Vessel v in FlightGlobals.VesselsLoaded)
					_instance.OnVesselWasModified(v);
			}
		}

		private void OnGameUnpause()
		{
			if(runUpdateAfterPause)
				UpdateAllVessels();
		}

		private void OnVesselCreate(Vessel v)
		{
			jointTracker.RemoveAllVesselJoints(v);

			updatedVessels.Remove(v);

#if IncludeAnalyzer
			KJRAnalyzer.WasModified(v);
#endif
		}

		internal void OnVesselWasModified(Vessel v)
		{
			if((object)v == null || v.isEVA || v.GetComponent<KerbalEVA>())
				return; 

			if(!v.rootPart.started) // some mods seem to trigger this call too early -> we ignore those calls
				return;

			jointTracker.RemoveAllVesselJoints(v);
			updatedVessels.Remove(v);

			if(!updatingVessels.Contains(v))
			{
				updatingVessels.Add(v);
				StartCoroutine(RunVesselJointUpdateFunctionDelayed(v));
			}
		}

		private void OnVesselDestroy(Vessel v)
		{
			easingVessels.Remove(v);

			updatedVessels.Remove(v);

#if IncludeAnalyzer
			KJRAnalyzer.Clear(v);
#endif
		}

		private void OnRoboticPartLockChanging(Part p, bool b)
		{
			OnVesselWasModified(p.vessel);
		}

		private void OnEVAConstructionMode(bool active)
		{
			if(!active)
            {
				foreach(Vessel v in constructingVessels)
					OnVesselWasModified(v);
				constructingVessels.Clear();
            }
		}

		private void OnEVAConstructionModePartAttached(Vessel v, Part p)
		{
			if(!constructingVessels.Contains(v))
				constructingVessels.Add(v);
		}

		private void OnEVAConstructionModePartDetached(Vessel v, Part p)
		{
			jointTracker.RemovePartJoints(p);

			if(!constructingVessels.Contains(v))
				constructingVessels.Add(v);
		}

		// this function can be called by compatible modules instead of calling
		// Vessel.CycleAllAutoStrut, if you want only KJR to cycle the extra joints
		public static void CycleAllAutoStrut(Vessel v)
		{
			_instance.OnVesselWasModified(v);
		}

		private void OnVesselOnRails(Vessel v)
		{
			if((object)v == null)
				return;

			if(updatedVessels.Contains(v))
			{
				jointTracker.RemoveAllVesselJoints(v);

				updatedVessels.Remove(v);
			}
		}

		private void OnVesselOffRails(Vessel v)
		{
			if((object)v == null || v.isEVA || v.GetComponent<KerbalEVA>())
				return; 

			if(!updatingVessels.Contains(v))
			{
				updatingVessels.Add(v);
				StartCoroutine(RunVesselJointUpdateFunctionDelayed(v));
			}
		}

		private void RemovePartJoints(Part p)
		{
			jointTracker.RemovePartJoints(p);
		}

		public void OnEaseStart(Vessel v)
		{
			if(KJRJointUtils.debug)
				Logger.Log("Easing " + v.vesselName, Logger.Level.Info);

			foreach(Part p in v.Parts)
			{
				if(KJRJointUtils.IsJointUnlockable(p))
					continue; // exclude those actions from joints that can be dynamically unlocked

				p.crashTolerance = p.crashTolerance * 10000f;
				if(p.attachJoint)
					p.attachJoint.SetUnbreakable(true, false);

				Joint[] partJoints = p.GetComponents<Joint>();

				if(p.Modules.Contains<LaunchClamp>())
				{
					for(int j = 0; j < partJoints.Length; j++)
						if(partJoints[j].connectedBody == null)
						{
							GameObject.Destroy(partJoints[j]);
							KJRJointUtils.ConnectLaunchClampToGround(p);
							break;
						}
				}
			}

			easingVessels.Add(v);
		}

		public void OnEaseStop(Vessel v)
		{
			if(!easingVessels.Contains(v))
				return; // we expect, that in this case, we are in an OnDestroy and should not get this call at all

			foreach(Part p in v.Parts)
			{
				if(KJRJointUtils.IsJointUnlockable(p))
					continue; // exclude those actions from joints that can be dynamically unlocked

				p.crashTolerance = p.crashTolerance / 10000f;
				if(p.attachJoint)
					p.attachJoint.SetUnbreakable(false, false);
			}

			if(!updatingVessels.Contains(v))
			{
				updatingVessels.Add(v);
				StartCoroutine(RunVesselJointUpdateFunctionDelayed(v));
			}
		}

		private void RunVesselJointUpdateFunction(Vessel v)
		{
			if(KJRJointUtils.debug)
			{
				Logger.Log("Processing vessel " + v.id + " (" + v.GetName() + "); root " +
							v.rootPart.partInfo.name + " (" + v.rootPart.flightID + ")", Logger.Level.Info);
			}

			bool bReinforced = false;

			KJRJointUtils.tempPartList = tempPartList;
			KJRJointUtils.tempSet1 = tempSet1;
			KJRJointUtils.tempSet2 = tempSet2;

			foreach(Part p in v.Parts)
			{
				KJRDockingNode.InitializePart(p);

				if(KJRJointUtils.reinforceAttachNodes)
				{
					if((p.parent != null) && (p.physicalSignificance == Part.PhysicalSignificance.FULL))
					{
						bReinforced = true;
						ReinforceAttachJoints(p);
					}
				}

				if(KJRJointUtils.reinforceLaunchClamps)
				{
					if(p.parent && p.GetComponent<LaunchClamp>())
					{
						ReinforceLaunchClamps(p);
					}
				}
			}

			if(KJRJointUtils.reinforceInversions)
			{
				ReinforceInversions(v);
				bReinforced = true;
			}

			if(KJRJointUtils.extraLevel > 0)
			{
				if(KJRJointUtils.extraLevel >= 3)
					AdditionalJointsToParent(v);

				AdditionalJointsBetweenEndpoints(v);
				bReinforced = true;
			}

			if(bReinforced && !updatedVessels.Contains(v))
				updatedVessels.Add(v);
		}

#if IncludeAnalyzer
		bool _late = false;

		public void FixedUpdate()
		{
			_late = true;
		}

		public void LateUpdate()
		{
			if(_late)
			{
				if(FlightGlobals.ready && FlightGlobals.Vessels != null)
				{
					KJRAnalyzer.LateUpdate();
				}

				_late = false;
			}
	   }
#endif

		// attachJoint's are always joints from a part to its parent
		private void ReinforceAttachJoints(Part p)
		{
			if(p.rb == null || p.attachJoint == null || !KJRJointUtils.IsJointAdjustmentAllowed(p))
				return;

			if(KJRJointUtils.debug && (p.attachMethod == AttachNodeMethod.LOCKED_JOINT))
			{
				Logger.Log("Already processed part before: " + p.partInfo.name + " (" + p.flightID + ") -> " +
							p.parent.partInfo.name + " (" + p.parent.flightID + ")", Logger.Level.Warning);
			}

			if(!KJRJointUtils.IsJointUnlockable(p)) // exclude those actions from joints that can be dynamically unlocked
			{
				float partMass = p.mass + p.GetResourceMass();

				ConfigurableJoint j = p.attachJoint.Joint;

				if(j == null)
					return;

				Part connectedPart = p.attachJoint.Target.RigidBodyPart;

				float parentMass = connectedPart.mass + connectedPart.GetResourceMass();

				if(partMass < KJRJointUtils.massForAdjustment || parentMass < KJRJointUtils.massForAdjustment)
					return;

				float momentOfInertia, breakForce, breakTorque;
				if(!KJRJointUtils.CalculateStrength(p, connectedPart, out momentOfInertia, out breakForce, out breakTorque))
					return;

				p.attachJoint.SetBreakingForces(breakForce, breakTorque);

				p.attachMethod = AttachNodeMethod.LOCKED_JOINT;
			}
		}

		private void ReinforceInversionsBuildJoint(KJRJointUtils.Solution s)
		{
			if(jointTracker.CheckDirectJointBetweenParts(s.part, s.linkPart))
				return;

			ConfigurableJoint joint = KJRJointUtils.BuildJoint(s);

			jointTracker.RegisterJoint(s.part, joint, true, KJRJointTracker.Reason.ReinforceInversions);
			jointTracker.RegisterJoint(s.linkPart, joint, true, KJRJointTracker.Reason.ReinforceInversions);

			foreach(Part p in s.set)
				jointTracker.RegisterJoint(p, joint, false, KJRJointTracker.Reason.ReinforceInversions);
		}

		public void ReinforceInversions(Vessel v)
		{
			if(v.Parts.Count <= 1)
				return;

			List<KJRJointUtils.Solution> sols = new List<KJRJointUtils.Solution>();

			List<Part> unresolved = new List<Part>();

			KJRJointUtils.FindInversionAndResolutions(v.rootPart, ref sols, ref unresolved);

			foreach(Part entry in unresolved)
				KJRJointUtils.FindChildInversionResolution(entry, ref sols, ref unresolved);

			foreach(KJRJointUtils.Solution s in sols)
				ReinforceInversionsBuildJoint(s);
		}

		private void BuildAndRegisterExtraJoint(Part part, Part linkPart, KJRJointTracker.Reason jointReason)
		{
			if(jointTracker.CheckDirectJointBetweenParts(part, linkPart))
				return;

			KJRJointUtils.tempSet1.Clear();
			KJRJointUtils.BuildLinkSetConditional(part, ref KJRJointUtils.tempSet1);

			KJRJointUtils.tempSet2.Clear();
			KJRJointUtils.BuildLinkSetConditional(linkPart, ref KJRJointUtils.tempSet2);

			tempResultSet.Clear();
			int rootIndex = 0;
			if(!KJRJointUtils.BuildLinkSetDifference(ref tempResultSet, ref rootIndex, ref KJRJointUtils.tempSet1, ref KJRJointUtils.tempSet2))
				return;

			ConfigurableJoint joint = KJRJointUtils.BuildExtraJoint(part, linkPart);

			jointTracker.RegisterJoint(part, joint, true, jointReason);
			jointTracker.RegisterJoint(linkPart, joint, true, jointReason);

			foreach(Part p in tempResultSet)
				jointTracker.RegisterJoint(p, joint, false, jointReason);
		}

		private const float stiffeningExtensionMassRatioThreshold = 5f;

		public void AdditionalJointsToParent(Vessel v)
		{
			foreach(Part p in v.Parts)
			{
				if((p.parent != null) && (p.parent.parent != null) && (p.physicalSignificance == Part.PhysicalSignificance.FULL))
				{
					ConfigurableJoint j = p.attachJoint.Joint; // second steps uses the first/main joint as reference

					Part newConnectedPart = p.parent.parent;

					bool massRatioBelowThreshold = false;
					int numPartsFurther = 0;

					float partMaxMass = KJRJointUtils.MaximumPossiblePartMass(p);
					List<Part> partsCrossed = new List<Part>();
					List<Part> possiblePartsCrossed = new List<Part>();

					partsCrossed.Add(p.parent);

					Part connectedRbPart = newConnectedPart;

					// search the first part with an acceptable mass/mass ration to this part (joints work better then)
					do
					{
						float massRat1 = (partMaxMass < newConnectedPart.mass) ? (newConnectedPart.mass / partMaxMass) : (partMaxMass / newConnectedPart.mass);

						if(massRat1 <= stiffeningExtensionMassRatioThreshold)
							massRatioBelowThreshold = true;
						else
						{
							float maxMass = KJRJointUtils.MaximumPossiblePartMass(newConnectedPart);
							float massRat2 = (p.mass < maxMass) ? (maxMass / p.mass) : (p.mass / maxMass);
						
							if(massRat2 <= stiffeningExtensionMassRatioThreshold)
								massRatioBelowThreshold = true;
							else
							{
								if((newConnectedPart.parent == null)
								|| !KJRJointUtils.IsJointAdjustmentAllowed(newConnectedPart))
									break;

								newConnectedPart = newConnectedPart.parent;

								if(newConnectedPart.rb == null)
									possiblePartsCrossed.Add(newConnectedPart);
								else
								{
									connectedRbPart = newConnectedPart;
									partsCrossed.AddRange(possiblePartsCrossed);
									partsCrossed.Add(newConnectedPart);
									possiblePartsCrossed.Clear();
								}

								numPartsFurther++;
							}
						}

					} while(!massRatioBelowThreshold);

					if(newConnectedPart.rb != null)
					{
						if(!jointTracker.CheckDirectJointBetweenParts(p, newConnectedPart))
						{
							ConfigurableJoint newJoint = KJRJointUtils.BuildExtraJoint(p, newConnectedPart);

							// register joint
							jointTracker.RegisterJoint(p, newJoint, true, KJRJointTracker.Reason.ExtraStabilityJoint);
							jointTracker.RegisterJoint(newConnectedPart, newJoint, true, KJRJointTracker.Reason.ExtraStabilityJoint);

							foreach(Part part in partsCrossed)
								jointTracker.RegisterJoint(part, newJoint, false, KJRJointTracker.Reason.ExtraStabilityJoint);
						}
					}
				}
			}
		}

		public void AdditionalJointsBetweenEndpoints(Vessel v)
		{
			if(v.Parts.Count <= 1)
				return;

			Dictionary<Part, List<Part>> childPartsToConnectByRoot = new Dictionary<Part,List<Part>>();

			KJRJointUtils.FindRootsAndEndPoints(v.rootPart, ref childPartsToConnectByRoot);

			foreach(Part root in childPartsToConnectByRoot.Keys)
			{
				if(!root.rb)
				{
					if(KJRJointUtils.debug)
						Logger.Log("AdditionalJointsBetweenEndpoints -> root.rb was null", Logger.Level.Error);
					continue;
				}

				List<Part> childPartsToConnect = childPartsToConnectByRoot[root];

				for(int i = 0; i < childPartsToConnect.Count; ++i)
				{
					Part p = childPartsToConnect[i];

					Part linkPart = childPartsToConnect[i + 1 >= childPartsToConnect.Count ? 0 : i + 1];

					BuildAndRegisterExtraJoint(p, linkPart, KJRJointTracker.Reason.ExtraStabilityJoint);


					int part2Index = i + childPartsToConnect.Count / 2;
					if(part2Index >= childPartsToConnect.Count)
						part2Index -= childPartsToConnect.Count;

					Part linkPart2 = childPartsToConnect[part2Index];

					BuildAndRegisterExtraJoint(p, linkPart2, KJRJointTracker.Reason.ExtraStabilityJoint);


					BuildAndRegisterExtraJoint(p, root, KJRJointTracker.Reason.ExtraStabilityJoint);
				}
			}
		}

		private void ReinforceLaunchClamps(Part part)
		{
			part.breakingForce = Mathf.Infinity;
			part.breakingTorque = Mathf.Infinity;
			part.mass = Mathf.Max(part.mass, (part.parent.mass + part.parent.GetResourceMass()) * 0.01f); // We do this to make sure that there is a mass ratio of 100:1 between the clamp and what it's connected to. This helps counteract some of the wobbliness simply, but also allows some give and springiness to absorb the initial physics kick.

			if(KJRJointUtils.debug)
				Logger.Log("Launch Clamp Break Force / Torque increased", Logger.Level.Info);

			if(part.parent.Rigidbody != null) // FEHLER, wir tun alles, tragen es aber nur ein, wenn irgendwas einen Rigidbody hat? nicht mal zwingend das Teil selber?
				BuildAndRegisterExtraJoint(part, part.parent, KJRJointTracker.Reason.ReinforceLaunchClamp);
		}
	}
}

/*	-> how to call KJR from a mod

	Type KJRManagerType = null;
	System.Reflection.MethodInfo KJRManagerCycleAllAutoStrutMethod = null;

	if(KJRManagerCycleAllAutoStrutMethod == null)
	{
		AssemblyLoader.loadedAssemblies.TypeOperation (t => {
			if(t.FullName == "KerbalJointReinforcement.KJRManager") { KJRManagerType = t; } });

		if(KJRManagerType != null)
			KJRManagerCycleAllAutoStrutMethod = KJRManagerType.GetMethod("CycleAllAutoStrut");
	}

	if(KJRManagerCycleAllAutoStrutMethod != null)
		KJRManagerCycleAllAutoStrutMethod.Invoke(null, new object[] { v });
*/
