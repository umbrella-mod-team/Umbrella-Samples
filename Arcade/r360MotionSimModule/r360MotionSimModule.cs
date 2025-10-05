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

namespace WIGUx.Modules.r360MotionSim
{
	public class r360MotionSimController : MonoBehaviour
	{
		static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

		[Header("Object Settings")]
		private Transform StickObject; // Reference to the stick mirroring object
		private Transform ThrottleObject; // Reference to the left stick mirroring object
		private Transform XObject; // Reference to the main X object
		private Transform YObject; // Reference to the main Y object
		private Transform ZObject; // Reference to the main Z object
		private GameObject cockpitCam; 
		private GameObject vrCam;    // Reference to the VR Camera  
		private GameObject playerCamera;   // Reference to the Player Camera
		private GameObject playerVRSetup;   // Reference to the VR Camera

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
		private float StickYRotationDegrees = 15f; // Degrees for Stick rotation, adjust as needed
		private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
		private readonly float thumbstickVelocity = 75f;  // Velocity for keyboard input
		private readonly float centeringVelocityX = 75f;  // Velocity for centering rotation
		private readonly float centeringVelocityY = 75f;  // Velocity for centering rotation
		private readonly float centeringVelocityZ = 75f;  // Velocity for centering rotation

		[Header("Rotation Tracking")]        // Sets up the rotation varibles and sets them to 0 
		private float currentRotationX = 0f;  // Current rotation for X-axis
		private float currentRotationY = 0f;  // Current rotation for Y-axis
		private float currentRotationZ = 0f;  // Current rotation for Z-axis

		[Header("Rotation Limits")]        // Rotation Limits 
		[SerializeField] float minRotationX = -1080f;
		[SerializeField] float maxRotationX = 1080f;
		[SerializeField] float minRotationY = -0f;
		[SerializeField] float maxRotationY = 0f;
		[SerializeField] float minRotationZ = -1080f;
		[SerializeField] float maxRotationZ = 1080f;

		[Header("Position Settings")]     // Initial positions setup
		private Vector3 XStartPosition;  // Initial X position for resetting
		private Vector3 YStartPosition;  // Initial Y positions for resetting
		private Vector3 ZStartPosition;  // Initial Z positions for resetting
		private Vector3 StickStartPosition; // Initial Throttle positions for resetting
		private Vector3 ThrottleStartPosition; // Initial Throttle positions for resetting
		private Vector3 playerCameraStartPosition;  // Initial Player Camera positions for resetting
		private Vector3 playerVRSetupStartPosition;  // Initial PlayerVR positions for resetting
		private Vector3 cockpitCamStartPosition;  // Initial cockpitCam positionsfor resetting
		private Vector3 vrCamStartPosition;    // Initial vrCam positionsfor resetting
        private Vector3 playerCameraInitialWorldScale; // Initial Player Camera world scale for resetting
        private Vector3 playerVRSetupInitialWorldScale; // Initial PlayerVR world scale for resetting

        [Header("Rotation Settings")]     // Initial rotations setup
		private Quaternion XStartRotation;  // Initial X rotation for resetting
		private Quaternion YStartRotation;  // Initial Y rotation for resetting
		private Quaternion ZStartRotation;  // Initial Z rotation for resetting
		private Quaternion StickStartRotation;  // Initial Stick rotation for resetting
		private Quaternion ThrottleStartRotation;  // Initial Throttle rotation for resetting
		private Quaternion playerCameraStartRotation;  // Initial Player Camera rotation for resetting
		private Quaternion playerVRSetupStartRotation;  // Initial PlayerVR rotation for resetting
		private Quaternion cockpitCamStartRotation;  // Initial cockpitCam rotation for resetting
		private Quaternion vrCamStartRotation;      // Initial VRCam rotation for resetting

		[Header("Lights and Emissives")]     // Setup Emissive and Lights
		private Transform thrustObject;
		public Renderer[] frontEmissiveObjects;
		public Renderer[] leftEmissiveObjects;
		public Renderer[] rightEmissiveObjects;
		private Coroutine frontCoroutine;
		private Coroutine leftCoroutine;
		private Coroutine rightCoroutine;
		private float frontFlashDuration = 0.4f;
		private float frontFlashDelay = 0.17f;
		private float sideFlashDuration = 0.02f;
		private float sideFlashDelay = 0.005f;
		private float frontDangerDuration = 0.2f;
		private float frontDangerDelay = 0.2f;
		private float sideDangerDuration = 0.1f;
		private float sideDangerDelay = 0.2f;
        public Light start;
        public Light danger;
        public Light crash;
		public Light emergency_stop;
        private Transform Start_lampObject; // Reference to the Fire left light emissive
        private Transform Danger_lampObject; // Reference to the Fire left light emissive
        private Transform Emergency_stop_lampObject; // Reference to the Fire left light emissive
        private Light[] lights;        //array of lights
		public float lightDuration = 0.35f; // Duration during which the lights will be on        
		Dictionary<string, int> lastLampStates = new Dictionary<string, int>
			 {
               { "start_lamp", 0 }, { "danger_lamp", 0 }, { "crash_lamp", 0 }, { "emergency_stop_lamp", 0 }
             };

		[Header("Timers and States")]  // Store last states and timers
		private bool inFocusMode = false;  // Flag to track focus mode state
		private bool isCenteringRotation = false; // Flag to track centering rotation state
		private bool isRiding = false; // Set riding state to false
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
			InitializeEmissiveArrays();
			StartAttractPattern();
            if (start) ToggleLight(start, false);
            if (Start_lampObject) ToggleEmissive(Start_lampObject.gameObject, false);
            if (danger) ToggleLight(danger, false);
            if (Danger_lampObject) ToggleEmissive(Danger_lampObject.gameObject.gameObject, false);
            if (crash) ToggleLight(crash, false);
            if (emergency_stop) ToggleLight(emergency_stop, false);
            if (Emergency_stop_lampObject) ToggleEmissive(Emergency_stop_lampObject.gameObject, false);
        }

		void Update()
		{
			bool inputDetected = false;  // Initialize for centering

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
				MapThumbsticks(ref inputDetected);
				MapButtons(ref inputDetected);
				HandleTransformAdjustment();
			}
		}
		void ReadData()
		{
			// 1) Your original “zeroed” lamp list:
			var currentLampStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
	{
		{ "start_lamp", 0 }, { "danger_lamp", 0 }, { "crash_lamp", 0 }, { "emergency_stop_lamp", 0 }
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
		void StartFocusMode()
		{
			logger.Debug("Compatible Rom Detected, Unlocking Cabinet...");
			logger.Debug($"{gameObject.name} Starting Focus Mode...");
			StopCurrentPatterns();
			StartDangerPattern();
			if (cockpitCam != null)
			{
				cockpitCam.transform.localPosition = cockpitCamStartPosition;
				cockpitCam.transform.localRotation = cockpitCamStartRotation;
			}
			if (vrCam != null)
			{
				vrCam.transform.localPosition = vrCamStartPosition;
				vrCam.transform.localRotation = vrCamStartRotation;
			}
			if (playerCamera != null)
			{
				playerCameraStartPosition = playerCamera.transform.position;
				playerCameraStartRotation = playerCamera.transform.rotation;
			}

			if (playerVRSetup != null)
			{
				playerVRSetupStartPosition = playerVRSetup.transform.position;
				playerVRSetupStartRotation = playerVRSetup.transform.rotation;
			}

			// Check containment
			bool inside = false;
			if (cockpitCollider != null)
			{
				Vector3 camPos = playerCamera.transform.position;
				bool boundsContains = cockpitCollider.bounds.Contains(camPos);
				Vector3 closest = cockpitCollider.ClosestPoint(camPos);
				inside = (closest == camPos);
				//    logger.Debug($"Containment check - bounds.Contains: {boundsContains}, ClosestPoint==pos: {inside}");
			}

            if (cockpitCollider != null && inside)
            {
                if (playerVRSetup == null)
                {
                    // Parent and apply offset to PlayerCamera
                    SaveOriginalParent(playerCamera);
                    Vector3 worldPos = playerCamera.transform.position;
                    Quaternion worldRot = playerCamera.transform.rotation;
                    playerCameraInitialWorldScale = playerCamera.transform.lossyScale;
                    KeyEmulator.SendQandEKeypress();
                    playerCamera.transform.SetParent(cockpitCam.transform, false);
                    playerCamera.transform.position = worldPos;
                    playerCamera.transform.rotation = worldRot;
                    NormalizeWorldScale(playerCamera, cockpitCam.transform);

                    logger.Debug($"{gameObject.name} Player is aboard and strapped in.");
                    isRiding = true; // Set riding state to true
                }
                if (playerVRSetup != null)
                {
                    // Parent and apply offset to PlayerVRSetup
                    SaveOriginalParent(playerVRSetup);
                    Vector3 worldPos = playerVRSetup.transform.position;
                    Quaternion worldRot = playerVRSetup.transform.rotation;
                    playerVRSetupInitialWorldScale = playerVRSetup.transform.lossyScale;
                    KeyEmulator.SendQandEKeypress();
                    playerVRSetup.transform.SetParent(vrCam.transform, false);
                    playerVRSetup.transform.position = worldPos;
                    playerVRSetup.transform.rotation = worldRot;
                    NormalizeWorldScale(playerVRSetup, vrCam.transform);

                    logger.Debug($"{gameObject.name} VR Player is aboard and strapped in!");
                    logger.Debug("Remember Your Hick manuver I don't want you passing out!");
                    logger.Debug("Gloc R360 Motion Sim Starting...");
                    logger.Debug("Dont Get Dizzy!");
					isRiding = true; // Set riding state to true
				}
			}
			else
			{
				logger.Debug("Player is not aboard the ride, Starting Without the player aboard.");
				isRiding = false; // Set riding state to false
			}

			inFocusMode = true;
		}

		void EndFocusMode()
		{
			logger.Debug("Exiting Focus Mode...");
			RestoreOriginalParent(playerCamera, "PlayerCamera");
			RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");
            if (start) ToggleLight(start, false);
            if (Start_lampObject) ToggleEmissive(Start_lampObject.gameObject, false);
            if (danger) ToggleLight(danger, false);
            if (Danger_lampObject) ToggleEmissive(Danger_lampObject.gameObject.gameObject, false);
            if (crash) ToggleLight(crash, false);
            if (emergency_stop) ToggleLight(emergency_stop, false);
            if (Emergency_stop_lampObject) ToggleEmissive(Emergency_stop_lampObject.gameObject, false);
            // Restore original parents of objects
            StartAttractPattern();
			ResetPositions();
			inFocusMode = false;  // Clear focus mode flag
		}
		private const float THUMBSTICK_DEADZONE = 0.13f; // Adjust as needed

		private Vector2 ApplyDeadzone(Vector2 input, float deadzone)
		{
			input.x = Mathf.Abs(input.x) < deadzone ? 0f : input.x;
			input.y = Mathf.Abs(input.y) < deadzone ? 0f : input.y;
			return input;
		}
        private void MapThumbsticks(ref bool inputDetected)
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
				if (Mathf.Abs(primaryThumbstick.x) > 0.01f || Mathf.Abs(primaryThumbstick.y) > 0.01f)
					inputDetected = true;
				isCenteringRotation = false; // Set if thumbstick moved
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

            // X ROTATION (Pitch, up/down on stick, XObject)
            if (primaryThumbstick.y != 0f)
			{
				float inputValue = primaryThumbstick.y * thumbstickVelocity * Time.deltaTime;
				float targetX = Mathf.Clamp(currentRotationX + inputValue, minRotationX, maxRotationX);
				float rotateX = targetX - currentRotationX;
				if (Mathf.Abs(rotateX) > 0.0001f)
				{
					XObject.Rotate(rotateX, 0f, 0f);
					currentRotationX = targetX;
					inputDetected = true;
					isCenteringRotation = false;
				}
			}

			// Y ROTATION (Yaw, left/right on stick, YObject)
			if (primaryThumbstick.x != 0f)
			{
				float inputValue = primaryThumbstick.x * thumbstickVelocity * Time.deltaTime;
				float targetY = Mathf.Clamp(currentRotationY + inputValue, minRotationY, maxRotationY);
				float rotateY = targetY - currentRotationY;
				if (Mathf.Abs(rotateY) > 0.0001f)
				{
					YObject.Rotate(0f, rotateY, 0f);
					currentRotationY = targetY;
					inputDetected = true;
					isCenteringRotation = false;
				}
			}

			// Z ROTATION (Roll, e.g., left/right on primary stick, ZObject)
			if (primaryThumbstick.x != 0f)
			{
				float inputValue = -primaryThumbstick.x * thumbstickVelocity * Time.deltaTime;
				float targetZ = Mathf.Clamp(currentRotationZ + inputValue, minRotationZ, maxRotationZ);
				float rotateZ = targetZ - currentRotationZ;
				if (Mathf.Abs(rotateZ) > 0.0001f)
				{
					ZObject.Rotate(0f, 0f, rotateZ);
					currentRotationZ = targetZ;
					inputDetected = true;
					isCenteringRotation = false;
				}
			}
			if (!inputDetected)
			{
				CenterRotation();    // Center the rotation if no input is detected
			}
		}

		private void MapButtons(ref bool inputDetected) // Pass by reference
		{
			if (!inFocusMode) return;
            // Fire3/X button pressed (turn on lights)
            if (XInput.GetDown(XInput.Button.X)
                || OVRInput.GetDown(OVRInput.Button.Two) // Oculus "B"/"Y" (use .Two for canonical red B right)
                || SteamVRInput.GetDown(SteamVRInput.TouchButton.B) // SteamVR top button on right controller
            )
            {
                if (Danger_lampObject) ToggleEmissive(Danger_lampObject.gameObject.gameObject, true);
                if (danger) ToggleLight(danger, true);
            }

            // Fire3/X button released (turn off lights)
            if (XInput.GetUp(XInput.Button.X)
                || OVRInput.GetUp(OVRInput.Button.Two)
                || SteamVRInput.GetUp(SteamVRInput.TouchButton.B)
            )
            {
                if (Danger_lampObject) ToggleEmissive(Danger_lampObject.gameObject.gameObject, false);
                if (danger) ToggleLight(danger, false);
            }
        }

        void ResetPositions()
		{
			logger.Debug("Resetting Positions");
			// Reset X to initial positions and rotations
			if (XObject != null)
			{
				XObject.localPosition = XStartPosition;
				XObject.localRotation = XStartRotation;
			}

			// Reset Y object to initial position and rotation
			if (YObject != null)
			{
				YObject.localPosition = YStartPosition;
				YObject.localRotation = YStartRotation;
			}
			// Reset Z object to initial position and rotation
			if (ZObject != null)
			{
				ZObject.localPosition = ZStartPosition;
				ZObject.localRotation = ZStartRotation;
			}
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
			if (isRiding == true)
			{
				if (cockpitCam != null)
				{
					cockpitCam.transform.localPosition = cockpitCamStartPosition;
					cockpitCam.transform.localRotation = cockpitCamStartRotation;
				}
				if (vrCam != null)
				{
					vrCam.transform.localPosition = vrCamStartPosition;
					vrCam.transform.localRotation = vrCamStartRotation;
				}
				if (playerVRSetup != null)
				{
					playerVRSetup.transform.position = playerVRSetupStartPosition;
					playerVRSetup.transform.rotation = playerVRSetupStartRotation;
				}
				if (playerCamera != null)
				{
					playerCamera.transform.position = playerCameraStartPosition;
					playerCamera.transform.rotation = playerCameraStartRotation;
				}
				isRiding = false; // Set riding state to false
			}
			else
			{
				logger.Debug($"{gameObject.name} Player was not aboard the ride, skipping reset.");
			}

			currentRotationX = 0f;
			currentRotationY = 0f;
			currentRotationZ = 0f;
		}

		void CenterRotation()
		{
			isCenteringRotation = true;
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
						case "start_lamp":
							ProcessLamp0(newValue);
							break;
						case "emergency_stop_lamp":
							ProcessLamp1(newValue);
							break;
						case "danger_lamp":
							ProcessLamp2(newValue);
							break;
                        case "crash_lamp":
                            ProcessLamp3(newValue);
                            break;
                        default:
							logger.Warning($"No processing function for '{lampKey}'");
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
			logger.Debug($"start_lamp updated: {state}");

			// Update lights
			if (start) ToggleLight(start, state == 1);
			// Update emissive material
			if (Start_lampObject) ToggleEmissive(Start_lampObject.gameObject, state == 1);
		}
		// Individual function for lamp1
		void ProcessLamp1(int state)
        {
            logger.Debug($"emergency_stop_lamp updated: {state}");

            // Update lights
            if (emergency_stop) ToggleLight(emergency_stop, state == 1);
            // Update emissive material
            if (Emergency_stop_lampObject) ToggleEmissive(Emergency_stop_lampObject.gameObject, state == 1);
        }
		
		// Individual function for lamp2
		void ProcessLamp2(int state)
        {
            logger.Debug($"danger_lamp updated: {state}");

            // Update lights
            if (danger) ToggleLight(danger, state == 1);
            // Update emissive material
            if (Danger_lampObject) ToggleEmissive(Danger_lampObject.gameObject.gameObject, state == 1);
        }
	
        void ProcessLamp3(int state)
        {
            logger.Debug($"crash_lamp updated: {state}");

            // Update lights
            if (crash) ToggleLight(crash, state == 1);
            // Update emissive material
        }

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

        // Method to disable emission
        void DisableEmission(Renderer[] emissiveObjects)
		{
			foreach (var renderer in emissiveObjects)
			{
				if (renderer != null)
				{
					renderer.material.DisableKeyword("_EMISSION");
				}
				else
				{
					logger.Debug("Renderer component not found on one of the emissive objects.");
				}
			}
		}

		// Method to log missing objects
		void LogMissingObject(Renderer[] emissiveObjects, string arrayName)
		{
			for (int i = 0; i < emissiveObjects.Length; i++)
			{
				if (emissiveObjects[i] == null)
				{
					//    logger.Error($"{arrayName} object at index {i} not found.");
				}
			}
		}

		// Attract pattern for the front
		IEnumerator FrontAttractPattern()
		{
			int previousStep = -1; // Track the previous step

			while (true)
			{
				// Iterate through each "step" (0 to 3, corresponding to "step 1" to "step 4")
				for (int step = 0; step < 4; step++)
				{
					// Turn on all lights for the current step
					for (int group = step; group < frontEmissiveObjects.Length; group += 4)
					{
						ToggleEmissive(frontEmissiveObjects[group], true);
					}

					// If there was a previous step, wait before turning off its lights
					if (previousStep >= 0)
					{
						yield return new WaitForSeconds(frontFlashDelay);

						// Turn off the previous step's lights
						for (int group = previousStep; group < frontEmissiveObjects.Length; group += 4)
						{
							ToggleEmissive(frontEmissiveObjects[group], false);
						}
					}

					// Update the previous step
					previousStep = step;

					// Wait for the duration before moving to the next step
					yield return new WaitForSeconds(frontFlashDuration);
				}

				// Turn off the last step's lights after the loop
				yield return new WaitForSeconds(frontFlashDelay);
				for (int group = previousStep; group < frontEmissiveObjects.Length; group += 4)
				{
					ToggleEmissive(frontEmissiveObjects[group], false);
				}
				previousStep = -1; // Reset previous step for the next cycle
			}
		}

		// Attract pattern for the side
		IEnumerator SideAttractPattern(Renderer[] emissiveObjects)
		{
			int previousIndex = -1; // Track the previous light index

			while (true)
			{
				for (int i = 0; i < emissiveObjects.Length; i++)
				{
					// Turn on the current light
					ToggleEmissive(emissiveObjects[i], true);

					// If there was a previous light, wait before turning it off
					if (previousIndex >= 0)
					{
						yield return new WaitForSeconds(sideFlashDelay); // Use the sideFlashDelay for the overlap timing
						ToggleEmissive(emissiveObjects[previousIndex], false);
					}

					// Update the previous light index
					previousIndex = i;

					// Wait before moving to the next light
					yield return new WaitForSeconds(sideFlashDuration);
				}

				// Turn off the last light after the loop
				yield return new WaitForSeconds(sideFlashDelay);
				ToggleEmissive(emissiveObjects[previousIndex], false);
				previousIndex = -1; // Reset previous index for the next cycle
			}
		}

		IEnumerator FrontDangerPattern()
		{
			while (true)
			{
				// Flash even-numbered lights
				for (int i = 1; i < frontEmissiveObjects.Length; i += 2)
				{
					ToggleEmissive(frontEmissiveObjects[i], true);
				}
				yield return new WaitForSeconds(frontDangerDuration);

				// Turn off even-numbered lights
				for (int i = 1; i < frontEmissiveObjects.Length; i += 2)
				{
					ToggleEmissive(frontEmissiveObjects[i], false);
				}

				// Flash odd-numbered lights
				for (int i = 0; i < frontEmissiveObjects.Length; i += 2)
				{
					ToggleEmissive(frontEmissiveObjects[i], true);
				}
				yield return new WaitForSeconds(frontDangerDuration);

				// Turn off odd-numbered lights
				for (int i = 0; i < frontEmissiveObjects.Length; i += 2)
				{
					ToggleEmissive(frontEmissiveObjects[i], false);
				}

				yield return new WaitForSeconds(frontDangerDelay);
			}
		}

		// Danger pattern for the sides
		IEnumerator SideDangerPattern(Renderer[] emissiveObjects)
		{
			while (true)
			{
				// Flash even-numbered lights in each group
				for (int group = 1; group < 3; group += 2) // This iterates over the second light in each group (index 1, 4, 7, 10, 13)
				{
					for (int i = group; i < emissiveObjects.Length; i += 3)
					{
						ToggleEmissive(emissiveObjects[i], true);
					}
				}
				yield return new WaitForSeconds(sideDangerDuration);

				// Turn off even-numbered lights
				for (int group = 1; group < 3; group += 2)
				{
					for (int i = group; i < emissiveObjects.Length; i += 3)
					{
						ToggleEmissive(emissiveObjects[i], false);
					}
				}

				// Flash odd-numbered lights in each group
				for (int group = 0; group < 3; group += 2) // This iterates over the first and third lights in each group (index 0, 3, 6, 9, 12)
				{
					for (int i = group; i < emissiveObjects.Length; i += 3)
					{
						ToggleEmissive(emissiveObjects[i], true);
					}
				}
				yield return new WaitForSeconds(sideDangerDuration);

				// Turn off odd-numbered lights
				for (int group = 0; group < 3; group += 2)
				{
					for (int i = group; i < emissiveObjects.Length; i += 3)
					{
						ToggleEmissive(emissiveObjects[i], false);
					}
				}

				yield return new WaitForSeconds(sideDangerDelay);
			}
		}


		// Method to toggle emissive on or off
		void ToggleEmissive(Renderer renderer, bool isOn)
		{
			if (renderer != null)
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
		}

		// Method to toggle all in the array
		void ToggleAll(Renderer[] emissiveObjects, bool isOn)
		{
			foreach (var renderer in emissiveObjects)
			{
				ToggleEmissive(renderer, isOn);
			}
		}

		public void TurnAllOff()
		{
			ToggleAll(frontEmissiveObjects, false);
			ToggleAll(leftEmissiveObjects, false);
			ToggleAll(rightEmissiveObjects, false);
		}

		public void StartAttractPattern()
		{
			StopCurrentPatterns();

			frontCoroutine = StartCoroutine(FrontAttractPattern());
			leftCoroutine = StartCoroutine(SideAttractPattern(leftEmissiveObjects));
			rightCoroutine = StartCoroutine(SideAttractPattern(rightEmissiveObjects));
		}

		public void StartDangerPattern()
		{
			StopCurrentPatterns();

			frontCoroutine = StartCoroutine(FrontDangerPattern());
			leftCoroutine = StartCoroutine(SideDangerPattern(leftEmissiveObjects));
			rightCoroutine = StartCoroutine(SideDangerPattern(rightEmissiveObjects));
		}

		private void StopCurrentPatterns()
		{
			if (frontCoroutine != null)
			{
				StopCoroutine(frontCoroutine);
				frontCoroutine = null;
			}

			if (leftCoroutine != null)
			{
				StopCoroutine(leftCoroutine);
				leftCoroutine = null;
			}

			if (rightCoroutine != null)
			{
				StopCoroutine(rightCoroutine);
				rightCoroutine = null;
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

		// Check if object is found and log appropriate message
		void CheckObject(GameObject obj, string name)
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
        void NormalizeWorldScale(GameObject obj, Transform newParent)
        {
            if (obj == null || newParent == null) return;

            Vector3 parentWorldScale = newParent.lossyScale;
            Vector3 targetWorldScale = Vector3.one;

            // Use the correct initial world scale for each object
            if (obj == playerCamera)
                targetWorldScale = playerCameraInitialWorldScale;
            else if (obj == playerVRSetup)
                targetWorldScale = playerVRSetupInitialWorldScale;
            else
                return; // Do nothing for unknown objects

            obj.transform.localScale = new Vector3(
                targetWorldScale.x / parentWorldScale.x,
                targetWorldScale.y / parentWorldScale.y,
                targetWorldScale.z / parentWorldScale.z
            );
        }

        // Save original parent of object in dictionary
        void SaveOriginalParent(GameObject obj)
		{
			if (obj != null && !originalParents.ContainsKey(obj))
			{
				originalParents[obj] = obj.transform.parent;
			}
		}

		// Restore original parent of object and log appropriate message
		void RestoreOriginalParent(GameObject obj, string name)
		{
			if (obj != null && originalParents.ContainsKey(obj))
			{
				Transform originalParent = originalParents[obj];
				// Restore original parent
				obj.transform.SetParent(originalParent, false);
				logger.Debug($"{gameObject.name} {name} restored to original parent.");

				// Reapply offset AFTER reparenting
				obj.transform.localPosition = originalParent.localPosition;
				obj.transform.localRotation = originalParent.localRotation;
			}
		}
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
        // Initialize the emissive arrays with the appropriate objects
        private void InitializeEmissiveArrays()
		{
			// Find front emissive objects under "emissive" in the root
			frontEmissiveObjects = new Renderer[16];
			Transform emissiveObject = transform.Find("emissive");
			if (emissiveObject != null)
			{
				frontEmissiveObjects[0] = emissiveObject.Find("emissive1step1")?.GetComponent<Renderer>();
				frontEmissiveObjects[1] = emissiveObject.Find("emissive1step2")?.GetComponent<Renderer>();
				frontEmissiveObjects[2] = emissiveObject.Find("emissive1step3")?.GetComponent<Renderer>();
				frontEmissiveObjects[3] = emissiveObject.Find("emissive1step4")?.GetComponent<Renderer>();
				frontEmissiveObjects[4] = emissiveObject.Find("emissive2step1")?.GetComponent<Renderer>();
				frontEmissiveObjects[5] = emissiveObject.Find("emissive2step2")?.GetComponent<Renderer>();
				frontEmissiveObjects[6] = emissiveObject.Find("emissive2step3")?.GetComponent<Renderer>();
				frontEmissiveObjects[7] = emissiveObject.Find("emissive2step4")?.GetComponent<Renderer>();
				frontEmissiveObjects[8] = emissiveObject.Find("emissive3step1")?.GetComponent<Renderer>();
				frontEmissiveObjects[9] = emissiveObject.Find("emissive3step2")?.GetComponent<Renderer>();
				frontEmissiveObjects[10] = emissiveObject.Find("emissive3step3")?.GetComponent<Renderer>();
				frontEmissiveObjects[11] = emissiveObject.Find("emissive3step4")?.GetComponent<Renderer>();
				frontEmissiveObjects[12] = emissiveObject.Find("emissive4step1")?.GetComponent<Renderer>();
				frontEmissiveObjects[13] = emissiveObject.Find("emissive4step2")?.GetComponent<Renderer>();
				frontEmissiveObjects[14] = emissiveObject.Find("emissive4step3")?.GetComponent<Renderer>();
				frontEmissiveObjects[15] = emissiveObject.Find("emissive4step4")?.GetComponent<Renderer>();
			}

			// Initialize left and right arrays from thrustObject
			leftEmissiveObjects = new Renderer[15];
			rightEmissiveObjects = new Renderer[15];
			thrustObject = ZObject.Find("thrust");
			if (thrustObject != null)
			{
				// Left side
				leftEmissiveObjects[0] = thrustObject.Find("thrustL_1_1")?.GetComponent<Renderer>();
				leftEmissiveObjects[1] = thrustObject.Find("thrustL_1_2")?.GetComponent<Renderer>();
				leftEmissiveObjects[2] = thrustObject.Find("thrustL_1_3")?.GetComponent<Renderer>();
				leftEmissiveObjects[3] = thrustObject.Find("thrustL_2_1")?.GetComponent<Renderer>();
				leftEmissiveObjects[4] = thrustObject.Find("thrustL_2_2")?.GetComponent<Renderer>();
				leftEmissiveObjects[5] = thrustObject.Find("thrustL_2_3")?.GetComponent<Renderer>();
				leftEmissiveObjects[6] = thrustObject.Find("thrustL_3_1")?.GetComponent<Renderer>();
				leftEmissiveObjects[7] = thrustObject.Find("thrustL_3_2")?.GetComponent<Renderer>();
				leftEmissiveObjects[8] = thrustObject.Find("thrustL_3_3")?.GetComponent<Renderer>();
				leftEmissiveObjects[9] = thrustObject.Find("thrustL_4_1")?.GetComponent<Renderer>();
				leftEmissiveObjects[10] = thrustObject.Find("thrustL_4_2")?.GetComponent<Renderer>();
				leftEmissiveObjects[11] = thrustObject.Find("thrustL_4_3")?.GetComponent<Renderer>();
				leftEmissiveObjects[12] = thrustObject.Find("thrustL_5_1")?.GetComponent<Renderer>();
				leftEmissiveObjects[13] = thrustObject.Find("thrustL_5_2")?.GetComponent<Renderer>();
				leftEmissiveObjects[14] = thrustObject.Find("thrustL_5_3")?.GetComponent<Renderer>();

				// Right side
				rightEmissiveObjects[0] = thrustObject.Find("thrustR_1_1")?.GetComponent<Renderer>();
				rightEmissiveObjects[1] = thrustObject.Find("thrustR_1_2")?.GetComponent<Renderer>();
				rightEmissiveObjects[2] = thrustObject.Find("thrustR_1_3")?.GetComponent<Renderer>();
				rightEmissiveObjects[3] = thrustObject.Find("thrustR_2_1")?.GetComponent<Renderer>();
				rightEmissiveObjects[4] = thrustObject.Find("thrustR_2_2")?.GetComponent<Renderer>();
				rightEmissiveObjects[5] = thrustObject.Find("thrustR_2_3")?.GetComponent<Renderer>();
				rightEmissiveObjects[6] = thrustObject.Find("thrustR_3_1")?.GetComponent<Renderer>();
				rightEmissiveObjects[7] = thrustObject.Find("thrustR_3_2")?.GetComponent<Renderer>();
				rightEmissiveObjects[8] = thrustObject.Find("thrustR_3_3")?.GetComponent<Renderer>();
				rightEmissiveObjects[9] = thrustObject.Find("thrustR_4_1")?.GetComponent<Renderer>();
				rightEmissiveObjects[10] = thrustObject.Find("thrustR_4_2")?.GetComponent<Renderer>();
				rightEmissiveObjects[11] = thrustObject.Find("thrustR_4_3")?.GetComponent<Renderer>();
				rightEmissiveObjects[12] = thrustObject.Find("thrustR_5_1")?.GetComponent<Renderer>();
				rightEmissiveObjects[13] = thrustObject.Find("thrustR_5_2")?.GetComponent<Renderer>();
				rightEmissiveObjects[14] = thrustObject.Find("thrustR_5_3")?.GetComponent<Renderer>();
			}

			LogMissingObject(frontEmissiveObjects, "frontEmissiveObjects");
			LogMissingObject(leftEmissiveObjects, "leftEmissiveObjects");
			LogMissingObject(rightEmissiveObjects, "rightEmissiveObjects");
		}

		void InitializeLights()
		{
			// Gets all Light components in the target object and its children
			Light[] lights = transform.GetComponentsInChildren<Light>(true);

			foreach (Light light in lights)
			{
				switch (light.gameObject.name)
				{
					case "start_light":
                        start = light;
						logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
						break;
                    case "crash_light":
                        crash = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "danger_light":
                        danger = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "emergency_stop_light":
                        emergency_stop = light;
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
            // Find references to PlayerCamera and VR setup objects
            playerCamera = PlayerVRSetup.PlayerCamera.gameObject;

            // Find and assign the whole VR rig try SteamVR first, then Oculus
            playerVRSetup = GameObject.Find("Player/[SteamVRCameraRig]");
            // If not found, try to find the Oculus VR rig
            if (playerVRSetup == null)
            {
                playerVRSetup = GameObject.Find("OVRCameraRig");
            }

            // Check if objects are found
            CheckObject(playerCamera, "PlayerCamera");
            if (playerVRSetup != null)
            {
                CheckObject(playerVRSetup, playerVRSetup.name); // will print either [SteamVRCameraRig] or OVRCameraRig
            }
            else
            {
                logger.Debug($"{gameObject.name} No VR Devices found. No SteamVR or OVR present)");
            }
            // Find X object in hierarchy
            XObject = transform.Find("X");
			if (XObject != null)
			{
				logger.Debug($"{gameObject.name} X object found.");
				XStartPosition = XObject.localPosition;
				XStartRotation = XObject.localRotation;

				// Find Y object under X
				YObject = XObject.Find("Y");
				if (YObject != null)
				{
					logger.Debug($"{gameObject.name} Y object found.");
					YStartPosition = YObject.localPosition;
					YStartRotation = YObject.localRotation;

					// Find Z object under X
					ZObject = YObject.Find("Z");
					if (ZObject != null)
					{
						logger.Debug($"{gameObject.name} Z object found.");
						ZStartPosition = ZObject.localPosition;
						ZStartRotation = ZObject.localRotation;

						// Find Throttle under Z
						ThrottleObject = ZObject.Find("Throttle");
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
						StickObject = ZObject.Find("Stick");
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
                        // Find cockpit camera
                        cockpitCam = ZObject.Find("eyes/cockpitcam")?.gameObject;
                        if (cockpitCam != null)
                        {
                            logger.Debug($"{gameObject.name} Cockpitcam object found.");
                            cockpitCamStartPosition = cockpitCam.transform.localPosition;
                            cockpitCamStartRotation = cockpitCam.transform.localRotation;
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} cockpitCam object not found.");
                        }

                        // Find vr camera
                        vrCam = ZObject.Find("eyes/vrcam")?.gameObject;
                        if (vrCam != null)
                        {
                            logger.Debug($"{gameObject.name} vrCam object found.");
                            vrCamStartPosition = vrCam.transform.localPosition;
                            vrCamStartRotation = vrCam.transform.localRotation;
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} vrCam object not found.");
                        }
                        // Find StartObject object under Z
                        Start_lampObject = ZObject.Find("Start_lamp");
                        if (Start_lampObject != null)
                        {
                            logger.Debug($"{gameObject.name} Start_lamp object found.");
                            // Ensure the Start object is initially off
                            Renderer renderer = Start_lampObject.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Start_lamp object not found.");
                        }
                        // Find Danger_lamp object under Z
                        Danger_lampObject = ZObject.Find("Danger_lamp");
						if (Danger_lampObject != null)
						{
							logger.Debug($"{gameObject.name} object found.");
                            // Ensure the Danger_lamp object is initially off
                            Renderer renderer = Danger_lampObject.GetComponent<Renderer>();
							if (renderer != null)
							{
								renderer.material.DisableKeyword("_EMISSION");
							}
							else
							{
								logger.Debug($"{gameObject.name} Renderer component is not found on Danger_lamp object.");
							}
						}
						else
						{
							logger.Debug($"{gameObject.name} Danger_lamp object not found under Z.");
						}
                        // Find M_start_lamp object under Z
                        Start_lampObject = ZObject.Find("Start_lamp");
                        if (Start_lampObject != null)
                        {
                            logger.Debug($"{gameObject.name} object found.");
                            // Ensure the Start_lamp object is initially off
                            Renderer renderer = Start_lampObject.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.DisableKeyword("_EMISSION");
                            }
                            else
                            {
                                logger.Debug($"{gameObject.name} Renderer component is not found on Start_lamp object.");
                            }
                        }
                        else
                        {
                            logger.Debug($"{gameObject.name} Start_lamp object not found under Z.");
                        }
                    }
					else
					{
						logger.Debug($"{gameObject.name} Z object not found.");
					}
				}
				else
				{
					logger.Debug($"{gameObject.name} Y object not found.");
				}
			}
			else
			{
				logger.Debug($"{gameObject.name} X object not found.");
			}
            // Find M_emergency_stop_lamp
            Emergency_stop_lampObject = transform.Find("Emergency_stop_lamp");
            if (Emergency_stop_lampObject != null)
            {
                logger.Debug($"{gameObject.name} object found.");
                // Ensure the Emergency_stop_lamp object is initially off
                Renderer renderer = Emergency_stop_lampObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
                else
                {
                    logger.Debug($"{gameObject.name} Renderer component is not found on Emergency_stop_lamp object.");
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Emergency_stop_lamp object not found under Z.");
            }
            // Attempt to find cockpitCollider by name
            if (cockpitCollider == null)
			{
				Collider[] colliders = GetComponentsInChildren<Collider>(true); // true = include inactive
				foreach (var col in colliders)
				{
					if (col.gameObject.name == "Cockpit")
					{
						cockpitCollider = col;
						logger.Debug($"{gameObject.name} cockpitCollider found in children: {cockpitCollider.name}");
						break;
					}
				}
			}
		}
        public static class KeySimulator
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
    }
}