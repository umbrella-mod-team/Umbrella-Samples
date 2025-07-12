using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using System.Xml.Linq;


namespace WIGUx.Modules.starbladeSim
{
    public class starbladeSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Object Settings")]
        private Transform TurretObject; // Reference to the left stick mirroring object

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
        private readonly float rotationSmoothness = 5f;  //sets the smoothness of the rotation
        private readonly float mouseSensitivityX = 350.5f;  // Velocity for mouse input
        private readonly float mouseSensitivityY = 350.5f;  // Velocity for mouse input
        private readonly float mouseSensitivityZ = 350.5f;  // Velocity for mouse input
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
        private Vector3 TurretStartPosition; // Initial Throttle positions for resetting

        [Header("Rotation Settings")]     // Initial rotations setup
        private Quaternion TurretStartRotation;  // Initial Throttle rotation for resetting

        [Header("Lights and Emissives")]     // Setup Emissive and Lights
        private Light firelight_light;
        public Light starblade1_light;
        public Light starblade2_light;
        private Transform FireEmissiveObject;
        private Transform LightLeftObject;
        private Transform LightRightObject;
        private float flashDuration = 0.15f;
        private float flashInterval = 0.15f;
        private float lightDuration = 0.5f; // Duration during which the lights will be on
        private bool areLighsOn = false; // track strobe lights
        private Coroutine Coroutine; // Coroutine variable to control the strobe flashing
        private Light[] lights;
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
            // Find Throttle object in hierarchy
            TurretObject = transform.Find("Turret");
            if (TurretObject != null)
            {
                logger.Debug($"{gameObject.name} Turret object found.");
                TurretStartPosition = TurretObject.localPosition;
                TurretStartRotation = TurretObject.localRotation;

                LightLeftObject = transform.Find("lights/LightLeft");
                if (LightLeftObject != null)
                {
                    logger.Debug("LightLeft object found.");
                    // Ensure the light_left object is initially off
                    Renderer renderer = LightLeftObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                    else
                    {
                        logger.Debug("Renderer component is not found on LightLeft object.");
                    }
                }
                LightRightObject = transform.Find("lights/LightRight");
                if (LightRightObject != null)
                {
                    logger.Debug("LightRight object found.");
                    // Ensure the light_right object is initially off
                    Renderer renderer = LightRightObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                    else
                    {
                        logger.Debug("Renderer component is not found on LightRight object.");
                    }
                }

            else
            {
                logger.Debug($"{gameObject.name} Turret object not found.");
            }

                // Gets all Light components in the target object and its children
                Light[] Lights = transform.GetComponentsInChildren<Light>();

            // Log the names of the objects containing the Light components and filter out unwanted lights
            foreach (Light light in Lights)
            {
                logger.Debug($"Light found: {light.gameObject.name}");
                switch (light.gameObject.name)
                {
                    case "starblade1":
                        starblade1_light = light;
                        Lights[0] = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "starblade2":
                        starblade2_light = light;
                        Lights[1] = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    case "firelight":
                        firelight_light = light;
                        Lights[0] = light;
                        logger.Debug($"{gameObject.name} Included Light found in object: " + light.gameObject.name);
                        break;
                    default:
                        logger.Debug($"{gameObject.name} Excluded Light found in object: " + light.gameObject.name);
                        break;
                }
            }
                // Find Turret object in hierarchy
                TurretObject = transform.Find("Turret");
            if (TurretObject != null)
            {
                logger.Debug($"{gameObject.name} Turret object found.");
                TurretStartPosition = TurretObject.localPosition;
                TurretStartRotation = TurretObject.localRotation;

                // Find the _firelight within ControllerZ
                // Find fireemissive object
                FireEmissiveObject = TurretObject.Find("FireEmissive");
                if (FireEmissiveObject != null)
                {
                    logger.Debug("fireemissive object found.");
                    // Ensure the fireemissive object is initially off
                    Renderer renderer = FireEmissiveObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                    else
                    {
                        logger.Debug("Renderer component is not found on FireEmissive object.");
                    }
                }
                else
                {
                    logger.Debug("FireEmissive object not found under aburnerX.");
                }
                ToggleFireLight(false);
                ToggleFireEmissive(false);
                ToggleLight1(false);
                ToggleLight2(false);

            }

            void Update()
        {

            bool inputDetected = false;  // Initialize at the beginning of the Update method

            if (GameSystem.ControlledSystem != null && !inFocusMode)
            {
                string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
                bool containsString = false;

                foreach (var gameString in compatibleGames)
                {
                    if (controlledSystemGamePathString != null && controlledSystemGamePathString.Contains(gameString))
                    {
                        containsString = true;
                        break;
                    }
                }

                if (containsString)
                {
                    StartFocusMode();
                }
            }

            if (GameSystem.ControlledSystem == null && inFocusMode)
            {
                EndFocusMode();
            }

            if (inFocusMode)
            {
                HandleInput(ref inputDetected);  // Pass by reference
                HandleMouseRotation(ref inputDetected);
            }
        }

        private void HandleXInput()
        {
            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

            if (XInput.IsConnected)
            {
                primaryThumbstick = XInput.Get(XInput.Axis.LThumbstick);
                secondaryThumbstick = XInput.Get(XInput.Axis.RThumbstick);

                Vector2 xboxPrimaryThumbstick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                Vector2 xboxSecondaryThumbstick = new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));

                // Combine VR and Xbox inputs
                primaryThumbstick += xboxPrimaryThumbstick;
                secondaryThumbstick += xboxSecondaryThumbstick;
            }
        }

        public static class VRControllerHelper
    {
        public static SteamVR_Controller.Device GetController(HandType handType)
        {
            int index = -1;

            // Get the device index based on the hand type
            switch (handType)
            {
                case HandType.Left:
                    index = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost);
                    break;
                case HandType.Right:
                    index = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost);
                    break;
            }

            // Return the controller device if a valid index was found
            if (index != -1)
            {
                return SteamVR_Controller.Input(index);
            }
            return null;
        }
        }


        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Debug($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Debug("Compatible Rom Dectected, Prepaing  for launch... ");
            logger.Debug("Star Blade Module starting...");
            logger.Debug("It's been an honor!...");


            Coroutine = StartCoroutine(FlashLights());
            isFlashing = true;
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            StopCoroutine(Coroutine);
            if (firelight1) ToggleLight(firelight1, false);
            if (firelight2) ToggleLight(firelight2, false);
            if (StartObject) ToggleEmissive(StartObject.gameObject, false);
            if (Hazard) ToggleEmissive(Hazard.gameObject, false);
            if (Fire2Object) ToggleEmissive(Fire2Object.gameObject, false);
            Coroutine = null;
            isFlashing = false;

            inFocusMode = false;  // Clear focus mode flag
        }
        void HandleMouseRotation(ref bool inputDetected)
        {
            // Get mouse input for Y and X axes
            float mouseRotateZ = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;
            float mouseRotateY = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;

            // Check current rotation and new proposed rotation for Z axis
            float newRotationZ = CurrentControllerRotationZ - mouseRotateZ;
            if (newRotationZ >= -ControllerrotationLimitZ && newRotationZ <= ControllerrotationLimitZ)
            {
                ControllerZ.Rotate(0, 0, mouseRotateZ);
                CurrentControllerRotationZ = newRotationZ; // Update current rotation
                inputDetected = true; 
 isCenteringRotation = false;
            }

            // Check current rotation and new proposed rotation for Y axis
            float newRotationY = CurrentControllerRotationY + mouseRotateY;
            if (newRotationY >= -ControllerrotationLimitY && newRotationY <= ControllerrotationLimitY)
            {
                ControllerY.Rotate(0, -mouseRotateY, 0);
                CurrentControllerRotationY = newRotationY; // Update current rotation
                inputDetected = true; 
 isCenteringRotation = false;
            }
        }

        //sexy new combined input handler
        void HandleInput(ref bool inputDetected)
        {
            if (!inFocusMode) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;
            // Fire2
            if (Input.GetButtonDown("Fire1"))
            {
                ToggleFireEmissive(true);
                ToggleFireLight(true);
                inputDetected = true; 
 isCenteringRotation = false;
            }

            // Reset position on button release
            if (Input.GetButtonUp("Fire1"))
            {
                ToggleFireEmissive(false);
                ToggleFireLight(false);
                inputDetected = true; 
 isCenteringRotation = false;
            }
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

        void ToggleFireEmissive(bool isActive)
        {
            if (fireemissiveObject != null)
            {
                Renderer renderer = fireemissiveObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (isActive)
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                    //     logger.Debug($"fireemissive object emission turned {(isActive ? "on" : "off")}.");
                }
                else
                {
                    logger.Debug("Renderer component is not found on fireemissive object.");
                }
            }
            else
            {
                logger.Debug("fireemissive object is not assigned.");
            }
        }
        void ToggleleftlightEmissive(bool isActive)
        {
            if (light_leftObject != null)
            {
                Renderer renderer = light_leftObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (isActive)
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                    //     logger.Debug($"_light_leftObject emission turned {(isActive ? "on" : "off")}.");
                }
                else
                {
                    logger.Debug("Renderer component is not found on light_leftObject.");
                }
            }
            else
            {
                logger.Debug("light_leftObject is not assigned.");
            }
        }
        void TogglerightlightEmissive(bool isActive)
        {
            if (light_rightObject != null)
            {
                Renderer renderer = light_rightObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (isActive)
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                    //     logger.Debug($"_light_rightObject emission turned {(isActive ? "on" : "off")}.");
                }
                else
                {
                    logger.Debug("Renderer component is not found on light_rightObject.");
                }
            }
            else
            {
                logger.Debug("light_rightObject is not assigned.");
            }
        }


        void ToggleFireLight(bool isActive)
        {
            // Toggle the light directly if the component is valid
            if (firelight != null)
            {
                firelight.enabled = isActive;
            }
            else
            {
                Debug.LogWarning("Attempted to toggle a null Light component.");
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

        IEnumerator FlashLights()
        {
            int currentIndex = 0; // Start with the first light in the array

            while (true)
            {
                // Select the current light
                Light light = Lights[currentIndex];

                // Check if the light is not null
                if (light != null)
                {
                    // Log the chosen light
                    // logger.Debug($"Flashing {light.name}");

                    // Turn on the chosen light
                    if (starbladelight) ToggleLight(light, true);

                    // Toggle the corresponding emissive
                    if (currentIndex == 0) // Light 1 (paired with left emissive)
                    {
                        ToggleleftlightEmissive(true);
                        TogglerightlightEmissive(false);
                    }
                    else if (currentIndex == 1) // Light 2 (paired with right emissive)
                    {
                        ToggleleftlightEmissive(false);
                        TogglerightlightEmissive(true);
                    }

                    // Wait for the flash duration
                    yield return new WaitForSeconds(flashDuration);

                    // Turn off the chosen light
                    if (light) ToggleLight(light, false);

                    // Turn off both emissives
                    ToggleleftlightEmissive(false);
                    TogglerightlightEmissive(false);

                    // Wait for the next flash interval
                    yield return new WaitForSeconds(flashInterval - flashDuration);
                }
                else
                {
                    logger.Debug("Light is null.");
                }

                // Move to the next light in the array
                currentIndex = (currentIndex + 1) % Lights.Length;
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

    }
}

