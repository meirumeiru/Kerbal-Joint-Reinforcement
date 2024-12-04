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

		public static WindowManager Instance
		{
			get { return _instance; }
		}

		private bool GUIHidden = false;

		// windows
		private static GameObject _settingsWindow;
		private static Vector3 _settingsWindowPosition;
		private static CanvasGroupFader _settingsWindowFader;

		// settings
		public static float _UIAlphaValue = 0.8f;
		public static float _UIScaleValue = 1.0f;
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
		public bool ShowReinforcedInversions = false;
		public bool ShowExtraStabilityJoints = false;

		internal bool GUIEnabled = false;

		private static bool isKeyboardLocked = false;

		private void Awake()
		{
			KJRJointUtils.LoadDefaults();
			KJRJointUtils.ApplyGameSettings();

			LoadConfigXml();

			KJRAnalyzer.OnLoad(ShowKSPJoints | ShowReinforcedInversions | ShowExtraStabilityJoints);

			_instance = this;
		}

		public void Start()
		{
			GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);

			GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequestedForAppLauncher);
			GameEvents.onGUIApplicationLauncherReady.Add(AddAppLauncherButton);

			GameEvents.onShowUI.Add(OnShowUI);
			GameEvents.onHideUI.Add(OnHideUI);
		}

		private void OnDestroy()
		{
			KeyboardLock(false);

			if(_settingsWindow)
			{
				_settingsWindow.DestroyGameObject ();
				_settingsWindow = null;
				_settingsWindowFader = null;
			}

			GameEvents.OnGameSettingsApplied.Remove(OnGameSettingsApplied);

			GameEvents.onGUIApplicationLauncherReady.Remove(AddAppLauncherButton);
			GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequestedForAppLauncher);
			DestroyAppLauncherButton();

			GameEvents.onShowUI.Remove(OnShowUI);
			GameEvents.onHideUI.Remove(OnHideUI);
		}

		private void OnGameSettingsApplied()
		{
			KJRJointUtils.ApplyGameSettings();
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
			OptReinforceExistingJoints.isOn = KJRJointUtils.reinforceAttachNodes;

			var OptReinforceInversions = AddNewOption(content, "Reinforce Inversions");
			OptReinforceInversions.isOn = KJRJointUtils.reinforceInversions;

			var OptShowReinforcedInversions = AddNewOption(content, "Show Reinforced Inversions");
			OptShowReinforcedInversions.isOn = ShowReinforcedInversions;

			var OptExtraStabilityJointLevel = GameObject.Instantiate(UIAssetsLoader.optionSliderLinePrefab);
			OptExtraStabilityJointLevel.transform.SetParent(content.transform, false);
			OptExtraStabilityJointLevel.GetChild("Label").GetComponent<Text>().text = "Extra Stability Joint Level";
			var JointLevelValue = OptExtraStabilityJointLevel.GetChild("Value").GetComponent<Text>();
			JointLevelValue.text = KJRJointUtils.extraLevel.ToString();
			var JointLevelSlider = OptExtraStabilityJointLevel.GetChild("Slider").GetComponent<Slider>();
			JointLevelSlider.value = KJRJointUtils.extraLevel;

			var OptExtraStabilityJointStrength = GameObject.Instantiate(UIAssetsLoader.optionInputLinePrefab);
			OptExtraStabilityJointStrength.transform.SetParent(content.transform, false);
			OptExtraStabilityJointStrength.GetChild("Label").GetComponent<Text>().text = "Extra Stability Joint Strength";
			var OptExtraStabilityJointStrengthValue = OptExtraStabilityJointStrength.GetChild("Value").GetComponent<InputField>();
			OptExtraStabilityJointStrengthValue.text = KJRJointUtils.extraLinearForceW.ToString();
			var OptExtraStabilityJointStrengthConstValue = OptExtraStabilityJointStrength.GetChild("ConstValue").GetComponent<Text>();
			OptExtraStabilityJointStrengthConstValue.text = KJRJointUtils.extraLinearForce.ToString();
			
			OptExtraStabilityJointStrength.SetActive(KJRJointUtils.extraLevel > 0);
			OptExtraStabilityJointStrengthValue.gameObject.SetActive(KJRJointUtils.extraLevel == 1);
			OptExtraStabilityJointStrengthConstValue.gameObject.SetActive(KJRJointUtils.extraLevel > 1);

			JointLevelSlider.onValueChanged.AddListener((v) =>
				{
					int extraLevel = (int)(JointLevelSlider.value + 0.1f);
					JointLevelValue.text = extraLevel.ToString();
					OptExtraStabilityJointStrength.SetActive(extraLevel > 0);
					OptExtraStabilityJointStrengthValue.gameObject.SetActive(extraLevel == 1);
					OptExtraStabilityJointStrengthConstValue.gameObject.SetActive(extraLevel > 1);
				});

			var OptShowExtraStabilityJoints = AddNewOption(content, "Show Extra Stability Joints");
			OptShowExtraStabilityJoints.isOn = ShowExtraStabilityJoints;

			var OptAutoStrutDisplay = AddNewOption(content, "Show AutoStruts");
			OptAutoStrutDisplay.isOn = PhysicsGlobals.AutoStrutDisplay;

			var footerButtons = _settingsWindow.GetChild("WindowFooter").GetChild("WindowFooterButtonsHLG");
	
			var cancelButton = footerButtons.GetChild("CancelButton").GetComponent<Button>();
			cancelButton.onClick.AddListener(() =>
				{
					OptShowKSPJoints.isOn = ShowKSPJoints;
					OptReinforceExistingJoints.isOn = KJRJointUtils.reinforceAttachNodes;
					OptReinforceInversions.isOn = KJRJointUtils.reinforceInversions;
					OptShowReinforcedInversions.isOn = ShowReinforcedInversions;
					JointLevelValue.text = KJRJointUtils.extraLevel.ToString();
					JointLevelSlider.value = KJRJointUtils.extraLevel;
					OptExtraStabilityJointStrength.SetActive(KJRJointUtils.extraLevel > 0);
					OptExtraStabilityJointStrengthValue.gameObject.SetActive(KJRJointUtils.extraLevel == 1);
					OptExtraStabilityJointStrengthConstValue.gameObject.SetActive(KJRJointUtils.extraLevel > 1);
					OptShowExtraStabilityJoints.isOn = ShowExtraStabilityJoints;
					OptAutoStrutDisplay.isOn = PhysicsGlobals.AutoStrutDisplay;
				});
	
			var defaultButton = footerButtons.GetChild("DefaultButton").GetComponent<Button>();
			defaultButton.onClick.AddListener(() =>
				{
					// remarks: here "default" means hard coded fix values

					KJRSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KJRSettings>();

					bool bCycle = false;

					OptShowKSPJoints.isOn = ShowKSPJoints = false;

					if(!KJRJointUtils.reinforceAttachNodes)
						bCycle = true;
					if(settings != null) settings.reinforceAttachNodes = true;
					OptReinforceExistingJoints.isOn = KJRJointUtils.reinforceAttachNodes = true;

					if(!KJRJointUtils.reinforceInversions)
						bCycle = true;
					if(settings != null) settings.reinforceInversions = true;
					OptReinforceInversions.isOn = KJRJointUtils.reinforceInversions = true;

					OptShowReinforcedInversions.isOn = ShowReinforcedInversions = false;

					if(KJRJointUtils.extraLevel != 0)
						bCycle = true;
					JointLevelValue.text = KJRJointUtils.extraLevel.ToString();
					if(settings != null) { settings.extraJoints = false; settings.extraLevel = 1; }
					JointLevelSlider.value = KJRJointUtils.extraLevel = 0;

					OptExtraStabilityJointStrength.SetActive(false);
					KJRJointUtils.extraLinearForceW = 100f;
					KJRJointUtils.extraAngularForceW = 100f;

					OptShowExtraStabilityJoints.isOn = ShowExtraStabilityJoints = false;
	
					OptAutoStrutDisplay.isOn = PhysicsGlobals.AutoStrutDisplay = false;

					KJRAnalyzer.Show = ShowKSPJoints | ShowReinforcedInversions | ShowExtraStabilityJoints;

					if(HighLogic.LoadedSceneIsFlight && bCycle)
						KJRManager.Instance.UpdateAllVessels();

					SaveConfigXml();
					GameSettings.SaveSettings();
				});
	
			var applyButton = footerButtons.GetChild("ApplyButton").GetComponent<Button>();
			applyButton.onClick.AddListener(() => 
				{
					KJRSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KJRSettings>();

					bool bCycle = false;

					ShowKSPJoints = OptShowKSPJoints.isOn;

					if(KJRJointUtils.reinforceAttachNodes != OptReinforceExistingJoints.isOn)
					{
						bCycle = true;
						if(settings != null) settings.reinforceAttachNodes = OptReinforceExistingJoints.isOn;
						KJRJointUtils.reinforceAttachNodes = OptReinforceExistingJoints.isOn;
					}

					if(KJRJointUtils.reinforceInversions != OptReinforceInversions.isOn)
					{
						bCycle = true;
						if(settings != null) settings.reinforceInversions = OptReinforceInversions.isOn;
						KJRJointUtils.reinforceInversions = OptReinforceInversions.isOn;
					}

					ShowReinforcedInversions = OptShowReinforcedInversions.isOn;

					int extraLevel = (int)(JointLevelSlider.value + 0.1f);

					if(KJRJointUtils.extraLevel != extraLevel)
					{
						bCycle = true;
						if(settings != null) { settings.extraJoints = (extraLevel > 0); settings.extraLevel = Math.Max(1, extraLevel); }
						KJRJointUtils.extraLevel = extraLevel;
					}

					float extraLinearForceW;
					if(float.TryParse(OptExtraStabilityJointStrengthValue.text, out extraLinearForceW)
					&& (KJRJointUtils.extraLinearForceW != extraLinearForceW))
					{
						if(KJRJointUtils.extraLevel == 1)
							bCycle = true;

						KJRJointUtils.extraLinearForceW = extraLinearForceW;
						KJRJointUtils.extraAngularForceW = extraLinearForceW;
					}

					ShowExtraStabilityJoints = OptShowExtraStabilityJoints.isOn;

					PhysicsGlobals.AutoStrutDisplay = OptAutoStrutDisplay.isOn;

					KJRAnalyzer.Show = ShowKSPJoints | ShowReinforcedInversions | ShowExtraStabilityJoints;

					if(HighLogic.LoadedSceneIsFlight && bCycle)
						KJRManager.Instance.UpdateAllVessels();

					SaveConfigXml();
					GameSettings.SaveSettings();
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
		}

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
				Logger.Log(string.Format("[GUI AddAppLauncherButton Exception, {0}", ex.Message), Logger.Level.Error);
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
			config.SetValue("dbg_ShowReinforcedInversions", ShowReinforcedInversions);
			config.SetValue("dbg_ShowExtraStabilityJoints", ShowExtraStabilityJoints);

			config.SetValue("reinforceAttachNodes", KJRJointUtils.reinforceAttachNodes);
			config.SetValue("reinforceInversions", KJRJointUtils.reinforceInversions);
			config.SetValue("extraLevel", KJRJointUtils.extraLevel);
			config.SetValue("extraLinearForceW", KJRJointUtils.extraLinearForceW);
			config.SetValue("extraAngularForceW", KJRJointUtils.extraAngularForceW);

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
			ShowReinforcedInversions = config.GetValue<bool>("dbg_ShowReinforcedInversions", false);
			ShowExtraStabilityJoints = config.GetValue<bool>("dbg_ShowExtraStabilityJoints", false);
		}
	}

#endif
}
