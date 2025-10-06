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

namespace WIGUx.Modules.metalhawkMotionSim
{
	public class metalhawkMotionSimController : MonoBehaviour
	{
		static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

		[Header("Object Settings")]
		private Transform StartObject; // Reference to the Start button object
		private Transform StickObject; // Reference to the Stick mirroring object
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
		private float StickXRotationDegrees = 20f; // Degrees for Stick rotation, adjust as needed
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
		[SerializeField] float minRotationX = -8f;
		[SerializeField] float maxRotationX = 8f;
		[SerializeField] float minRotationY = -0f;
		[SerializeField] float maxRotationY = 0f;
		[SerializeField] float minRotationZ = -8f;
		[SerializeField] float maxRotationZ = 8f;

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
		public Light fire1_light;
		public Light fire2_light;
		public float lightDuration = 0.35f; // Duration during which the lights will be on
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
		private bool inFocusMode = false;  // Flag to track focus mode state
		private bool isCenteringRotation = false; // Flag to track centering rotation state
		private bool isRiding = false; // Set riding state to true
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
			if (fire1_light) ToggleLight(fire1_light, false);
			if (fire2_light) ToggleLight(fire2_light, false);
			// if (startObject) ToggleEmissive(startObject.gameObject, false);
			StartAttractPattern();
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

		void StartFocusMode()
		{
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
			StopCurrentPatterns();
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
                // logger.Debug($"{gameObject.name} Containment check - bounds.Contains: {boundsContains}, ClosestPoint==pos: {inside}");
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
                    playerCamera.transform.SetParent(cockpitCam.transform, false);
                    playerCamera.transform.position = worldPos;
                    playerCamera.transform.rotation = worldRot;
                    NormalizeWorldScale(playerCamera, cockpitCam.transform);
                    StartCoroutine(AlignPlayerHeadToCamera(playerCamera, cockpitCam.transform));
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
                    playerVRSetup.transform.SetParent(vrCam.transform, false);
                    playerVRSetup.transform.position = worldPos;
                    playerVRSetup.transform.rotation = worldRot;
                    NormalizeWorldScale(playerVRSetup, vrCam.transform);
                    StartCoroutine(AlignPlayerHeadToCamera(playerVRSetup, vrCam.transform));
                    logger.Debug($"{gameObject.name} VR Player is aboard and strapped in!");
                    logger.Debug("Metal Hawk Motion Sim starting...");
					logger.Debug("Spread your wings and fly!...");
					isRiding = true; // Set riding state to true
				}
			}
			else
			{
				logger.Debug($"{gameObject.name} Player is not aboard the ride, Starting Without the Player aboard.");
			}
			inFocusMode = true;  // Set focus mode flag
		}

		void EndFocusMode()
		{
			logger.Debug($"{gameObject.name} Exiting Focus Mode...");
			RestoreOriginalParent(playerCamera, "PlayerCamera");
			RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");
            if (fire1_light) ToggleLight(fire1_light, false);
            if (fire2_light) ToggleLight(fire2_light, false);
            StartAttractPattern();
			ResetPositions();
			inFocusMode = false;  // Clear focus mode flag
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
			/*
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
            */
		}
		// Individual function for lamp0
		void ProcessLamp0(int state)
		{
			// Update lights
		}
		void ResetPositions()      // Reset objects and cockpit cam to initial position and rotation
		{
			logger.Debug($"{gameObject.name} Resetting Positions");
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

			// Reset rotation allowances and current rotation values
			currentRotationX = 0f;
			currentRotationY = 0f;
			currentRotationZ = 0f;
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
				float inputValue = -primaryThumbstick.x * thumbstickVelocity * Time.deltaTime;
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
		private void MapButtons(ref bool inputDetected)
		{
			//no buttons for this right now
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
		void CenterRotation()
		{
			isCenteringRotation = true;
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
        // === ALIGNMENT (VR + DESKTOP, unified, delayed) ===
        // moves vrcam or cockpitcam so the player head/camera
        // aligns with the "eyes" anchor height.
        IEnumerator AlignPlayerHeadToCamera(GameObject player, Transform cameraTransform)
        {
            if (player == null || cameraTransform == null)
            {
                //   logger.Debug($"{gameObject.name} [Align] skipped — player or cameraTransform null");
                yield break;
            }

            // "eyes" anchor is the parent of vrcam/cockpitcam, so use cameraTransform.parent
            Transform eyesAnchor = cameraTransform.parent;
            if (eyesAnchor == null)
            {
                //      logger.Debug($"{gameObject.name} [Align] no 'eyes' anchor found (cameraTransform has no parent).");
                yield break;
            }

            // locate VR or desktop head inside the current camera branch, not globally
            Transform head =
                cameraTransform.Find("[SteamVRCameraRig]/Camera (eye)/Head") ??
                cameraTransform.Find("OVRCameraRig/TrackingSpace/CenterEyeAnchor") ??
                cameraTransform.Find("Camera (Main)") ??
                cameraTransform.Find("Camera (eye)") ??
                cameraTransform.Find("Camera");

            // desktop fallback: use cached PlayerCamera if available
            if (head == null && playerCamera != null)
                head = playerCamera.transform;

            if (head == null)
            {
                //    logger.Debug($"{gameObject.name} [Align] ❌ could not find head or player camera under {cameraTransform.name}.");
                yield break;
            }

            // compute world offset between player's head and eyes anchor
            Vector3 offset = head.position - eyesAnchor.position;

            // move the camera anchor (vrcam or cockpitcam) so head/camera lines up with eyes
            cameraTransform.position -= offset;

            string mode;
            if (head.name.Contains("CenterEyeAnchor"))
                mode = "OVR";
            else if (head.name.Contains("PlayerCamera") || head.name.Contains("Camera (Main)"))
                mode = "Desktop";
            else
                mode = "SteamVR";

            // logger.Debug($"{gameObject.name} [Align] ✅ adjusted {cameraTransform.name} by {-offset.y:F3} for {mode}");
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
				obj.transform.SetParent(originalParents[obj]);
				logger.Debug($"{gameObject.name} {name} restored to original parent.");
			}
		}

		// Unset parent of object and log appropriate message
		void UnsetParentObject(GameObject obj, string name)
		{
			if (obj != null)
			{
				obj.transform.SetParent(null);
				logger.Debug($"{gameObject.name} {name} unset from parent.");
			}
		}
        IEnumerator attractPattern()  //Pattern For Attract Mode
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 1f));
            while (true)
            {
              //  if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, false);
                if (fire1_light) ToggleLight(fire1_light, false);
                if (fire2_light) ToggleLight(fire2_light, false);
                yield return new WaitForSeconds(attractFlashDuration);
            //    if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, true);
                if (fire1_light) ToggleLight(fire1_light, true);
                if (fire2_light) ToggleLight(fire2_light, true);
                yield return new WaitForSeconds(attractFlashDelay);
            }
        }

        IEnumerator dangerPattern() //Pattern For Focused Danger Mode
        {
            while (true)
            {
             //   if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, false);
                if (fire1_light) ToggleLight(fire1_light, false);
                if (fire2_light) ToggleLight(fire2_light, false);
                yield return new WaitForSeconds(dangerFlashDuration);
            //    if (BrakelightsObject) ToggleEmissive(BrakelightsObject.gameObject, true);
                if (fire1_light) ToggleLight(fire1_light, true);
                if (fire2_light) ToggleLight(fire2_light, true);
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
					case "firelight1":
						fire1_light = light;
						logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
						break;
					case "firelight2":
						fire2_light = light;
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

					// Find Z object under Y
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
						// Find cockpit camera under cockpit
						cockpitCam = ZObject.Find("eyes/cockpitcam")?.gameObject;
						if (cockpitCam != null)
						{
							logger.Debug($"{gameObject.name} Cockpitcam object found.");

							// Store initial position and rotation of cockpit cam
							cockpitCamStartPosition = cockpitCam.transform.localPosition;
							cockpitCamStartRotation = cockpitCam.transform.localRotation;
						}
						else
						{
							logger.Error("Cockpitcam object not found under Z!");
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
    }
}