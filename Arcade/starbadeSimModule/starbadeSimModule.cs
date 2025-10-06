using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;
using System.IO;
using System.Xml.Linq;


namespace WIGUx.Modules.starbladeSim
{
    public class starbladeSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        [Header("Lights and Emissives")]     // Setup Emissive and Lights
        private Transform GunObject;
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
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects
        private bool lastTriggerState = false; // track last fire state

        void Start()
        {
            gameSystem = GetComponent<GameSystem>();
            GunObject = transform.Find("Gun");
            if (GunObject != null)
            {
                logger.Debug($"{gameObject.name} Gun object found.");

                LightLeftObject = transform.Find("lights/LightLeft");
                if (LightLeftObject != null)
                {
                    Renderer renderer = LightLeftObject.GetComponent<Renderer>();
                    if (renderer != null) renderer.material.DisableKeyword("_EMISSION");
                    logger.Debug("LightLeft object initialized.");
                }

                LightRightObject = transform.Find("lights/LightRight");
                if (LightRightObject != null)
                {
                    Renderer renderer = LightRightObject.GetComponent<Renderer>();
                    if (renderer != null) renderer.material.DisableKeyword("_EMISSION");
                    logger.Debug("LightRight object initialized.");
                }

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

                FireEmissiveObject = GunObject.Find("FireEmissive");
                if (FireEmissiveObject != null)
                {
                    Renderer renderer = FireEmissiveObject.GetComponent<Renderer>();
                    if (renderer != null) renderer.material.DisableKeyword("_EMISSION");
                    logger.Debug("FireEmissive initialized.");
                }

                // turn off all lights/emissives initially
                if (firelight_light) ToggleLight(firelight_light, false);
                if (starblade1_light) ToggleLight(starblade1_light, false);
                if (starblade2_light) ToggleLight(starblade2_light, false);
                if (FireEmissiveObject) ToggleEmissive(FireEmissiveObject.gameObject, false);
                if (LightLeftObject) ToggleEmissive(LightLeftObject.gameObject, false);
                if (LightRightObject) ToggleEmissive(LightRightObject.gameObject, false);
            }
        }

        private const float THUMBSTICK_DEADZONE = 0.13f; // Adjust as needed

        void Update()
        {
            CheckInsertedGameName();
            CheckControlledGameName();

            if (!string.IsNullOrEmpty(insertedGameName) && !string.IsNullOrEmpty(controlledGameName) && insertedGameName == controlledGameName && !inFocusMode)
                StartFocusMode();

            if (GameSystem.ControlledSystem == null && inFocusMode)
                EndFocusMode();

            if (inFocusMode)
            {
                bool triggerDown = IsTriggerPressed();
                HandleFireInput(triggerDown);
            }
        }

        private bool IsTriggerPressed()
        {
            if (Input.GetMouseButton(0)) return true;

            if (XInput.IsConnected)
            {
                float r = XInput.Get(XInput.Trigger.RIndexTrigger);
                float l = XInput.Get(XInput.Trigger.LIndexTrigger);
                if (r > 0.3f || l > 0.3f) return true;
            }

            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                float l = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
                float r = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
                if (l > 0.3f || r > 0.3f) return true;
            }

            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                float l = SteamVRInput.GetTriggerValue(HandType.Left);
                float r = SteamVRInput.GetTriggerValue(HandType.Right);
                if (l > 0.3f || r > 0.3f) return true;
            }

            return false;
        }

        private void HandleFireInput(bool triggerDown)
        {
            if (triggerDown && !lastTriggerState)
            {
                if (firelight_light) ToggleLight(firelight_light, true);
                if (starblade1_light) ToggleLight(starblade1_light, true);
                if (starblade2_light) ToggleLight(starblade2_light, true);
                if (FireEmissiveObject) ToggleEmissive(FireEmissiveObject.gameObject, true);
                lastTriggerState = true;
                logger.Debug("[StarbladeSim] Trigger pressed → Fire ON");
            }
            else if (!triggerDown && lastTriggerState)
            {
                if (firelight_light) ToggleLight(firelight_light, false);
                if (starblade1_light) ToggleLight(starblade1_light, false);
                if (starblade2_light) ToggleLight(starblade2_light, false);
                if (FireEmissiveObject) ToggleEmissive(FireEmissiveObject.gameObject, false);
                lastTriggerState = false;
                logger.Debug("[StarbladeSim] Trigger released → Fire OFF");
            }
        }

        void StartFocusMode()
        {
            Coroutine = StartCoroutine(FlashLights());
            isFlashing = true;
            inFocusMode = true;
            logger.Debug("Star Blade FocusMode ON.");
        }

        void EndFocusMode()
        {
            StopCoroutine(Coroutine);
            if (firelight_light) ToggleLight(firelight_light, false);
            if (starblade1_light) ToggleLight(starblade1_light, false);
            if (starblade2_light) ToggleLight(starblade2_light, false);
            if (FireEmissiveObject) ToggleEmissive(FireEmissiveObject.gameObject, false);
            if (LightLeftObject) ToggleEmissive(LightLeftObject.gameObject, false);
            if (LightRightObject) ToggleEmissive(LightRightObject.gameObject, false);
            Coroutine = null;
            isFlashing = false;
            inFocusMode = false;
        }
        // === FLASH LIGHTS DURING FOCUS MODE ===
        IEnumerator FlashLights()
        {
            logger.Debug("[StarbladeSim] FlashLights coroutine started.");

            while (inFocusMode)
            {
                // turn ON lights
                if (starblade1_light) ToggleLight(starblade1_light, true);
                if (starblade2_light) ToggleLight(starblade2_light, true);

                yield return new WaitForSeconds(flashDuration);

                // turn OFF lights
                if (starblade1_light) ToggleLight(starblade1_light, false);
                if (starblade2_light) ToggleLight(starblade2_light, false);

                yield return new WaitForSeconds(flashInterval);
            }

            // ensure lights end OFF when focus mode stops
            if (starblade1_light) ToggleLight(starblade1_light, false);
            if (starblade2_light) ToggleLight(starblade2_light, false);

            logger.Debug("[StarbladeSim] FlashLights coroutine ended.");
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
            if (GameSystem.ControlledSystem != null && GameSystem.ControlledSystem.Game != null && !string.IsNullOrEmpty(GameSystem.ControlledSystem.Game.path))
                controlledGameName = FileNameHelper.GetFileName(GameSystem.ControlledSystem.Game.path);
            else
                controlledGameName = string.Empty;
        }

        void ToggleEmissive(GameObject targetObject, bool isActive)
        {
            if (targetObject == null) return;
            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer == null) return;
            if (isActive) renderer.material.EnableKeyword("_EMISSION");
            else renderer.material.DisableKeyword("_EMISSION");
        }

        void ToggleLight(Light targetLight, bool isActive)
        {
            if (targetLight == null) return;
            if (targetLight.gameObject.activeSelf != isActive)
                targetLight.gameObject.SetActive(isActive);
            targetLight.enabled = isActive;
        }

        public static class FileNameHelper
        {
            public static string GetFileName(string filePath)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string FileName = System.Text.RegularExpressions.Regex.Replace(fileName, "[\\/:*?\"<>|]", "_");
                return FileName;
            }
        }
    }
}
