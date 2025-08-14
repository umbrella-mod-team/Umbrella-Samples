using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using WIGU;
using WIGUx.Modules.MameHookModule;

namespace WIGUx.Modules.flightSimModule
{
	public class flightSimController : MonoBehaviour
	{
		static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

		[Header("Object Settings")]
		private Transform StartObject; // Reference to the Start button object
		private Transform StickObject; // Reference to the Stick mirroring object
		private Transform ThrottleObject; // Reference to the left stick mirroring object

		[Header("Input Settings")]
		public string primaryThumbstickHorizontal = "Horizontal"; // Input axis for primary thumbstick horizontal
		public string primaryThumbstickVertical = "Vertical"; // Input axis for primary thumbstick vertical
		public string secondaryThumbstickHorizontal = "RightStickHorizontal"; // Input axis for secondary thumbstick horizontal
		public string secondaryThumbstickVertical = "RightStickVertical"; // Input axis for secondary thumbstick forward/backward
		public string leftTrigger = "LIndexTrigger";
		public string rightTrigger = "RIndexTrigger";

		[Header("Velocity Multiplier Settings")]        // Speeds for the animation of the in game flight stick or wheel
		private float primaryThumbstickRotationMultiplier = 10f; // Multiplier for primary thumbstick rotation intensity
		private float secondaryThumbstickRotationMultiplier = 25f; // Multiplier for secondary thumbstick rotation intensity
		private float triggerRotationMultiplier = 20f; // Multiplier for trigger rotation intensity
		private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments
		private float StickXRotationDegrees = 10f; // Degrees for Stick rotation, adjust as needed
		private float StickYRotationDegrees = 10f; // Degrees for Stick rotation, adjust as needed
		private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
		private readonly float thumbstickVelocity = 50f;  // Velocity for keyboard input
		private readonly float centeringVelocityX = 25f;  // Velocity for centering rotation
		private readonly float centeringVelocityY = 25f;  // Velocity for centering rotation
		private readonly float centeringVelocityZ = 25f;  // Velocity for centering rotation

		[Header("Rotation Tracking")]        // Sets up the rotation varibles and sets them to 0 
		private float currentRotationX = 0f;  // Current rotation for X-axis
		private float currentRotationY = 0f;  // Current rotation for Y-axis
		private float currentRotationZ = 0f;  // Current rotation for Z-axis

		[Header("Rotation Limits")]        // Rotation Limits 
		[SerializeField] float minRotationX = -15f;
		[SerializeField] float maxRotationX = 15f;
		[SerializeField] float minRotationY = -15f;
		[SerializeField] float maxRotationY = 15f;
		[SerializeField] float minRotationZ = -15f;
		[SerializeField] float maxRotationZ = 15f;

		[Header("Position Settings")]     // Initial positions setup
		private Vector3 StickStartPosition; // Initial Throttle positions for resetting
		private Vector3 ThrottleStartPosition; // Initial Throttle positions for resetting


		[Header("Rotation Settings")]     // Initial rotations setup
		private Quaternion StickStartRotation;  // Initial Stick rotation for resetting
		private Quaternion ThrottleStartRotation;  // Initial Throttle rotation for resetting


		[Header("Lights and Emissives")]     // Setup Emissive and Lights
		private Light firelight1;
		private Light firelight2;
		private float lightDuration = 0.35f;
		private float attractFlashDuration = 0.7f;
		private float attractFlashDelay = 0.7f;
		private float dangerFlashDuration = 0.3f;
		private float dangerFlashDelay = 0.3f;
		private Coroutine dangerCoroutine; // Coroutine variable to control the focused danger mode
		private Coroutine attractCoroutine; // Coroutine variable to control the attract mode
		private Light[] lights;        //array of lights
		Dictionary<string, int> lastLampStates = new Dictionary<string, int>
			 {
			   { "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }
			 };
		[Header("Timers and States")]  // Store last states and timers
		private bool isFlashing = false; //set the flashing lights flag
		private bool isHigh = false; //set the high gear flag
		private bool inFocusMode = false;  // Flag to track focus mode state
		private bool isCenteringRotation = false; // Flag to track centering rotation state
		private GameSystemState systemState; //systemstate

		[Header("Collider Triggers")]
		[SerializeField] private Collider cockpitCollider;

		[Header("Rom Check")]
		private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
		private string insertedGameName = string.Empty;
		private string controlledGameName = string.Empty;
		private string configPath;
		private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects

		void Start()
		{
			CheckInsertedGameName();
			CheckControlledGameName();
			configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
			gameSystem = GetComponent<GameSystem>();
			InitializeLights();
			InitializeObjects();
			// if (firelight1) ToggleLight(firelight1, false);
			// if (firelight2) ToggleLight(firelight2, false);
			//if (StartObject) ToggleEmissive(StartObject.gameObject, false);
			// if (Hazard) ToggleEmissive(Hazard.gameObject, false);
			// if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, false);
			// StartAttractPattern();
		}

		void Update()
		{
			CheckInsertedGameName();
			CheckControlledGameName();
			if (WIGUx.Modules.MameHookModule.MameHookController.ActiveRomsList != null)
			{
				foreach (var rom in WIGUx.Modules.MameHookModule.MameHookController.ActiveRomsList)
				{
					if (rom == insertedGameName)
						ReadData();
				}
			}
			/*
            if (isCenteringRotation && !inputDetected)
            {
                bool centeredX = false, centeredY = false, centeredZ = false;

                // X axis
                float angleX = Quaternion.Angle(XObject.localRotation, XStartRotation);
                if (angleX > 0.01f)
                {
                    XObject.localRotation = Quaternion.RotateTowards(
                        XObject.localRotation,
                        XStartRotation,
                        centeringVelocityX * Time.deltaTime);
                    currentRotationX = Mathf.MoveTowards(
                        currentRotationX, 0f, centeringVelocityX * Time.deltaTime);
                }
                else
                {
                    XObject.localRotation = XStartRotation;
                    currentRotationX = 0f;
                    centeredX = true;
                }

                // Y axis
                float angleY = Quaternion.Angle(YObject.localRotation, YStartRotation);
                if (angleY > 0.01f)
                {
                    YObject.localRotation = Quaternion.RotateTowards(
                        YObject.localRotation,
                        YStartRotation,
                        centeringVelocityY * Time.deltaTime);
                    currentRotationY = Mathf.MoveTowards(
                        currentRotationY, 0f, centeringVelocityY * Time.deltaTime);
                }
                else
                {
                    YObject.localRotation = YStartRotation;
                    currentRotationY = 0f;
                    centeredY = true;
                }

                // Z axis
                float angleZ = Quaternion.Angle(ZObject.localRotation, ZStartRotation);
                if (angleZ > 0.01f)
                {
                    ZObject.localRotation = Quaternion.RotateTowards(
                        ZObject.localRotation,
                        ZStartRotation,
                        centeringVelocityZ * Time.deltaTime);
                    currentRotationZ = Mathf.MoveTowards(
                        currentRotationZ, 0f, centeringVelocityZ * Time.deltaTime);
                }
                else
                {
                    ZObject.localRotation = ZStartRotation;
                    currentRotationZ = 0f;
                    centeredZ = true;
                }

                if (centeredX && centeredY && centeredZ)
                {
                    isCenteringRotation = false;
                }
            }
            */
			// Enter focus when names match
			if (!string.IsNullOrEmpty(insertedGameName)
				&& !string.IsNullOrEmpty(controlledGameName)
				&& insertedGameName == controlledGameName
				&& !inFocusMode)
			{
				StartFocusMode();
			}
			if (GameSystem.ControlledSystem == null && inFocusMode)
			{
				EndFocusMode();
			}
			if (inFocusMode)
			{
				MapThumbsticks();
				MapButtons();
			}
		}


		void StartFocusMode()
		{
			logger.Debug($"{gameObject.name} Starting Focus Mode...");

			inFocusMode = true;  // Set focus mode flag
		}

		void EndFocusMode()
		{
			logger.Debug($"{gameObject.name} Exiting Focus Mode...");
			inFocusMode = false;  // Clear focus mode flag
		}

		void ResetPositions()      // Reset objects and cockpit cam to initial position and rotation
		{
			logger.Debug($"{gameObject.name} Resetting Positions");

			if (StickObject != null)
			{
				StickObject.localPosition = StickStartPosition;
				StickObject.localRotation = StickStartRotation;
			}
			if (ThrottleObject != null)
			{
				ThrottleObject.localPosition = ThrottleStartPosition;
				ThrottleObject.localRotation = ThrottleStartRotation;
			}
		}
		private const float THUMBSTICK_DEADZONE = 0.13f; // Adjust as needed

		private Vector2 ApplyDeadzone(Vector2 input, float deadzone)
		{
			input.x = Mathf.Abs(input.x) < deadzone ? 0f : input.x;
			input.y = Mathf.Abs(input.y) < deadzone ? 0f : input.y;
			return input;
		}
        private void MapThumbsticks()
        {
            if (!inFocusMode) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;
            float LIndexTrigger = 0f, RIndexTrigger = 0f;
            float primaryHandTrigger = 0f, secondaryHandTrigger = 0f;

            // === INPUT SELECTION WITH DEADZONE ===
            // OVR CONTROLLERS (adds to VR input if both are present)
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                primaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                secondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

                LIndexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
                RIndexTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
                primaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
                secondaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);

                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);
            }

            // STEAMVR CONTROLLERS (adds to VR input if both are present)
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                var leftController = SteamVRInput.GetController(HandType.Left);
                var rightController = SteamVRInput.GetController(HandType.Right);
                if (leftController != null) primaryThumbstick += leftController.GetAxis();
                if (rightController != null) secondaryThumbstick += rightController.GetAxis();

                LIndexTrigger = Mathf.Max(LIndexTrigger, SteamVRInput.GetTriggerValue(HandType.Left));
                RIndexTrigger = Mathf.Max(RIndexTrigger, SteamVRInput.GetTriggerValue(HandType.Right));

                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);
            }

            // XBOX CONTROLLER (adds to VR input if both are present)
            if (XInput.IsConnected)
            {
                primaryThumbstick += XInput.Get(XInput.Axis.LThumbstick);
                secondaryThumbstick += XInput.Get(XInput.Axis.RThumbstick);

                // Optionally use Unity Input axes as backup:
                primaryThumbstick += new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                secondaryThumbstick += new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));

                LIndexTrigger = Mathf.Max(LIndexTrigger, XInput.Get(XInput.Trigger.LIndexTrigger));
                RIndexTrigger = Mathf.Max(RIndexTrigger, XInput.Get(XInput.Trigger.RIndexTrigger));

                primaryThumbstick = ApplyDeadzone(primaryThumbstick, THUMBSTICK_DEADZONE);
                secondaryThumbstick = ApplyDeadzone(secondaryThumbstick, THUMBSTICK_DEADZONE);
            }
            // Map thumbstick for Stick 
            if (StickObject)
            {
                // Rotation applied on top of starting rotation
                Quaternion primaryRotation = Quaternion.Euler(
                    primaryThumbstick.y * StickYRotationDegrees,
                    0f,
                    -primaryThumbstick.x * StickXRotationDegrees
                );
                StickObject.localRotation = StickStartRotation * primaryRotation;
            }

            // Map triggers to throttle rotation on X-axis
            if (ThrottleObject)
            {
                // Triggers for throttle rotation on X-axis
                Quaternion triggerRotation = Quaternion.Euler(
                    (RIndexTrigger - LIndexTrigger) * triggerRotationMultiplier, // X-axis
                    0f,
                    0f
                );
                ThrottleObject.localRotation = ThrottleStartRotation * triggerRotation;
            }
        }

        private void MapButtons() // Pass by reference
		{
			if (!inFocusMode) return;

            // LeftTrigger
            if (
                XInput.GetDown(XInput.Button.LIndexTrigger)
                || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)
                || SteamVRInput.GetDown(SteamVRInput.TouchButton.LTrigger)
            )
            {
                //   if (brakelight1) ToggleLight(brakelight1, true);
                //   if (brakelight2) ToggleLight(brakelight2, true);
                //   if (StartObject) ToggleEmissive(StartObject.gameObject, true);
                //   if (Brakelight1) ToggleEmissive(Brakelight1.gameObject, true);
                //   if (Brakelight2) ToggleEmissive(Brakelight2.gameObject, true);
                //   if (Brakelight3) ToggleEmissive(Brakelight3.gameObject, true);
            }

            // Reset position on button release
            if (
                XInput.GetUp(XInput.Button.LIndexTrigger)
                || OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger)
                || SteamVRInput.GetUp(SteamVRInput.TouchButton.LTrigger)
            )
            {
                //    if (brakelight1) ToggleLight(brakelight1, false);
                //  if (brakelight2) ToggleLight(brakelight2, false);
                //    if (StartObject) ToggleEmissive(StartObject.gameObject, false);
                //     if (Brakelight1) ToggleEmissive(Brakelight1.gameObject, false);
                //     if (Brakelight2) ToggleEmissive(Brakelight2.gameObject, false);
                //      if (Brakelight3) ToggleEmissive(Brakelight3.gameObject, false);
            }

        }

        void CenterRotation()
		{
			isCenteringRotation = true;
		}

        /*
             void HandleTransformAdjustment()
        {
            if (!inFocusMode) return;

            bool cockpitCamMoved = false;
            bool vrCamMoved = false;

            // Move BOTH cameras if isRiding is true
            if (isRiding)
            {
                // Desktop camera (cockpitCam)
                if (cockpitCam != null)
                {
                    if (Input.GetKey(KeyCode.Home))
                    {
                        cockpitCam.transform.localPosition += Vector3.forward * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.End))
                    {
                        cockpitCam.transform.localPosition += Vector3.back * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.up * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.down * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.left * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        cockpitCam.transform.localPosition += Vector3.right * adjustSpeed * Time.deltaTime;
                        cockpitCamMoved = true;
                    }
                    if (Input.GetKeyDown(KeyCode.Backspace))
                    {
                        cockpitCam.transform.Rotate(0, 90, 0);
                        cockpitCamMoved = true;
                    }
                }

                // VR camera (vrCam)
                if (vrCam != null)
                {
                    if (Input.GetKey(KeyCode.Home))
                    {
                        vrCam.transform.localPosition += Vector3.forward * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.End))
                    {
                        vrCam.transform.localPosition += Vector3.back * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        vrCam.transform.localPosition += Vector3.up * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        vrCam.transform.localPosition += Vector3.down * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        vrCam.transform.localPosition += Vector3.left * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        vrCam.transform.localPosition += Vector3.right * adjustSpeed * Time.deltaTime;
                        vrCamMoved = true;
                    }
                    if (Input.GetKeyDown(KeyCode.Backspace))
                    {
                        vrCam.transform.Rotate(0, 90, 0);
                        vrCamMoved = true;
                    }
                }
            }

            // Save and log **only if there was a change**
            if (vrCam != null && vrCamMoved)
            {
                vrCamStartPosition = vrCam.transform.localPosition;
                vrCamStartRotation = vrCam.transform.localRotation;
                Debug.Log($"{gameObject.name}vrCam localPosition: " + vrCam.transform.localPosition.ToString("F4"));
            }
            if (cockpitCam != null && cockpitCamMoved)
            {
                cockpitCamStartPosition = cockpitCam.transform.localPosition;
                cockpitCamStartRotation = cockpitCam.transform.localRotation;
                Debug.Log($"{gameObject.name} cockpitCam localPosition: " + cockpitCam.transform.localPosition.ToString("F4"));
            }
        }
        */
        private void CheckInsertedGameName()
		{
			if (gameSystem != null && gameSystem.Game != null && !string.IsNullOrEmpty(gameSystem.Game.path))
				insertedGameName = FileNameHelper.GetFileName(gameSystem.Game.path);
			else
				insertedGameName = string.Empty;
		}

		private void CheckControlledGameName()
		{
			if (GameSystem.ControlledSystem != null && GameSystem.ControlledSystem.Game != null
				&& !string.IsNullOrEmpty(GameSystem.ControlledSystem.Game.path))
				controlledGameName = FileNameHelper.GetFileName(GameSystem.ControlledSystem.Game.path);
			else
				controlledGameName = string.Empty;
		}

		// Helper class to extract and sanitize file names.
		public static class FileNameHelper
		{
			// Extracts the file name without the extension and replaces invalid file characters with underscores.
			public static string GetFileName(string filePath)
			{
				string fileName = Path.GetFileNameWithoutExtension(filePath);
				string FileName = System.Text.RegularExpressions.Regex.Replace(fileName, "[\\/:*?\"<>|]", "_");
				return FileName;
			}
		}
        public static class KeyEmulator
        {
            // Virtual key codes for Q and E
            const byte VK_Q = 0x51;
            const byte VK_E = 0x45;
            const uint KEYEVENTF_KEYDOWN = 0x0000;
            const uint KEYEVENTF_KEYUP = 0x0002;

            [DllImport("user32.dll")]
            static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

            public static void SendQandEKeypress()
            {
                // Send Q down
                keybd_event(VK_Q, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                // Send E down
                keybd_event(VK_E, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

                // Send Q up
                keybd_event(VK_Q, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                // Send E up
                keybd_event(VK_E, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }
        void ToggleEmissiveRenderer(Renderer renderer, bool isOn)
		{
			if (isOn)
			{
				renderer.material.EnableKeyword("_EMISSION");
			}
			else
			{
				renderer.material.DisableKeyword("_EMISSION");
			}
		}

		void ToggleEmissive(GameObject targetObject, bool isActive)
		{
			if (targetObject != null)
			{
				Renderer renderer = targetObject.GetComponent<Renderer>();
				if (renderer != null)
				{
					Material material = renderer.material;

					if (isActive)
					{
						material.EnableKeyword("_EMISSION");
					}
					else
					{
						material.DisableKeyword("_EMISSION");
					}

					// logger.Debug($"{targetObject.name} emissive state set to {(isActive ? "ON" : "OFF")}.");
				}
				else
				{
					logger.Debug($"{gameObject.name} Renderer component not found on {targetObject.name}.");
				}
			}
			else
			{
				logger.Debug($"{gameObject.name} {targetObject.name} emissive object is not assigned.");
			}
		}
		void ToggleLight(Light targetLight, bool isActive)
		{
			if (targetLight == null) return;

			// Ensure the GameObject itself is active
			if (targetLight.gameObject.activeSelf != isActive)
				targetLight.gameObject.SetActive(isActive);

			// Then toggle the component
			targetLight.enabled = isActive;
		}

		void ReadData()
		{
			// 1) Your original “zeroed” lamp list:
			var currentLampStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
	{
		{ "lamp0", 0 }, { "lamp1", 0 }, { "lamp2", 0 }, { "lamp3", 0 }
	};

			// 2) Reflectively fetch the lamp list (falling back if needed)
			IEnumerable<string> lampList = null;
			var hookType = Type.GetType(
				"WIGUx.Modules.MameHookModule.MameHookController, WIGUx.Modules.MameHookModule"
			);
			if (hookType != null)
			{
				var lampProp = hookType.GetProperty(
					"currentLampState",
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
				);
				lampList = lampProp?.GetValue(null) as IEnumerable<string>;
			}
			if (lampList == null)
				lampList = MameHookController.currentLampState;

			// 3) Parse into your state dictionary
			if (lampList != null)
			{
				foreach (var entry in lampList)
				{
					var parts = entry.Split('|');
					if (parts.Length != 2) continue;

					string lamp = parts[0].Trim();
					if (currentLampStates.ContainsKey(lamp)
						&& int.TryParse(parts[1].Trim(), out int value))
					{
						currentLampStates[lamp] = value;
					}
				}
			}

			// 4) Dispatch only those lamps to your existing logic
			foreach (var kv in currentLampStates)
			{
				// matches: void ProcessLampState(string lampKey, Dictionary<string,int> currentStates)
				ProcessLampState(kv.Key, currentLampStates);
			}
		}
		// 🔹 Helper function for safe lamp processing
		void ProcessLampState(string lampKey, Dictionary<string, int> currentStates)
		{
			if (!lastLampStates.ContainsKey(lampKey))
			{
				lastLampStates[lampKey] = 0;
				logger.Error($"{gameObject.name} Added missing key '{lampKey}' to lastLampStates.");
			}

			if (currentStates.TryGetValue(lampKey, out int newValue))
			{
				if (lastLampStates[lampKey] != newValue)
				{
					lastLampStates[lampKey] = newValue;

					// Call the corresponding function dynamically
					switch (lampKey)
					{
						case "lamp0":
							ProcessLamp0(newValue);
							break;
						case "lamp1":
							ProcessLamp1(newValue);
							break;
						case "lamp2":
							ProcessLamp2(newValue);
							break;
						case "lamp3":
							ProcessLamp3(newValue);
							break;
						default:
							logger.Debug($"No processing function for '{lampKey}'");
							break;
					}
				}
			}
			else
			{
				logger.Error($"{gameObject.name} Lamp key '{lampKey}' not found in current states.");
			}
		}

		// Individual function for lamp0
		void ProcessLamp0(int state)
		{
			logger.Debug($"Lamp 0 updated: {state}");

			// Update lights

			// Update emissive material

		}
		// Individual function for lamp1
		void ProcessLamp1(int state)
		{
			logger.Debug($"Lamp 1 updated: {state}");

			// Update lights

			// Update emissive material

		}
		// Individual function for lamp2
		void ProcessLamp2(int state)
		{
			logger.Debug($"Lamp 2 updated: {state}");

			// Update lights

			// Update emissive material

		}

		// Individual function for lamp3
		void ProcessLamp3(int state)
		{
			logger.Debug($"Lamp 3 updated: {state}");

			// Update lights

			// Update emissive material

		}

		void CheckObject(GameObject obj, string name)     // Check if object is found and log appropriate message
		{
			if (obj == null)
			{
				logger.Error($"{gameObject.name} {name} not found!");
			}
			else
			{
				logger.Debug($"{gameObject.name} {name} found.");
			}
		}
		void ChangeColorEmissive(GameObject targetObject, Color emissionColor, float intensity, bool isActive)
		{
			if (targetObject != null)
			{
				Renderer renderer = targetObject.GetComponent<Renderer>();
				if (renderer != null)
				{
					Material material = renderer.material;

					if (isActive)
					{
						material.EnableKeyword("_EMISSION");
						material.SetColor("_EmissionColor", emissionColor * intensity);
					}
					else
					{
						material.DisableKeyword("_EMISSION");
					}

					//    logger.Debug($"{gameObject.name} {targetObject.name} emissive state set to {(isActive ? "ON" : "OFF")} with color {emissionColor} and intensity {intensity}.");
				}
				else
				{
					//    logger.Debug($"{gameObject.name} Renderer component not found on {targetObject.name}.");
				}
			}
			else
			{
				logger.Debug($"{gameObject.name} Target emissive object is not assigned.");
			}
		}


		IEnumerator attractPattern()  //Pattern For Attract Mode
		{
			yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 1f));
			while (true)
			{
				//  if (StartObject) ToggleEmissive(StartObject.gameObject, false);
				yield return new WaitForSeconds(attractFlashDuration);
				//   if (startObject) ToggleEmissive(startObject.gameObject, true);
				yield return new WaitForSeconds(attractFlashDelay);
			}
		}
		IEnumerator dangerPattern() //Pattern For Focused Danger Mode
		{
			while (true)
			{
				//   if (StartObject) ToggleEmissive(StartObject.gameObject, true);
				yield return new WaitForSeconds(dangerFlashDuration);
				//  if (StartObject) ToggleEmissive(StartObject.gameObject, false);
				yield return new WaitForSeconds(dangerFlashDelay);
			}
		}
		public void StartAttractPattern()
		{
			// Stop any currently running coroutines
			StopCurrentPatterns();
			attractCoroutine = StartCoroutine(attractPattern());
		}
		public void StartDangerPattern()
		{
			// Stop any currently running coroutines
			StopCurrentPatterns();
			dangerCoroutine = StartCoroutine(dangerPattern());
		}

		private void StopCurrentPatterns()
		{
			if (attractCoroutine != null)
			{
				StopCoroutine(attractCoroutine);
				attractCoroutine = null;
			}
			if (dangerCoroutine != null)
			{
				StopCoroutine(dangerCoroutine);
				dangerCoroutine = null;
			}
		}
		void InitializeLights()
		{
			// Gets all Light components in the target object and its children
			Light[] lights = transform.GetComponentsInChildren<Light>(true);

			foreach (Light light in lights)
			{
				switch (light.gameObject.name)
				{
					case "fire1light":
						firelight1 = light;
						logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
						break;
					case "fire2light":
						firelight2 = light;
						logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
						break;
					default:
						logger.Debug($"{gameObject.name} Excluded Light found in object: " + light.gameObject.name);
						break;
				}
			}
		}
		void InitializeObjects()
		{
			// Find Throttle under Z
			ThrottleObject = transform.Find("Throttle");
			if (ThrottleObject != null)
			{
				logger.Debug($"{gameObject.name} Throttle object found.");
				ThrottleStartPosition = ThrottleObject.localPosition;
				ThrottleStartRotation = ThrottleObject.localRotation;
			}
			else
			{
				logger.Debug($"{gameObject.name} Throttle object not found.");
			}

			// Find Stick under Z
			StickObject = transform.Find("Stick");
			if (StickObject != null)
			{
				logger.Debug($"{gameObject.name} Stick object found.");
				// Store initial position and rotation of the stick
				StickStartPosition = StickObject.localPosition;
				StickStartRotation = StickObject.localRotation;
			}
			else
			{
				logger.Debug($"{gameObject.name} Stick object not found.");
			}
		}
	}
}
