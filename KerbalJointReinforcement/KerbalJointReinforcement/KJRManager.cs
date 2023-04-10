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

		List<Vessel> updatedVessels;
		HashSet<Vessel> easingVessels;
		KJRMultiJointManager multiJointManager;
		List<Vessel> updatingVessels;
		List<Vessel> constructingVessels;

		internal KJRMultiJointManager GetMultiJointManager()
		{
			return multiJointManager;
		}

		public void Awake()
		{
			KJRJointUtils.LoadConstants();
			updatedVessels = new List<Vessel>();
			easingVessels = new HashSet<Vessel>();
			multiJointManager = new KJRMultiJointManager();
			updatingVessels = new List<Vessel>();
			constructingVessels = new List<Vessel>();

			_instance = this;
		}

		public void Start()
		{
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

			updatedVessels = null;
			easingVessels = null;

			multiJointManager = null;
		}

		IEnumerator RunVesselJointUpdateFunctionDelayed(Vessel v)
		{
			yield return new WaitForFixedUpdate();

			if (!EVAConstructionModeController.Instance.IsOpen || (EVAConstructionModeController.Instance.panelMode != EVAConstructionModeController.PanelMode.Construction))
			{
				updatingVessels.Remove(v);

KJRJointUtils.jc = 0;
				RunVesselJointUpdateFunction(v);

ScreenMessages.PostScreenMessage("KJR joints built: " + KJRJointUtils.jc, 30, ScreenMessageStyle.UPPER_CENTER);

			foreach(Part p in v.Parts)
					p.ReleaseAutoStruts(); // FEHLER, weiss halt nicht


#if IncludeAnalyzer
				KJRAnalyzerJoint.RunVesselJointUpdateFunction(v);

				KJRAnalyzer.WasModified(v);
#endif
			}
		}

		private void OnVesselCreate(Vessel v)
		{
			multiJointManager.RemoveAllVesselJoints(v);

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

			multiJointManager.RemoveAllVesselJoints(v);
			updatedVessels.Remove(v);

			if(KJRJointUtils.debug)
			{
				StringBuilder debugString = new StringBuilder();
				debugString.AppendLine("KJR: Modified vessel " + v.id + " (" + v.GetName() + ")");
				debugString.AppendLine(System.Environment.StackTrace);
				debugString.AppendLine("Now contains: ");
				foreach(Part p in v.Parts)
					debugString.AppendLine("  " + p.partInfo.name + " (" + p.flightID + ")");
				Debug.Log(debugString);
			}

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
				foreach (Vessel v in constructingVessels)
					OnVesselWasModified(v);
				constructingVessels.Clear();
            }
		}

		private void OnEVAConstructionModePartAttached(Vessel v, Part p)
		{
			if (!constructingVessels.Contains(v))
				constructingVessels.Add(v);
		}

		private void OnEVAConstructionModePartDetached(Vessel v, Part p)
		{
			multiJointManager.RemovePartJoints(p);

			if (!constructingVessels.Contains(v))
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
				multiJointManager.RemoveAllVesselJoints(v);

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
			multiJointManager.RemovePartJoints(p);
		}

		public void OnEaseStart(Vessel v)
		{
			if(KJRJointUtils.debug)
				Debug.Log("KJR easing " + v.vesselName);

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
				Debug.Log("KJR: Processing vessel " + v.id + " (" + v.GetName() + "); root " +
							v.rootPart.partInfo.name + " (" + v.rootPart.flightID + ")");
			}

			bool bReinforced = false;

#if IncludeAnalyzer
			if(WindowManager.Instance.ReinforceExistingJoints)
			{
#endif

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

#if IncludeAnalyzer
			}
#endif

#if IncludeAnalyzer
			if(WindowManager.Instance.ReinforceInversions)
			{
#endif
			if(KJRJointUtils.reinforceInversions)
			{
				ReinforceInversions(v);
				bReinforced = true;
			}

#if IncludeAnalyzer
			}
#endif

#if IncludeAnalyzer
			if(WindowManager.Instance.BuildExtraStabilityJoints)
			{
#endif
			if(KJRJointUtils.addExtraStabilityJoints)
			{
				if(KJRJointUtils.extraLevel >= 2)
					AdditionalJointsToParent(v);

				AdditionalJointsBetweenEndpoints(v);
				bReinforced = true;
			}
#if IncludeAnalyzer
			}
#endif

			if(bReinforced && !updatedVessels.Contains(v))
				updatedVessels.Add(v);
		}

#if IncludeAnalyzer
		public void FixedUpdate()
		{
			if(FlightGlobals.ready && FlightGlobals.Vessels != null)
			{
				KJRAnalyzer.Update();
			}
	   }
#endif

static bool TakeNew = false;
		static float _l = 0.9f;
		static float _u = 1.1f;

static int calctype = 2;

		// attachJoint's are always joints from a part to its parent
		private void ReinforceAttachJoints(Part p)
		{
			if(p.rb == null || p.attachJoint == null || !KJRJointUtils.IsJointAdjustmentAllowed(p))
				return;

			if(KJRJointUtils.debug && (p.attachMethod == AttachNodeMethod.LOCKED_JOINT))
			{
				Debug.Log("KJR: Already processed part before: " + p.partInfo.name + " (" + p.flightID + ") -> " +
							p.parent.partInfo.name + " (" + p.parent.flightID + ")");
			}

			List<ConfigurableJoint> jointList;

			if(p.Modules.Contains<CModuleStrut>())	// FEHLER, wieso dann nicht?
			{
float decouplerAndClampJointStrength = float.MaxValue; // FEHLER, temp, neue Zwischenlösung

				CModuleStrut s = p.Modules.GetModule<CModuleStrut>();

				if((s.jointTarget != null) && (s.jointRoot != null))
				{
					jointList = s.strutJoint.joints;

					if(jointList != null)
					{
						for(int i = 0; i < jointList.Count; i++)
						{
							ConfigurableJoint j = jointList[i];

							if(j == null)
								continue;

							JointDrive strutDrive = j.angularXDrive;
							strutDrive.positionSpring = decouplerAndClampJointStrength;
							strutDrive.maximumForce = decouplerAndClampJointStrength;
							j.xDrive = j.yDrive = j.zDrive = j.angularXDrive = j.angularYZDrive = strutDrive;

							j.xMotion = j.yMotion = j.zMotion = ConfigurableJointMotion.Locked;
							j.angularXMotion = j.angularYMotion = j.angularZMotion = ConfigurableJointMotion.Locked;

							//float scalingFactor = (s.jointTarget.mass + s.jointTarget.GetResourceMass() + s.jointRoot.mass + s.jointRoot.GetResourceMass()) * 0.01f;

							j.breakForce = decouplerAndClampJointStrength;
							j.breakTorque = decouplerAndClampJointStrength;
						}

						p.attachMethod = AttachNodeMethod.LOCKED_JOINT;
					}
				}
			}
			
			jointList = p.attachJoint.joints;

			if(jointList == null)
				return;

			StringBuilder debugString = KJRJointUtils.debug ? new StringBuilder() : null;

			if(!KJRJointUtils.IsJointUnlockable(p)) // exclude those actions from joints that can be dynamically unlocked
			{
				float partMass = p.mass + p.GetResourceMass();
				for(int i = 0; i < jointList.Count; i++)
				{
					ConfigurableJoint j = jointList[i];
					if(j == null)
						continue;

					Rigidbody connectedBody = j.connectedBody;

					Part connectedPart = connectedBody.GetComponent<Part>() ?? p.parent;
					float parentMass = connectedPart.mass + connectedPart.GetResourceMass();

					if(partMass < KJRJointUtils.massForAdjustment || parentMass < KJRJointUtils.massForAdjustment)
					{
						if(KJRJointUtils.debug)
							Debug.Log("KJR: Part mass too low, skipping: " + p.partInfo.name + " (" + p.flightID + ")");

						continue;
					}				
				
					// check attachment nodes for better orientation data
					AttachNode attach = p.FindAttachNodeByPart(p.parent);
					AttachNode p_attach = p.parent.FindAttachNodeByPart(p);
					AttachNode node = attach ?? p_attach;

					if(node == null)
					{
						// check if it's a pair of coupled docking ports
						var dock1 = p.Modules.GetModule<ModuleDockingNode>();
						var dock2 = p.parent.Modules.GetModule<ModuleDockingNode>();

						//Debug.Log(dock1 + " " + (dock1 ? "" + dock1.dockedPartUId : "?") + " " + dock2 + " " + (dock2 ? "" + dock2.dockedPartUId : "?"));

						if(dock1 && dock2 && (dock1.dockedPartUId == p.parent.flightID || dock2.dockedPartUId == p.flightID))
						{
							attach = p.FindAttachNode(dock1.referenceAttachNode);
							p_attach = p.parent.FindAttachNode(dock2.referenceAttachNode);
							node = attach ?? p_attach;
						}
					}

					// if still no node and apparently surface attached, use the normal one if it's there
					if(node == null && p.attachMode == AttachModes.SRF_ATTACH)
						node = attach = p.srfAttachNode;

					float breakForce = Math.Min(p.breakingForce, connectedPart.breakingForce) * KJRJointUtils.breakForceMultiplier;
					float breakTorque = Math.Min(p.breakingTorque, connectedPart.breakingTorque) * KJRJointUtils.breakTorqueMultiplier;
					Vector3 anchor = j.anchor;
					Vector3 connectedAnchor = j.connectedAnchor;
					Vector3 axis = j.axis;

					float radius = 0;
					float area = 0;
					float momentOfInertia = 0;

					if(node != null)
					{
						// part that owns the node -> for surface attachment, this can only be parent if docking flips hierarchy
						Part main = (node == attach) ? p : p.parent;

						// orientation and position of the node in owner's local coords
						Vector3 ndir = node.orientation.normalized;
						Vector3 npos = node.position + node.offset;

						// and in the current part's local coords
						Vector3 dir = axis = p.transform.InverseTransformDirection(main.transform.TransformDirection(ndir));

						if(node.nodeType == AttachNode.NodeType.Surface)
						{
							// guessed main axis / for parts with stack nodes should be the axis of the stack
							Vector3 up = KJRJointUtils.GuessUpVector(main).normalized;

							// if guessed up direction is same as node direction, it's basically stack
							// for instance, consider a radially-attached docking port
							if(Mathf.Abs(Vector3.Dot(up, ndir)) > 0.9f)
							{
								radius = Mathf.Min(KJRJointUtils.CalculateRadius(main, ndir), KJRJointUtils.CalculateRadius(connectedPart, ndir));
								if(radius <= 0.001)
									radius = node.size * 1.25f;
								area = Mathf.PI * radius * radius;				// area of cylinder
								momentOfInertia = area * radius * radius / 4;	// moment of inertia of cylinder
							}
							else
							{
								// x along surface, y along ndir normal to surface, z along surface & main axis (up)
								var size1 = KJRJointUtils.CalculateExtents(main, ndir, up);

								var size2 = KJRJointUtils.CalculateExtents(connectedPart, ndir, up);

								// use average of the sides, since we don't know which one is used for attaching
								float width1 = (size1.x + size1.z) / 2;
								float width2 = (size2.x + size2.z) / 2;
								if(size1.y * width1 > size2.y * width2)
								{
									area = size1.y * width1;
									radius = Mathf.Max(size1.y, width1);
								}
								else
								{
									area = size2.y * width2;
									radius = Mathf.Max(size2.y, width2);
								}

								momentOfInertia = area * radius / 12;			// moment of inertia of a rectangle bending along the longer length
							}
						}
						else
						{
							radius = Mathf.Min(KJRJointUtils.CalculateRadius(p, dir), KJRJointUtils.CalculateRadius(connectedPart, dir));
							if(radius <= 0.001)
								radius = node.size * 1.25f;
							area = Mathf.PI * radius * radius;					// area of cylinder
							momentOfInertia = area * radius * radius / 4;		// moment of inertia of cylinder
						}
					}
					// assume part is attached along its "up" cross section / use a cylinder to approximate properties
					else if(p.attachMode == AttachModes.STACK)
					{
						radius = Mathf.Min(KJRJointUtils.CalculateRadius(p, Vector3.up), KJRJointUtils.CalculateRadius(connectedPart, Vector3.up));
						if(radius <= 0.001)
							radius = 1.25f; // FEHLER, komisch, wieso setzen wir dann nicht alles < 1.25f auf 1.25f? -> zudem hatten wir hier sowieso einen Bug, das ist also sowieso zu hinterfragen
						area = Mathf.PI * radius * radius;						// area of cylinder
						momentOfInertia = area * radius * radius / 4;			// moment of Inertia of cylinder
					}
					else if(p.attachMode == AttachModes.SRF_ATTACH)
					{					
						// x,z sides, y along main axis
						Vector3 up1 = KJRJointUtils.GuessUpVector(p);
						var size1 = KJRJointUtils.CalculateExtents(p, up1);

						Vector3 up2 = KJRJointUtils.GuessUpVector(connectedPart);
						var size2 = KJRJointUtils.CalculateExtents(connectedPart, up2);

						// use average of the sides, since we don't know which one is used for attaching
						float width1 = (size1.x + size1.z) / 2;
						float width2 = (size2.x + size2.z) / 2;
						if(size1.y * width1 > size2.y * width2)
						{
							area = size1.y * width1;
							radius = Mathf.Max(size1.y, width1);
						}
						else
						{
							area = size2.y * width2;
							radius = Mathf.Max(size2.y, width2);
						}
						momentOfInertia = area * radius / 12;					// moment of inertia of a rectangle bending along the longer length
					}

					// if using volume, raise al stiffness-affecting parameters to the 1.5 power
					if(KJRJointUtils.useVolumeNotArea)
					{
						area = Mathf.Pow(area, 1.5f);
						momentOfInertia = Mathf.Pow(momentOfInertia, 1.5f);
					}

					// FEHLER, jetzt probier ich meine Berechnung
					float momentOfInertia2, breakForce2, breakTorque2;
if(calctype == 1)
	{
					KJRJointUtils.CalculateStrength(p, connectedPart,
						out momentOfInertia2, out breakForce2, out breakTorque2);
					}
else if(calctype == 2)
					{
					KJRJointUtils.CalculateStrength2(p, connectedPart,
						out momentOfInertia2, out breakForce2, out breakTorque2);
					}
else
					{
					KJRJointUtils.CalculateStrength0(p, connectedPart,
						out momentOfInertia2, out breakForce2, out breakTorque2);
momentOfInertia2 = momentOfInertia;
					}


					breakForce = Mathf.Max(KJRJointUtils.breakStrengthPerArea * area, breakForce);
					breakTorque = Mathf.Max(KJRJointUtils.breakTorquePerMOI * momentOfInertia, breakTorque);

if((momentOfInertia * _l > momentOfInertia2)
|| (momentOfInertia * _u < momentOfInertia2)
|| (breakForce * _l > breakForce2)
|| (breakForce * _u < breakForce2)
|| (breakTorque * _l > breakTorque2)
|| (breakTorque * _u < breakTorque2))
					{
						// mehr als 10% rauf oder runter

						TakeNew = TakeNew;
					}
else if(TakeNew)
					{
						momentOfInertia = momentOfInertia2;
						breakForce = breakForce2;
						breakTorque = breakTorque2;
					}

					JointDrive angDrive = j.angularXDrive;
					angDrive.positionSpring = Mathf.Max(momentOfInertia * KJRJointUtils.angularDriveSpring, angDrive.positionSpring);
					angDrive.positionDamper = Mathf.Max(momentOfInertia * KJRJointUtils.angularDriveDamper * 0.1f, angDrive.positionDamper);
// FEHLER, xtreme-Debugging
//if(KJRJointUtils.debug && (angDrive.maximumForce > breakTorque))
//	Debug.LogError("KJR: weakening joint!!!!!");
	//				angDrive.maximumForce = breakTorque; -> FEHLER, das macht's wirklich schwächer, aber das andere ist mir fast zu stark
// FEHLER, neue Idee... weil, das scheint mir etwas komisch hier..., wir machen das Zeug nämlich schwächer?
angDrive.maximumForce = Mathf.Max(breakTorque, angDrive.maximumForce);

					/*float moi_avg = p.rb.inertiaTensor.magnitude;

					moi_avg += (p.transform.localToWorldMatrix.MultiplyPoint(p.CoMOffset) - p.parent.transform.position).sqrMagnitude * p.rb.mass;

					if(moi_avg * 2f / drive.positionDamper < 0.08f)
					{
						drive.positionDamper = moi_avg / (0.04f);

						drive.positionSpring = drive.positionDamper * drive.positionDamper / moi_avg;
					}*/
					j.angularXDrive = j.angularYZDrive = j.slerpDrive = angDrive;

					JointDrive linDrive = j.xDrive;
//if(KJRJointUtils.debug && (linDrive.maximumForce > breakForce))
//	Debug.LogError("KJR: weakening joint!!!!!");
	//				linDrive.maximumForce = breakForce; -> FEHLER, das macht's wirklich schwächer, aber das andere ist mir fast zu stark
// FEHLER, neue Idee... weil, das scheint mir etwas komisch hier..., wir machen das Zeug nämlich schwächer?
linDrive.maximumForce = Mathf.Max(breakForce, linDrive.maximumForce);

					j.xDrive = j.yDrive = j.zDrive = linDrive;

					j.linearLimit = j.angularYLimit = j.angularZLimit = j.lowAngularXLimit = j.highAngularXLimit
						= new SoftJointLimit { limit = 0, bounciness = 0 };
					j.linearLimitSpring = j.angularYZLimitSpring = j.angularXLimitSpring
						= new SoftJointLimitSpring { spring = 0, damper = 0 };

					j.targetAngularVelocity = Vector3.zero;
					j.targetVelocity = Vector3.zero;
					j.targetRotation = Quaternion.identity;
					j.targetPosition = Vector3.zero;

					j.breakForce = breakForce;			// FEHLER, das hier entfernt das "unbreakable"... wollen wir das? und das SetBreakingForces nachher überschreibt den Wert hier gleich wieder -> klären -> sonst noch korrekten Wert rechne? * PhysicsGlobals.JointBreakForceFactor irgendwas
					j.breakTorque = breakTorque;		// FEHLER, gleiche Frage wie eine Zeile oberhalb

//PhysicsGlobals.JointBreakForceFactor
//PhysicsGlobals.JointBreakTorqueFactor
//if(KJRJointUtils.debug && (linDrive.maximumForce > breakForce))
//	Debug.LogError("KJR: weakening joint!!!!!");
// FEHLER, kann hier nix sagen... aber, egal jetzt mal

					p.attachJoint.SetBreakingForces(j.breakForce, j.breakTorque);

					p.attachMethod = AttachNodeMethod.LOCKED_JOINT;
				}
			}

			if(KJRJointUtils.debug)
				Debug.Log(debugString.ToString());
		}

		private void ReinforceInversionsBuildJoint(KJRJointUtils.Sol2 s)
		{
			if(multiJointManager.CheckDirectJointBetweenParts(s.part, s.linkPart))
			{
				++KJRJointUtils.jc;
				return;
			}

			ConfigurableJoint joint = KJRJointUtils.BuildJoint(s);

			multiJointManager.RegisterMultiJoint(s.part, joint, true, KJRMultiJointManager.Reason.ReinforceInversions);
			multiJointManager.RegisterMultiJoint(s.linkPart, joint, true, KJRMultiJointManager.Reason.ReinforceInversions);

			foreach(Part p in s.set)
				multiJointManager.RegisterMultiJoint(p, joint, false, KJRMultiJointManager.Reason.ReinforceInversions);
		}

		public void ReinforceInversions(Vessel v)
		{
			if(v.Parts.Count <= 1)
				return;

			List<KJRJointUtils.Sol2> sols = new List<KJRJointUtils.Sol2>();

		//	Dictionary<Part, Part> inversionResolutions = new Dictionary<Part,Part>();
			List<Part> unresolved = new List<Part>();

			KJRJointUtils.FindInversionAndResolutions(v.rootPart, ref sols, ref unresolved);

// FEHLER, mal sehen, was man damit jetzt tun könnte -> eigentlich müsste man das in die Liste der zu verstärkenden teils aufnehmen... irgendwie

			KJRJointUtils.tempPartList = new List<Part>();

			foreach(Part entry in unresolved)
			{
				KJRJointUtils.tempPartList.Clear();

				KJRJointUtils.FindChildInversionResolution(entry, ref sols, ref unresolved);
					// FEHLER, da holen wir mal mögliche Lösungen raus und rechnen für die erste was... nur so zum Test
					// was wir damit tun? keine Ahnung... und ist irgendwas davon sinnvoll? keine Ahnung... sehen wir dann mal
			}

			KJRJointUtils.tempPartList = null;

// FEHLER, später die "solutions" zusammenhängen
	//		foreach(KeyValuePair<Part, Part> entry in inversionResolutions)
	//			ReinforceInversionsBuildJoint(entry.Key, entry.Value);

			foreach(KJRJointUtils.Sol2 s in sols)
				ReinforceInversionsBuildJoint(s);
		}

		private void MultiPartJointBuildJoint(Part part, Part linkPart, KJRMultiJointManager.Reason jointReason)
		{
			if(multiJointManager.CheckDirectJointBetweenParts(part, linkPart))
			{
				++KJRJointUtils.jc;
				return;
			}

			if(!multiJointManager.TrySetValidLinkedSet(part, linkPart))
				return;

			ConfigurableJoint joint = KJRJointUtils.BuildExtraJoint(part, linkPart);

			multiJointManager.RegisterMultiJoint(part, joint, true, jointReason);
			multiJointManager.RegisterMultiJoint(linkPart, joint, true, jointReason);

			foreach(Part p in multiJointManager.linkedSet)
				multiJointManager.RegisterMultiJoint(p, joint, false, jointReason);
		}

		public void AdditionalJointsToParent(Vessel v)
		{
			foreach(Part p in v.Parts)
			{
				if((p.parent != null) && (p.parent.parent != null) && (p.physicalSignificance == Part.PhysicalSignificance.FULL))
				{
					ConfigurableJoint j = p.attachJoint.Joint; // second steps uses the first/main joint as reference

float stiffeningExtensionMassRatioThreshold = 5f; // FEHLER, ich will die Funktion hier ausbauen, daher kommt das temp hier rein

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

					} while(!massRatioBelowThreshold);// && numPartsFurther < 5);

// FEHLER, könnte man's mit MultiPartJointBuildJoint machen?
					if(newConnectedPart.rb != null)
					{
						if(!multiJointManager.CheckDirectJointBetweenParts(p, newConnectedPart))
						{
							ConfigurableJoint newJoint = KJRJointUtils.BuildExtraJoint(p, newConnectedPart);

							// register joint
							multiJointManager.RegisterMultiJoint(p, newJoint, true, KJRMultiJointManager.Reason.ExtraStabilityJoint);
							multiJointManager.RegisterMultiJoint(newConnectedPart, newJoint, true, KJRMultiJointManager.Reason.ExtraStabilityJoint);

							foreach(Part part in partsCrossed)
								multiJointManager.RegisterMultiJoint(part, newJoint, false, KJRMultiJointManager.Reason.ExtraStabilityJoint);
						}
						else
							++KJRJointUtils.jc;
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
				if(!root.rb) // FEHLER, muss neu immer gelten
					continue;

				List<Part> childPartsToConnect = childPartsToConnectByRoot[root];

				for(int i = 0; i < childPartsToConnect.Count; ++i)
				{
					Part p = childPartsToConnect[i];

					Part linkPart = childPartsToConnect[i + 1 >= childPartsToConnect.Count ? 0 : i + 1];

					MultiPartJointBuildJoint(p, linkPart, KJRMultiJointManager.Reason.ExtraStabilityJoint);


					int part2Index = i + childPartsToConnect.Count / 2;
					if(part2Index >= childPartsToConnect.Count)
						part2Index -= childPartsToConnect.Count;

					Part linkPart2 = childPartsToConnect[part2Index];

					MultiPartJointBuildJoint(p, linkPart2, KJRMultiJointManager.Reason.ExtraStabilityJoint);


					MultiPartJointBuildJoint(p, root, KJRMultiJointManager.Reason.ExtraStabilityJoint);
				}
			}
		}

		private void ReinforceLaunchClamps(Part part)
		{
			part.breakingForce = Mathf.Infinity;
			part.breakingTorque = Mathf.Infinity;
			part.mass = Mathf.Max(part.mass, (part.parent.mass + part.parent.GetResourceMass()) * 0.01f); // We do this to make sure that there is a mass ratio of 100:1 between the clamp and what it's connected to. This helps counteract some of the wobbliness simply, but also allows some give and springiness to absorb the initial physics kick.

			if(KJRJointUtils.debug)
				Debug.Log("KJR: Launch Clamp Break Force / Torque increased");

			StringBuilder debugString = null;

			if(KJRJointUtils.debug)
			{
				debugString = new StringBuilder();
				debugString.AppendLine("The following joints added by " + part.partInfo.title + " to increase stiffness:");
			}

			if(part.parent.Rigidbody != null) // FEHLER, wir tun alles, tragen es aber nur ein, wenn irgendwas einen Rigidbody hat? nicht mal zwingend das Teil selber?
				MultiPartJointBuildJoint(part, part.parent, KJRMultiJointManager.Reason.ReinforceLaunchClamp);

			if(KJRJointUtils.debug)
			{
				debugString.AppendLine(part.parent.partInfo.title + " connected to part " + part.partInfo.title);
				Debug.Log(debugString.ToString());
			}
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
