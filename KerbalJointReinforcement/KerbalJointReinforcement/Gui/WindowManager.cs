using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using KSP.IO;
using KSP.UI;
using KSP.UI.Screens;

namespace KerbalJointReinforcement
{
#if IncludeAnalyzer

	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class KJRFlightWindowManager : WindowManager
	{
		public override string AddonName { get { return this.name; } }
	}

	[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
	public class KJRSpaceCenterWindowManager : WindowManager
	{
		public override string AddonName { get { return this.name; } }
	}

	public class WindowManager : MonoBehaviour
	{
		public virtual String AddonName { get; set; }

		private static WindowManager _instance;

		private bool GUIHidden = false;

		// windows
		private static GameObject _settingsWindow;
		private static Vector3 _settingsWindowPosition;
		private static CanvasGroupFader _settingsWindowFader;

		// settings
		public static float _UIAlphaValue = 0.8f;
		public static float _UIScaleValue = 1.0f;
		private const float UI_FADE_TIME = 0.1f;
		private const float UI_MIN_ALPHA = 0.2f;
		private const float UI_MIN_SCALE = 0.5f;
		private const float UI_MAX_SCALE = 2.0f;

		private static bool bInvalid = false;

		internal void Invalidate()
		{
			bInvalid = true;
			if(appLauncherButton != null)
			{
				GUIEnabled = appLauncherButton.toggleButton.CurrentState == UIRadioButton.State.True;
				appLauncherButton.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT;
			}
			else
				GUIEnabled = false;
		}

		private ApplicationLauncherButton appLauncherButton;

		public bool ShowKSPJoints = false;
		public bool ReinforceExistingJoints = true;

		public bool BuildAdditionalJointToParent = false;
		public bool ShowAdditionalJointToParent = false;

		public bool BuildMultiPartJointTreeChildren = false;
		public bool ShowMultiPartJointTreeChildren = false;

		public bool BuildMultiPartJointTreeChildrenRoot = false;
		public bool ShowMultiPartJointTreeChildrenRoot = false;

		public bool ReinforceInversions = true;
		public bool ShowReinforcedInversions = false;

		public bool ShowInstability = false;

		internal bool GUIEnabled = false;

		private static bool isKeyboardLocked = false;

		public static WindowManager Instance
		{
			get { return _instance; }
		}

		private void Awake()
		{
			LoadConfigXml();

			KJRAnalyzer.OnLoad(ShowKSPJoints | ShowAdditionalJointToParent | ShowMultiPartJointTreeChildren | ShowMultiPartJointTreeChildrenRoot);

			Logger.Log("[NewGUI] awake, Mode: " + AddonName);

			if((HighLogic.LoadedScene != GameScenes.FLIGHT) && (HighLogic.LoadedScene != GameScenes.SPACECENTER))
			{
				_instance = null;
				return;
			}

			_instance = this;

			GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequestedForAppLauncher);
			GameEvents.onGUIApplicationLauncherReady.Add(AddAppLauncherButton);

			Logger.Log("[GUI] Added Toolbar GameEvents Handlers", Logger.Level.Debug);

			GameEvents.onShowUI.Add(OnShowUI);
			GameEvents.onHideUI.Add(OnHideUI);

			Logger.Log("[GUI] awake finished successfully", Logger.Level.Debug);
		}

		private void OnShowUI()
		{
			if(GUIHidden)
            {
				GUIHidden = false;
				ShowKJRWindow();
            }
		}

		private void OnHideUI()
		{
			if(GUIHidden = GUIEnabled)
				HideKJRWindow();
		}

		private void SetGlobalAlpha(float newAlpha)
		{
			_UIAlphaValue = Mathf.Clamp(newAlpha, UI_MIN_ALPHA, 1.0f);

			if(_settingsWindow)
				_settingsWindow.GetComponent<CanvasGroup>().alpha = _UIAlphaValue;
		}

		private void SetGlobalScale(float newScale)
		{
			newScale = Mathf.Clamp(newScale, UI_MIN_SCALE, UI_MAX_SCALE);

			if(_settingsWindow)
				_settingsWindow.transform.localScale = Vector3.one * newScale;

			_UIScaleValue = newScale;
		}

		////////////////////////////////////////
		// Settings
		private Toggle AddNewOption(GameObject content, string text)
		{
			var Opt = GameObject.Instantiate(UIAssetsLoader.optionLinePrefab);
			Opt.transform.SetParent(content.transform, false);
			Opt.GetChild("Label").GetComponent<Text>().text = text;

			return Opt.GetChild("Toggle").GetComponent<Toggle>();
		}

		private void InitSettingsWindow(bool startSolid = true)
		{
			_settingsWindow = GameObject.Instantiate(UIAssetsLoader.settingsWindowPrefab);
			_settingsWindow.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);
			_settingsWindow.GetChild("WindowTitle").AddComponent<PanelDragger>();
			_settingsWindowFader = _settingsWindow.AddComponent<CanvasGroupFader>();

			_settingsWindow.GetComponent<CanvasGroup>().alpha = 0f;

			if(_settingsWindowPosition == Vector3.zero)
				_settingsWindowPosition = _settingsWindow.transform.position; // get the default position from the prefab
			else
				_settingsWindow.transform.position = ClampWindowPosition(_settingsWindowPosition);

			var closeButton = _settingsWindow.GetChild("WindowTitle").GetChild("RightWindowButton");
			if(closeButton != null)
				closeButton.GetComponent<Button>().onClick.AddListener(OnHideCallback);

			var content = _settingsWindow.GetChild("WindowContent");

			var OptShowKSPJoints = AddNewOption(content, "ShowKSPJoints");
			OptShowKSPJoints.isOn = ShowKSPJoints;

			var OptReinforceExistingJoints = AddNewOption(content, "Reinforce Existing Joints");
			OptReinforceExistingJoints.isOn = ReinforceExistingJoints;

			var OptBuildAdditionalJointToParent = AddNewOption(content, "Build Additional Joints To Parent");
			OptBuildAdditionalJointToParent.isOn = BuildAdditionalJointToParent;

			var OptShowAdditionalJointToParent = AddNewOption(content, "Show Additional Joints To Parent");
			OptShowAdditionalJointToParent.isOn = ShowAdditionalJointToParent;

			var OptBuildMultiPartJointTreeChildren = AddNewOption(content, "Build MultiPartJointTreeChildren");
			OptBuildMultiPartJointTreeChildren.isOn = BuildMultiPartJointTreeChildren;

			var OptShowMultiPartJointTreeChildren = AddNewOption(content, "Show MultiPartJointTreeChildren");
			OptShowMultiPartJointTreeChildren.isOn = ShowMultiPartJointTreeChildren;

			var OptBuildMultiPartJointTreeChildrenRoot = AddNewOption(content, "Build MultiPartJointTreeChildrenRoot");
			OptBuildMultiPartJointTreeChildrenRoot.isOn = BuildMultiPartJointTreeChildrenRoot;

			var OptShowMultiPartJointTreeChildrenRoot = AddNewOption(content, "Show MultiPartJointTreeChildrenRoot");
			OptShowMultiPartJointTreeChildrenRoot.isOn = ShowMultiPartJointTreeChildrenRoot;

			var OptReinforceInversions = AddNewOption(content, "Reinforce Inversions");
			OptReinforceInversions.isOn = ReinforceInversions;

			var OptShowReinforcedInversions = AddNewOption(content, "Show Reinforced Inversions");
			OptShowReinforcedInversions.isOn = ShowReinforcedInversions;

			var OptAutoStrutDisplay = AddNewOption(content, "Show AutoStruts");
			OptAutoStrutDisplay.isOn = PhysicsGlobals.AutoStrutDisplay;

			var OptShowInstability = AddNewOption(content, "Show Instability");
			OptShowInstability.isOn = ShowInstability;

			var footerButtons = _settingsWindow.GetChild("WindowFooter").GetChild("WindowFooterButtonsHLG");
	
			var cancelButton = footerButtons.GetChild("CancelButton").GetComponent<Button>();
			cancelButton.onClick.AddListener(() =>
				{
					OptShowKSPJoints.isOn = ShowKSPJoints;
					OptReinforceExistingJoints.isOn = ReinforceExistingJoints;
					OptBuildAdditionalJointToParent.isOn = BuildAdditionalJointToParent;
					OptShowAdditionalJointToParent.isOn = ShowAdditionalJointToParent;
					OptBuildMultiPartJointTreeChildren.isOn = BuildMultiPartJointTreeChildren;
					OptShowMultiPartJointTreeChildren.isOn = ShowMultiPartJointTreeChildren;
					OptBuildMultiPartJointTreeChildrenRoot.isOn = BuildMultiPartJointTreeChildrenRoot;
					OptShowMultiPartJointTreeChildrenRoot.isOn = ShowMultiPartJointTreeChildrenRoot;
					OptReinforceInversions.isOn = ReinforceInversions;
					OptShowReinforcedInversions.isOn = ShowReinforcedInversions;
					OptAutoStrutDisplay.isOn = PhysicsGlobals.AutoStrutDisplay;
					OptShowInstability.isOn = ShowInstability;
				});
	
			var defaultButton = footerButtons.GetChild("DefaultButton").GetComponent<Button>();
			defaultButton.onClick.AddListener(() =>
				{
					bool bCycle = false, bCycle2 = false;

					OptShowKSPJoints.isOn = ShowKSPJoints = false;

					if(!ReinforceExistingJoints)
						bCycle = true;
					OptReinforceExistingJoints.isOn = ReinforceExistingJoints = true;

					if(BuildAdditionalJointToParent)
						bCycle = true;
					OptBuildAdditionalJointToParent.isOn = BuildAdditionalJointToParent = false;

					OptShowAdditionalJointToParent.isOn = ShowAdditionalJointToParent = false;
	
					if(BuildMultiPartJointTreeChildren)
						bCycle = true;
					OptBuildMultiPartJointTreeChildren.isOn = BuildMultiPartJointTreeChildren = false;

					OptShowMultiPartJointTreeChildren.isOn = ShowMultiPartJointTreeChildren = false;

					if(BuildMultiPartJointTreeChildrenRoot)
						bCycle = true;
					OptBuildMultiPartJointTreeChildrenRoot.isOn = BuildMultiPartJointTreeChildrenRoot = false;

					OptShowMultiPartJointTreeChildrenRoot.isOn = ShowMultiPartJointTreeChildrenRoot = false;

					if(!ReinforceInversions)
						bCycle = true;
					OptReinforceInversions.isOn = ReinforceInversions = true;

					OptShowReinforcedInversions.isOn = ShowReinforcedInversions = false;

					OptAutoStrutDisplay.isOn = PhysicsGlobals.AutoStrutDisplay = false;

					if(ShowInstability)
						bCycle2 = true;
					OptShowInstability.isOn = ShowInstability = false;

					KJRAnalyzer.Show = ShowKSPJoints | ShowAdditionalJointToParent | ShowMultiPartJointTreeChildren | ShowMultiPartJointTreeChildrenRoot;

					if(HighLogic.LoadedSceneIsFlight)
					{
						if(bCycle)
							KJRManager.Instance.OnVesselWasModified(FlightGlobals.ActiveVessel);
						else if(bCycle2)
							KJRAnalyzerJoint.RunVesselJointUpdateFunction(FlightGlobals.ActiveVessel);
					}
				});
	
			var applyButton = footerButtons.GetChild("ApplyButton").GetComponent<Button>();
			applyButton.onClick.AddListener(() => 
				{
					bool bCycle = false, bCycle2 = false;

					ShowKSPJoints = OptShowKSPJoints.isOn;

					if(ReinforceExistingJoints != OptReinforceExistingJoints.isOn)
					{
						bCycle = true;
						ReinforceExistingJoints = OptReinforceExistingJoints.isOn;
					}

					if(BuildAdditionalJointToParent != OptBuildAdditionalJointToParent.isOn)
					{
						bCycle = true;
						BuildAdditionalJointToParent = OptBuildAdditionalJointToParent.isOn;
					}

					ShowAdditionalJointToParent = OptShowAdditionalJointToParent.isOn;

					if(BuildMultiPartJointTreeChildren != OptBuildMultiPartJointTreeChildren.isOn)
					{
						bCycle = true;
						BuildMultiPartJointTreeChildren = OptBuildMultiPartJointTreeChildren.isOn;
					}
	
					ShowMultiPartJointTreeChildren = OptShowMultiPartJointTreeChildren.isOn;

					if(BuildMultiPartJointTreeChildrenRoot != OptBuildMultiPartJointTreeChildrenRoot.isOn)
					{
						bCycle = true;
						BuildMultiPartJointTreeChildrenRoot = OptBuildMultiPartJointTreeChildrenRoot.isOn;
					}

					ShowMultiPartJointTreeChildrenRoot = OptShowMultiPartJointTreeChildrenRoot.isOn;

					if(ReinforceInversions != OptReinforceInversions.isOn)
					{
						bCycle = true;
						ReinforceInversions = OptReinforceInversions.isOn;
					}

					ShowReinforcedInversions = OptShowReinforcedInversions.isOn;

					PhysicsGlobals.AutoStrutDisplay = OptAutoStrutDisplay.isOn;

					if(ShowInstability != OptShowInstability.isOn)
					{
						bCycle2 = true;
						ShowInstability = OptShowInstability.isOn;
					}

					KJRAnalyzer.Show = ShowKSPJoints | ShowAdditionalJointToParent | ShowMultiPartJointTreeChildren | ShowMultiPartJointTreeChildrenRoot;

					if(HighLogic.LoadedSceneIsFlight)
					{
						if(bCycle)
							KJRManager.Instance.OnVesselWasModified(FlightGlobals.ActiveVessel);
						else if(bCycle2)
							KJRAnalyzerJoint.RunVesselJointUpdateFunction(FlightGlobals.ActiveVessel);
					}
				});
		}

		public void RebuildUI()
		{
			bInvalid = false;

			if(_settingsWindow)
			{
				_settingsWindowPosition = _settingsWindow.transform.position;
				_settingsWindow.DestroyGameObjectImmediate();
				_settingsWindow = null;
			}
			
			if(UIAssetsLoader.allPrefabsReady && _settingsWindow == null)
				InitSettingsWindow();

			// we don't need to set global alpha as all the windows will be faded it to the setting
			SetGlobalScale(_UIScaleValue);
		}

		public void ShowKJRWindow()
		{
			RebuildUI();

			_settingsWindowFader.FadeTo(_UIAlphaValue, 0.1f, () => { appLauncherButton.SetTrue(false); GUIEnabled = true; });
		}

		public void HideKJRWindow()
		{
			if(_settingsWindowFader)
				_settingsWindowFader.FadeTo(0f, 0.1f, () =>
					{
						GUIEnabled = false;
						_settingsWindowPosition = _settingsWindow.transform.position;
						_settingsWindow.DestroyGameObjectImmediate();
						_settingsWindow = null;
						_settingsWindowFader = null;
					});
		}

		public void Update()
		{
			if(!GUIEnabled)
				return;

			if(!UIAssetsLoader.allPrefabsReady)
			{
				HideKJRWindow();

				GUIEnabled = false;
		//		appLauncherButton.SetFalse(false);
			}

			if(bInvalid)
				RebuildUI();
			
			if(EventSystem.current.currentSelectedGameObject != null && 
			   (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null
				|| EventSystem.current.currentSelectedGameObject.GetType() == typeof(InputField)))
			{
				if(!isKeyboardLocked)
					KeyboardLock(true); 
			}
			else
			{
				if(isKeyboardLocked)
					KeyboardLock(false);
			}
			
			// at this point we should have windows instantiated
			// all we need to do is update the fields
/* FEHLER, Update fehlt noch
			foreach(var paKJR in _servoUIControls)
			{
				if(!paKJR.Value.activeInHierarchy)
					continue;
				UpdateServoReadoutsFlight(paKJR.Key, paKJR.Value);
			}

			foreach(var paKJR in _servoGroupUIControls) 
			{
				if(!paKJR.Value.activeInHierarchy)
					continue;
				UpdateGroupReadoutsFlight (paKJR.Key, paKJR.Value);
			}
*/		}

		private void AddAppLauncherButton()
		{
			if((appLauncherButton != null) || !ApplicationLauncher.Ready || (ApplicationLauncher.Instance == null))
				return;

			try
			{
				Texture2D texture = UIAssetsLoader.iconAssets.Find(i => i.name == "icon_button");

				appLauncherButton = ApplicationLauncher.Instance.AddModApplication(
					ShowKJRWindow,
					HideKJRWindow,
					null, null, null, null,
					ApplicationLauncher.AppScenes.NEVER,
					texture);

				ApplicationLauncher.Instance.AddOnHideCallback(OnHideCallback);
			}
			catch(Exception ex)
			{
				Logger.Log(string.Format("[GUI AddAppLauncherButton Exception, {0}", ex.Message), Logger.Level.Fatal);
			}

			Invalidate();
		}

		private void OnHideCallback()
		{
			try
			{
				appLauncherButton.SetFalse(false);
			}
			catch(Exception)
			{}

			HideKJRWindow();
		}

		void OnGameSceneLoadRequestedForAppLauncher(GameScenes SceneToLoad)
		{
			DestroyAppLauncherButton();
		}

		private void DestroyAppLauncherButton()
		{
			try
			{
				if(appLauncherButton != null && ApplicationLauncher.Instance != null)
				{
					ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
					appLauncherButton = null;
				}

				if(ApplicationLauncher.Instance != null)
					ApplicationLauncher.Instance.RemoveOnHideCallback(OnHideCallback);
			}
			catch(Exception e)
			{
				Logger.Log("[GUI] Failed unregistering AppLauncher handlers," + e.Message);
			}
		}
		private void OnDestroy()
		{
			Logger.Log("[GUI] destroy");

			KeyboardLock(false);
			SaveConfigXml();

			if(_settingsWindow)
			{
				_settingsWindow.DestroyGameObject ();
				_settingsWindow = null;
				_settingsWindowFader = null;
			}

			GameEvents.onGUIApplicationLauncherReady.Remove (AddAppLauncherButton);
			GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequestedForAppLauncher);
			DestroyAppLauncherButton();

			GameEvents.onShowUI.Remove(OnShowUI);
			GameEvents.onHideUI.Remove(OnHideUI);

			Logger.Log("[GUI] OnDestroy finished successfully", Logger.Level.Debug);
		}

		internal void KeyboardLock(Boolean apply)
		{
			if(apply) // only do this lock in the editor - no point elsewhere
			{
				// only add a new lock if there isnt already one there
				if(InputLockManager.GetControlLock("KJRKeyboardLock") != ControlTypes.KEYBOARDINPUT)
				{
					Logger.Log(String.Format("[GUI] AddingLock-{0}", "KJRKeyboardLock"), Logger.Level.SuperVerbose);

					InputLockManager.SetControlLock(ControlTypes.KEYBOARDINPUT, "KJRKeyboardLock");
				}
			}
			else // otherwise make sure the lock is removed
			{
				// only try and remove it if there was one there in the first place
				if(InputLockManager.GetControlLock("KJRKeyboardLock") == ControlTypes.KEYBOARDINPUT)
				{
					Logger.Log(String.Format("[GUI] Removing-{0}", "KJRKeyboardLock"), Logger.Level.SuperVerbose);
					InputLockManager.RemoveControlLock("KJRKeyboardLock");
				}
			}

			isKeyboardLocked = apply;
		}

		public static Vector3 ClampWindowPosition(Vector3 windowPosition)
		{
			Canvas canvas = UIMasterController.Instance.appCanvas;
			RectTransform canvasRectTransform = canvas.transform as RectTransform;

			var windowPositionOnScreen = RectTransformUtility.WorldToScreenPoint(UIMasterController.Instance.uiCamera, windowPosition);

			float clampedX = Mathf.Clamp(windowPositionOnScreen.x, 0, Screen.width);
			float clampedY = Mathf.Clamp(windowPositionOnScreen.y, 0, Screen.height);

			windowPositionOnScreen = new Vector2(clampedX, clampedY);

			Vector3 newWindowPosition;
			if(RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRectTransform, 
				   windowPositionOnScreen, UIMasterController.Instance.uiCamera, out newWindowPosition))
				return newWindowPosition;
			else
				return Vector3.zero;
		}

		private void OnSave()
		{
			SaveConfigXml();
		}

		public void SaveConfigXml()
		{
			if(_settingsWindow)
				_settingsWindowPosition = _settingsWindow.transform.position;

			PluginConfiguration config = PluginConfiguration.CreateForType<WindowManager>();
			config.load();

			config.SetValue("dbg_controlWindowPosition", _settingsWindowPosition);
			config.SetValue("dbg_UIAlphaValue", (double)_UIAlphaValue);
			config.SetValue("dbg_UIScaleValue", (double)_UIScaleValue);
			config.SetValue("dbg_ShowKSPJoints", ShowKSPJoints);
			config.SetValue("dbg_ReinforceExistingJoints", ReinforceExistingJoints);
			config.SetValue("dbg_BuildAdditionalJointToParent", BuildAdditionalJointToParent);
			config.SetValue("dbg_ShowAdditionalJointToParent", ShowAdditionalJointToParent);
			config.SetValue("dbg_BuildMultiPartJointTreeChildren", BuildMultiPartJointTreeChildren);
			config.SetValue("dbg_ShowMultiPartJointTreeChildren", ShowMultiPartJointTreeChildren);
			config.SetValue("dbg_BuildMultiPartJointTreeChildrenRoot", BuildMultiPartJointTreeChildrenRoot);
			config.SetValue("dbg_ShowMultiPartJointTreeChildrenRoot", ShowMultiPartJointTreeChildrenRoot);
			config.SetValue("dbg_ReinforceInversions", ReinforceInversions);
			config.SetValue("dbg_ShowReinforcedInversions", ShowReinforcedInversions);

			config.save();
		}

		public void LoadConfigXml()
		{
			PluginConfiguration config = PluginConfiguration.CreateForType<WindowManager>();
			config.load();

			_settingsWindowPosition = config.GetValue<Vector3>("dbg_controlWindowPosition");

			_UIAlphaValue = (float)config.GetValue<double>("dbg_UIAlphaValue", 0.8);
			_UIScaleValue = (float)config.GetValue<double>("dbg_UIScaleValue", 1.0);
			ShowKSPJoints = config.GetValue<bool>("dbg_ShowKSPJoints", false);
			ReinforceExistingJoints = config.GetValue<bool>("dbg_ReinforceExistingJoints", true);
			BuildAdditionalJointToParent = config.GetValue<bool>("dbg_BuildAdditionalJointToParent", false);
			ShowAdditionalJointToParent = config.GetValue<bool>("dbg_ShowAdditionalJointToParent", false);
			BuildMultiPartJointTreeChildren = config.GetValue<bool>("dbg_BuildMultiPartJointTreeChildren", false);
			ShowMultiPartJointTreeChildren = config.GetValue<bool>("dbg_ShowMultiPartJointTreeChildren", false);
			BuildMultiPartJointTreeChildrenRoot = config.GetValue<bool>("dbg_BuildMultiPartJointTreeChildrenRoot", false);
			ShowMultiPartJointTreeChildrenRoot = config.GetValue<bool>("dbg_ShowMultiPartJointTreeChildrenRoot", false);
			ReinforceInversions = config.GetValue<bool>("dbg_ReinforceInversions", true);
			ShowReinforcedInversions = config.GetValue<bool>("dbg_ShowReinforcedInversions", false);
		}
	}

#endif
}
