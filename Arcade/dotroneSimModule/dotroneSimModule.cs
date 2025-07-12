using System.IO;
using UnityEngine;
using WIGU;
using System.Collections;
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.GlobalIllumination;

namespace WIGUx.Modules.dotroneSimController
{
    public class dotroneController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        // Emissive object variables
        private Transform bezelObject;
        private Transform shroudLeftObject;
        private Transform shroudRightObject;
        private Transform backlightObject;
        private Transform frontlightObject;
        private Transform sticktriggerObject;
        private Transform StickObject;
        private Transform floorObject;
        private Transform joystickObject;
        private Transform marqueeObject;
        private Transform topLightObject;
        private Transform topLight2Object;
        private Transform frontObject;
        private Transform leftReflectObject;
        private Transform rightReflectObject;
        private Transform overlayObject;
        private Transform cabinetObject;
        private Transform backgroundObject;
        private Renderer backgroundRenderer;  // Reference to the background object renderer
        private float originalAlpha;         // Store the original alpha value
        private Material backgroundMaterial;  // Material of the background object
        private Color originalColor;          // Store the original color of the material (including alpha)

        // Light object variables
        private Light strobeLight;
        private Light joyLight;  // Joystick light component
        private Light bottomLight; // 1 bottom light component
        private Light[] rightLights = new Light[5];  // 5 top lights for the right side (top light 1-5)
        private Light[] leftLights = new Light[5];   // 5 top lights for the left side (top light 6-10)
        private float primaryThumbstickRotationMultiplier = 10f; // Multiplier for primary thumbstick rotation intensity
        private Coroutine currentCoroutine;        // Coroutine references for pattern control
        private Coroutine bgCR;       // background lamp
        private Coroutine seqCR;      // pattern sequencer
        private Coroutine footCR;     // foot‑lights
        private bool lastPowerState = false; // default: off
        private GameSystemState systemState;

        private bool inFocusMode = false;  // Flag to track focus mode state
        private Quaternion StickStartRotation;  // Initial Stick rotation
        private Vector3 StickStartPosition; // Initial Stick positions for resetting
        // Timing control (4.8 Hz and 9.4118 Hz delays for forward and reverse)
        private float delayLow = 1f / 4.8f;               // 4.8 Hz -> 77.616 ms
        private float delayHigh = 1f / 9.4118f;           // 9.4118 Hz -> 106.25 ms
        private bool isFlasherEnabled = false;

        [Header("Rom Check")]
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        // private string filePath;
        private string filePath = "./Emulators/MAME/outputs/dotronen.txt";
        private string configPath;
        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects
        private string lastData;  // Store the last data read
        private string newData;
        void Start()
        {
            CheckInsertedGameName();
            CheckControlledGameName();
            configPath = $"./Emulators/MAME/inputs/{insertedGameName}.ini";
            gameSystem = GetComponent<GameSystem>();
            WriteLampConfig(filePath);
            InitializeLights();
            InitializeObjects();
            DisableAllEmissives();
            TurnOffAllLights();
        }

        void Update()
        {
            systemState = GetComponent<GameSystemState>();  //check the power state
            ReadData();
            CheckInsertedGameName();
            CheckControlledGameName();
            bool isCurrentlyOn = systemState.IsOn;  //is it on?
            //Turn on the jazz when powered on
            if (isCurrentlyOn && !lastPowerState)
            {
                TurnOnAllLights();
                EnableAllEmissives();
                strobeLight.enabled = false;
            }
            //Shut down the fun bus
            else if (!isCurrentlyOn && lastPowerState)
            {
                DisableAllEmissives();
                TurnOffAllLights();
                strobeLight.enabled = false;
            }
            // Always update the lastPowerState at the end
            lastPowerState = isCurrentlyOn;

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
            }
        }
        void WriteLampConfig(string filePath)
        {
            string content = "00\n";

            try
            {
                File.WriteAllText(filePath, content);
                logger.Debug("File written to: " + filePath);
            }
            catch (IOException e)
            {
                logger.Debug("File write failed: " + e.Message);
            }
        }

        void StartFocusMode()
        {
            logger.Debug($"{gameObject.name} Starting Focus Mode...");
           // StopCurrentPatterns();
            logger.Debug($"{gameObject.name} Greetings Programs...");
            inFocusMode = true;  // Set focus mode flag
        }
        void EndFocusMode()
        {
            logger.Debug($"{gameObject.name} Exiting Focus Mode...");
            if (StickObject != null)
            {
                StickObject.localPosition = StickStartPosition;
                StickObject.localRotation = StickStartRotation;
            }
            inFocusMode = false;  // Clear focus mode flag
        }

        private void MapThumbsticks()
        {
            if (!inFocusMode) return;
            if (StickObject == null) return;

            Vector2 primaryThumbstick = Vector2.zero;
            Vector2 secondaryThumbstick = Vector2.zero;

            // VR controller input
            if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
            {
                primaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                secondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                Vector2 ovrPrimaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                Vector2 ovrSecondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                float ovrPrimaryIndexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
                float ovrSecondaryIndexTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
                float ovrPrimaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
                float ovrSecondaryHandTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);
            }
            else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
            {
                var leftController = SteamVRInput.GetController(HandType.Left);
                var rightController = SteamVRInput.GetController(HandType.Right);
                primaryThumbstick = leftController.GetAxis();
                secondaryThumbstick = rightController.GetAxis();
            }
            // Ximput controller input
            if (XInput.IsConnected)
            {
                primaryThumbstick = XInput.Get(XInput.Axis.LThumbstick);
                secondaryThumbstick = XInput.Get(XInput.Axis.RThumbstick);
                Vector2 xboxPrimaryThumbstick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                Vector2 xboxSecondaryThumbstick = new Vector2(Input.GetAxis("RightStickHorizontal"), Input.GetAxis("RightStickVertical"));
                float LIndexTrigger = XInput.Get(XInput.Trigger.LIndexTrigger);
                float RIndexTrigger = XInput.Get(XInput.Trigger.RIndexTrigger);
                // Combine VR and Xbox inputs
                primaryThumbstick += xboxPrimaryThumbstick;
                secondaryThumbstick += xboxSecondaryThumbstick;
            }
            // Map primary thumbstick to Stick
            Quaternion primaryRotation = Quaternion.Euler(-primaryThumbstick.x * primaryThumbstickRotationMultiplier, 0, -primaryThumbstick.y * primaryThumbstickRotationMultiplier);
            StickObject.localRotation = primaryRotation;
        }

        void ReadData()
        {
            if (!File.Exists(filePath))
                return;

            // try up to 3 times to open it, in case MAME is writing
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using (var fs = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite))       // allow MAME to keep writing
                    using (var reader = new StreamReader(fs))
                    {
                        newData = reader.ReadToEnd().Trim();
                    }
                    break; // success
                }
                catch (IOException)
                {
                    // wait a few ms and retry
                    System.Threading.Thread.Sleep(5);
                }
            }

            if (string.IsNullOrEmpty(newData))
                return;

            // normalize to exactly two hex digits
            newData = newData.PadLeft(2, '0')
                           .ToUpperInvariant()
                           .Substring(newData.Length - 2, 2);

            if (newData == lastData)
                return;

            HandleDataChange(newData);
            lastData = newData;
        }


        // called from your ReadData() whenever rawHex changes ("03", "FF", etc)
        void HandleDataChange(string newData)
        {
            // --- stop any running routines ---
            logger.Debug($"Pattern = {newData}");

            // --- STOP & RESET BACKGROUND COROUTINE ---
            if (bgCR != null)
            {
                StopCoroutine(bgCR);
                bgCR = null;
                // reset the flashing/strobe and alpha
                strobeLight.enabled = false;
                backgroundMaterial.color = originalColor;
            }

            // --- STOP & RESET SEQUENCER COROUTINE (top‑lights) ---
            if (seqCR != null)
            {
                StopCoroutine(seqCR);
                seqCR = null;
                // turn off ALL top‑light pairs + strobe
                foreach (var l in rightLights) l.enabled = false;
                foreach (var l in leftLights) l.enabled = false;
                joyLight.enabled = false;
                bottomLight.enabled = false;
                // also clear any emissive flags you set in StrobeLightSequence
                SetEmissive(bezelObject, false);
                SetEmissive(backlightObject, false);
                // … do this for each emissive object …
            }

            // --- STOP & RESET FOOT‑LIGHT COROUTINE ---
            if (footCR != null)
            {
                StopCoroutine(footCR);
                footCR = null;
                bottomLight.enabled = false;
            }


            // --- dispatch on the two‑digit hex value ---
            switch (newData)
            {
                case "00":
                    // transparent background + foot strobes
                    bgCR = StartCoroutine(RestoreBackground());
                    break;

                case "01":
                    // solid background only
                    bgCR = StartCoroutine(SolidBackground());
                    break;

                case "02":
                    // background strobe only
                    bgCR = StartCoroutine(BackgroundFlashingEffect());
                    break;

                case "03":
                    // forward fast sequence
                    seqCR = StartCoroutine(ForwardFastPattern());
                    break;

                case "04":
                    // forward slow sequence
                    seqCR = StartCoroutine(ForwardSlowPattern());
                    break;

                case "05":
                    // reverse fast sequence
                    seqCR = StartCoroutine(ReverseFastPattern());
                    break;

                case "06":
                    // reverse slow sequence
                    seqCR = StartCoroutine(ReverseSlowPattern());
                    break;

                case "07":
                    // cabinet‑wide strobe
                    seqCR = StartCoroutine(StrobeLightSequence());
                    break;

                case "08":
                    // foot lights only
                    footCR = StartCoroutine(FootLightSequence());
                    break;

                // alias patterns:
                case "FB":
                    seqCR = StartCoroutine(ReverseFastPattern());
                    break;
                case "FC":
                    seqCR = StartCoroutine(ForwardFastPattern());
                    break;

                // combined effects:
                case "FD":
                    seqCR = StartCoroutine(StrobeLightSequence());
                    bgCR = StartCoroutine(BackgroundFlashingEffect());
                    break;
                case "FF":
                    seqCR = StartCoroutine(StrobeLightSequence());
                    bgCR = StartCoroutine(BackgroundFlashingEffect());
                    footCR = StartCoroutine(FootLightSequence());
                    break;

                default:
                    // unrecognized codes still get the foot strobes
                    footCR = StartCoroutine(FootLightSequence());
                    break;
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
        IEnumerator SolidBackground()
        {
            logger.Debug("Solid Background.");
            // ramp the background up to 80% opacity
            Color c = originalColor;
            c.a = 0.8f;
            backgroundMaterial.color = c;
            SetEmissive(backlightObject, true);
            // ensure your strobe light isn’t running
            strobeLight.enabled = false;

            // we don’t need to loop or wait here, so just exit
            yield break;
        }

        IEnumerator RestoreBackground() 
        {

            logger.Debug("Transparent Background.");
            Color c = originalColor;
            c.a = 0.8f;                   // or whatever “transparent” level you want
            backgroundMaterial.color = c;
            SetEmissive(backlightObject, true);
            strobeLight.enabled = false;  // make sure strobe’s off
            yield break;
        }

        // Forward Slow Pattern (4.8 Hz)
        IEnumerator ForwardSlowPattern()
        {
            while (true)  // Looping for continuous pattern
            {
                logger.Debug("Starting Forward Slow Pattern.");

                // Flash pairs of lights together
                // Turn on Pair 1 (Right 1-6) & Pair 4 (Right 4-9)
                if (rightLights[0] != null && leftLights[0] != null && rightLights[3] != null && leftLights[3] != null)
                {
                    rightLights[2].enabled = false;
                    leftLights[2].enabled = false;
                    rightLights[0].enabled = true;
                    leftLights[0].enabled = true;
                    rightLights[3].enabled = true;
                    leftLights[3].enabled = true;
                    logger.Debug("Pair 1 (1-6) and Pair 4 (4-9) flashing together");
                }

                // Wait for high delay (slow pattern duration)
                yield return new WaitForSeconds(delayHigh);

                // Turn on Pair 2 (Right 2-7) & Pair 5 (Right 5-10)
                if (rightLights[1] != null && leftLights[1] != null && rightLights[4] != null && leftLights[4] != null)
                {
                    rightLights[0].enabled = false;
                    leftLights[0].enabled = false;
                    rightLights[3].enabled = false;
                    leftLights[3].enabled = false;
                    rightLights[1].enabled = true;
                    leftLights[1].enabled = true;
                    rightLights[4].enabled = true;
                    leftLights[4].enabled = true;
                    logger.Debug("Pair 2 (2-7) and Pair 5 (5-10) flashing together");
                }

                // Wait for high delay (slow pattern duration)
                yield return new WaitForSeconds(delayHigh);

                // Turn on Pair 3 (Right 3-8)
                if (rightLights[2] != null && leftLights[2] != null)
                {
                    rightLights[1].enabled = false;
                    leftLights[1].enabled = false;
                    rightLights[4].enabled = false;
                    leftLights[4].enabled = false;
                    rightLights[2].enabled = true;
                    leftLights[2].enabled = true;
                    logger.Debug("Pair 3 (3-8) flashing together");
                }
                // Wait for high delay (slow pattern duration)
                yield return new WaitForSeconds(delayHigh);
            }
        }

        // Forward Fast Pattern (9.4118 Hz)
        IEnumerator ForwardFastPattern()
        {
            while (true)  // Looping for continuous pattern
            {
                logger.Debug("Starting Forward Slow Pattern.");

                // Flash pairs of lights together
                // Turn on Pair 1 (Right 1-6) & Pair 4 (Right 4-9)
                if (rightLights[0] != null && leftLights[0] != null && rightLights[3] != null && leftLights[3] != null)
                {
                    rightLights[2].enabled = false;
                    leftLights[2].enabled = false;
                    rightLights[0].enabled = true;
                    leftLights[0].enabled = true;
                    rightLights[3].enabled = true;
                    leftLights[3].enabled = true;
                    logger.Debug("Pair 1 (1-6) and Pair 4 (4-9) flashing together");
                }

                // Wait for high delay (slow pattern duration)
                yield return new WaitForSeconds(delayLow);

                // Turn on Pair 2 (Right 2-7) & Pair 5 (Right 5-10)
                if (rightLights[1] != null && leftLights[1] != null && rightLights[4] != null && leftLights[4] != null)
                {
                    rightLights[0].enabled = false;
                    leftLights[0].enabled = false;
                    rightLights[3].enabled = false;
                    leftLights[3].enabled = false;
                    rightLights[1].enabled = true;
                    leftLights[1].enabled = true;
                    rightLights[4].enabled = true;
                    leftLights[4].enabled = true;
                    logger.Debug("Pair 2 (2-7) and Pair 5 (5-10) flashing together");
                }

                // Wait for high delay (slow pattern duration)
                yield return new WaitForSeconds(delayLow);

                // Turn on Pair 3 (Right 3-8)
                if (rightLights[2] != null && leftLights[2] != null)
                {
                    rightLights[1].enabled = false;
                    leftLights[1].enabled = false;
                    rightLights[4].enabled = false;
                    leftLights[4].enabled = false;
                    rightLights[2].enabled = true;
                    leftLights[2].enabled = true;
                    logger.Debug("Pair 3 (3-8) flashing together");
                }
                // Wait before restarting the loop
                yield return new WaitForSeconds(delayLow);
            }
        }
        // Reverse Slow Pattern (4.8 Hz)
        IEnumerator ReverseSlowPattern()
        {
            while (true)  // Looping for continuous pattern
            {
                logger.Debug("Starting Reverse Slow Pattern.");

                // Flash pairs of lights in reverse order Pair 2 (2-7) and Pair 5 (5-10) flashing together
                if (rightLights[4] != null && leftLights[4] != null && rightLights[1] != null && leftLights[1] != null)
                {
                    rightLights[2].enabled = false;
                    leftLights[2].enabled = false;
                    rightLights[4].enabled = true;
                    leftLights[4].enabled = true;
                    rightLights[1].enabled = true;
                    leftLights[1].enabled = true;
                    logger.Debug("Pair 2 (2-7) and Pair 5 (5-10) flashing together");

                }
                // Wait for high delay (slow pattern duration)
                yield return new WaitForSeconds(delayHigh);

                // Turn on Pair 1 (1-6) and Pair 4 (4-9) flashing together
                if (rightLights[1] != null && leftLights[1] != null && rightLights[3] != null && leftLights[3] != null)
                {
                    rightLights[4].enabled = false;
                    leftLights[4].enabled = false;
                    rightLights[1].enabled = false;
                    leftLights[1].enabled = false;
                    rightLights[3].enabled = true;
                    leftLights[3].enabled = true;
                    rightLights[0].enabled = true;
                    leftLights[0].enabled = true;
                    logger.Debug("Pair 1 (1-6) and Pair 4 (4-9) flashing together");
                }

                // Wait for high delay (slow pattern duration)
                yield return new WaitForSeconds(delayHigh);

                // Turn on Pair 3 (3-8) flashing together
                if (rightLights[2] != null && leftLights[2] != null)
                {
                    rightLights[3].enabled = false;
                    leftLights[3].enabled = false;
                    rightLights[0].enabled = false;
                    leftLights[0].enabled = false;
                    rightLights[2].enabled = true;
                    leftLights[2].enabled = true;
                    logger.Debug("Pair 3 (3-8) flashing together");
                }
                // Wait before restarting the loop
                yield return new WaitForSeconds(delayHigh);
            }
        }

        // Reverse Fast Pattern (9.4118 Hz)
        IEnumerator ReverseFastPattern()
        {
            while (true)  // Looping for continuous pattern
            {
                logger.Debug("Starting Reverse Fast Pattern.");
                // Turn on Pair 1 (1-6) and Pair 4 (4-9) flashing together
                if (rightLights[1] != null && leftLights[1] != null && rightLights[4] != null && leftLights[4] != null)
                {
                    rightLights[2].enabled = false;
                    leftLights[2].enabled = false;
                    rightLights[4].enabled = true;
                    leftLights[4].enabled = true;
                    rightLights[1].enabled = true;
                    leftLights[1].enabled = true;
                    logger.Debug("Pair 2 (2-7) and Pair 5 (5-10) flashing together");
                }
                // Wait for high delay (slow pattern duration)
                yield return new WaitForSeconds(delayLow);

                // Turn on Pair 1 (1-6) and Pair 4 (4-9) flashing together
                if (rightLights[0] != null && leftLights[0] != null && rightLights[3] != null && leftLights[3] != null)
                {
                    rightLights[4].enabled = false;
                    leftLights[4].enabled = false;
                    rightLights[1].enabled = false;
                    leftLights[1].enabled = false;
                    rightLights[3].enabled = true;
                    leftLights[3].enabled = true;
                    rightLights[0].enabled = true;
                    leftLights[0].enabled = true;
                    logger.Debug("Pair 1 (1-6) and Pair 4 (4-9) flashing together");
                }

                // Wait for high delay (slow pattern duration)
                yield return new WaitForSeconds(delayLow);

                // Turn on Pair 3 (3-8) flashing together
                if (rightLights[2] != null && leftLights[2] != null)
                {
                    rightLights[3].enabled = false;
                    leftLights[3].enabled = false;
                    rightLights[0].enabled = false;
                    leftLights[0].enabled = false;
                    rightLights[2].enabled = true;
                    leftLights[2].enabled = true;
                    logger.Debug("Pair 3 (3-8) flashing together");
                }

                // Wait for high delay (slow pattern duration)
                yield return new WaitForSeconds(delayLow);
            }
        }
        // Coroutine for the strobe light sequence (for pattern "07")
        IEnumerator StrobeLightSequence()
        {
            logger.Debug("Strobing.");
            while (true)
            {
                joyLight.enabled = true;
                bottomLight.enabled = true;
                joyLight.enabled = true;
                SetEmissive(bezelObject, true);
                SetEmissive(backlightObject, true);
                SetEmissive(joystickObject, true);
                SetEmissive(frontlightObject, true);
                SetEmissive(backlightObject, true);
                SetEmissive(sticktriggerObject, true);
                SetEmissive(floorObject, true);
                SetEmissive(shroudRightObject, true);
                SetEmissive(shroudLeftObject, true);
                SetEmissive(overlayObject, true);
                SetEmissive(topLightObject, true);
                SetEmissive(topLight2Object, true);
                SetEmissive(leftReflectObject, true);
                SetEmissive(rightReflectObject, true);

                yield return new WaitForSeconds(0.077616f);  // Flash duration (77.616 ms on)

                joyLight.enabled = false;
                strobeLight.enabled = false;  // Turn off the strobe light
                bottomLight.enabled = false;
                SetEmissive(bezelObject, false);
                SetEmissive(backlightObject, false);
                SetEmissive(joystickObject, false);
                SetEmissive(frontlightObject, false);
                SetEmissive(backlightObject, false);
                SetEmissive(sticktriggerObject, false);
                SetEmissive(floorObject, false);
                SetEmissive(shroudRightObject, false);
                SetEmissive(shroudLeftObject, false);
                SetEmissive(overlayObject, false);
                SetEmissive(topLightObject, false);
                SetEmissive(topLight2Object, false);
                SetEmissive(leftReflectObject, false);
                SetEmissive(rightReflectObject, false);
                yield return new WaitForSeconds(0.038808f);  // Off duration (38.808 ms off)
            }
        }

        // Coroutine for the background flashing effect
        IEnumerator BackgroundFlashingEffect()
        {
            logger.Debug("Flashing Background.");
            while (true)
            {

                // Make background more visible (clearer)
                Color newColor = originalColor;
                newColor.a = 0.2f;  // 20% opacity
                backgroundMaterial.color = newColor;  // Apply the new color with modified alpha
                strobeLight.enabled = true;
                yield return new WaitForSeconds(0.077616f);  // Flash duration (77.616 ms on)

                // Make background less visible (opaque)
                backgroundMaterial.color = originalColor;  // Restore to original alpha 
                strobeLight.enabled = false;
                yield return new WaitForSeconds(0.038808f);  // Off duration (38.808 ms off)
            }
        }

        // Coroutine for the strobe light sequence (for pattern "07")
        IEnumerator FootLightSequence()
        {
            logger.Debug("Flashing Footstrobes.");
            while (true)
            {
                bottomLight.enabled = true;
                yield return new WaitForSeconds(0.1f);  // Adjust the flash duration for the strobe effect
                bottomLight.enabled = false;
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Test function to ensure all lights are on
        void TurnOnAllLights()
        {
            /*
            foreach (Light light in rightLights)
            {
                light.enabled = true;
            }

            foreach (Light light in leftLights)
            {
                light.enabled = true;
            }
            */
            if (joyLight != null)
            {
                joyLight.enabled = true;  // Turn on joystick light
            }
            else
            {
                logger.Debug($"{gameObject.name} Joystick light is not assigned.");
            }
            if (strobeLight != null)
            {
                strobeLight.enabled = true;  // Turn on strobe light
            }
            else
            {
                logger.Debug($"{gameObject.name} strobe light is not assigned.");
            }

            if (bottomLight != null)
            {
                bottomLight.enabled = true;  // Turn on bottom light
            }
            else
            {
                logger.Debug($"{gameObject.name} Bottom light is not assigned.");
            }
        }
        void TurnOffTopLights()
        {
            if (rightLights != null)
            {
                foreach (Light light in rightLights)
                {
                    if (light != null)
                    {
                        light.enabled = false;  // Turn off right lights
                    }
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Right lights array is not assigned.");
            }

            if (leftLights != null)
            {
                foreach (Light light in leftLights)
                {
                    if (light != null)
                    {
                        light.enabled = false;  // Turn off left lights
                    }
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Left lights array is not assigned.");
            }
        }
        // Function to turn off all lights
        void TurnOffAllLights()
        {
            if (rightLights != null)
            {
                foreach (Light light in rightLights)
                {
                    if (light != null)
                    {
                        light.enabled = false;  // Turn off right lights
                    }
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Right lights array is not assigned.");
            }

            if (leftLights != null)
            {
                foreach (Light light in leftLights)
                {
                    if (light != null)
                    {
                        light.enabled = false;  // Turn off left lights
                    }
                }
            }
            else
            {
                logger.Debug($"{gameObject.name} Left lights array is not assigned.");
            }

            if (joyLight != null)
            {
                joyLight.enabled = false;  // Turn off joystick light
            }
            else
            {
                logger.Debug($"{gameObject.name} Joystick light is not assigned.");
            }
            if (strobeLight != null)
            {
                strobeLight.enabled = false;  // Turn off strobe light
            }
            else
            {
                logger.Debug($"{gameObject.name} strobe light is not assigned.");
            }

            if (bottomLight != null)
            {
                bottomLight.enabled = false;  // Turn off bottom light
            }
            else
            {
                logger.Debug($"{gameObject.name} Bottom light is not assigned.");
            }
        }

        void DisableAllEmissives()
        {
            logger.Debug($"{gameObject.name} Disabling all emissives at startup...");

            // List of all emissive GameObjects
            Transform[] emissiveObjects = new Transform[]
            {
        bezelObject, shroudLeftObject, shroudRightObject, backlightObject, frontlightObject, sticktriggerObject, cabinetObject, floorObject, joystickObject,
        marqueeObject, topLightObject, topLight2Object, frontObject, leftReflectObject, backgroundObject, overlayObject, rightReflectObject
            };

            // Loop through each emissive object and disable its emission
            foreach (var emissiveObject in emissiveObjects)
            {
                if (emissiveObject != null)
                {
                    Renderer renderer = emissiveObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                        logger.Debug($"{emissiveObject.name} emission disabled.");
                    }
                }
                else
                {
                    logger.Debug($"Emissive object {emissiveObject?.name} not found.");
                }
            }
        }
        // Method to enable or disable emissive based on the Transform reference
        void SetEmissive(Transform emissiveTransform, bool enable)
        {
            if (emissiveTransform != null)
            {
                Renderer renderer = emissiveTransform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // If the renderer is disabled, enable it
                    if (!renderer.enabled)
                    {
                        renderer.enabled = true;
                        logger.Debug($"{emissiveTransform.name} Renderer was disabled, now enabled.");
                    }

                    // Enable or disable the emission keyword based on the flag
                    if (enable)
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                        logger.Debug($"{emissiveTransform.name} emission enabled.");
                    }
                    else
                    {
                        renderer.material.DisableKeyword("_EMISSION");
                        logger.Debug($"{emissiveTransform.name} emission disabled.");
                    }
                }
                else
                {
                    logger.Warning($"{emissiveTransform.name} does not have a Renderer component.");
                }
            }
            else
            {
                logger.Warning($"Emissive object {emissiveTransform?.name} not found.");
            }
        }
        void EnableAllEmissives()
        {
            logger.Debug($"{gameObject.name} enabling all emissives at boot..");

            // List of all emissive GameObjects
            Transform[] emissiveObjects = new Transform[]
            {
        bezelObject, shroudLeftObject, shroudRightObject, backlightObject, frontlightObject, sticktriggerObject, floorObject, cabinetObject, joystickObject,
        marqueeObject, topLightObject, topLight2Object, frontObject, overlayObject, leftReflectObject, rightReflectObject
            };

            // Loop through each emissive object and disable its emission
            foreach (var emissiveObject in emissiveObjects)
            {
                if (emissiveObject != null)
                {
                    Renderer renderer = emissiveObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                        logger.Debug($"{emissiveObject.name} emission enabled.");
                    }
                }
                else
                {
                    logger.Debug($"Emissive object {emissiveObject?.name} not found.");
                }
            }
        }
        void InitializeObjects()
        {
            bezelObject = transform.Find("Emissives/bezel");
            if (bezelObject != null) logger.Debug($"{gameObject.name} bezel found.");
            else logger.Warning("bezel not found.");

            shroudLeftObject = transform.Find("Emissives/shroudleft");
            if (shroudLeftObject != null) logger.Debug($"{gameObject.name} shroudleft found.");
            else logger.Warning("shroudleft not found.");

            shroudRightObject = transform.Find("Emissives/shroudright");
            if (shroudRightObject != null) logger.Debug($"{gameObject.name} shroudright found.");
            else logger.Warning("shroudright not found.");

            backlightObject = transform.Find("Emissives/backlight");
            if (backlightObject != null) logger.Debug($"{gameObject.name} backlight found.");
            else logger.Warning("backlight not found.");

            frontlightObject = transform.Find("Emissives/frontlight");
            if (frontlightObject != null) logger.Debug($"{gameObject.name} frontlight found.");
            else logger.Warning("frontlight not found.");

            // Find stick under Emissives
            StickObject = transform.Find("Emissives/Stick");
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

            sticktriggerObject = transform.Find("Emissives/Stick/sticktrigger");
            if (sticktriggerObject != null) logger.Debug($"{gameObject.name} sticktrigger found.");
            else logger.Warning("sticktrigger not found.");

            floorObject = transform.Find("Emissives/floor");
            if (floorObject != null) logger.Debug($"{gameObject.name} floor found.");
            else logger.Warning("floor not found.");

            joystickObject = transform.Find("Emissives/joystick");
            if (joystickObject != null) logger.Debug($"{gameObject.name} joystick found.");
            else logger.Warning("joystick not found.");

            marqueeObject = transform.Find("Emissives/marquee");
            if (marqueeObject != null) logger.Debug($"{gameObject.name} marquee found.");
            else logger.Warning("marquee not found.");

            topLightObject = transform.Find("Emissives/toplight");
            if (topLightObject != null) logger.Debug($"{gameObject.name} toplight found.");
            else logger.Warning("toplight not found.");

            topLight2Object = transform.Find("Emissives/toplight2");
            if (topLight2Object != null) logger.Debug($"{gameObject.name} toplight2 found.");
            else logger.Warning("toplight2 not found.");

            frontObject = transform.Find("Emissives/front");
            if (frontObject != null) logger.Debug($"{gameObject.name} front found.");
            else logger.Warning("front not found.");

            leftReflectObject = transform.Find("Emissives/leftreflect");
            if (leftReflectObject != null) logger.Debug($"{gameObject.name} leftreflect found.");
            else logger.Warning("leftreflect not found.");

            cabinetObject = transform.Find("Emissives/Cabinet");
            if (cabinetObject != null) logger.Debug($"{gameObject.name} Cabinet found.");
            else logger.Warning("Cabinet not found.");

            rightReflectObject = transform.Find("Emissives/rightreflect");
            if (rightReflectObject != null) logger.Debug($"{gameObject.name} rightreflect found.");
            else logger.Warning("rightreflect not found.");

            backgroundObject = transform.Find("Emissives/background/background (Transparent)");
            if (backgroundObject != null) logger.Debug($"{gameObject.name} background (Transparent) found.");
            else logger.Warning("background (Transparent) not found.");
            // Get the material of the background object
            backgroundMaterial = backgroundObject.GetComponent<Renderer>().material;
            originalColor = backgroundMaterial.color;

            overlayObject = transform.Find("Emissives/overlay");
            if (overlayObject != null) logger.Debug($"{gameObject.name} overlay found.");
            else logger.Warning("overlay not found.");
        }
        void InitializeLights()
        {
            // Light component arrays
            // Dynamically assign joyLight and footLight if they are not manually assigned
            joyLight = GameObject.Find("Lights/joylight")?.GetComponent<Light>();
            if (joyLight != null)
            {
                logger.Debug($"{gameObject.name} joyLight found.");
            }
            else
            {
                logger.Warning("joyLight not found. Ensure it's active and correctly named under 'Lights'.");
            }

            bottomLight = GameObject.Find("Lights/bottomlight")?.GetComponent<Light>();
            if (bottomLight != null)
            {
                logger.Debug($"{gameObject.name} bottomLight found.");
            }
            else
            {
                logger.Warning("bottomLight not found. Ensure it's active and correctly named under 'Lights'.");
            }

            strobeLight = GameObject.Find("Lights/strobelight")?.GetComponent<Light>();
            if (strobeLight != null)
            {
                logger.Debug($"{gameObject.name} strobelight found.");
            }
            else
            {
                logger.Warning("strobelight not found. Ensure it's active and correctly named under 'Lights'.");
            }
            // Optional: Debug log to confirm objects were found
            logger.Debug($"{gameObject.name} All Emissive objects have been checked.");

            // Assign lights to the rightLights array (top light 1 to 5)
            for (int i = 0; i < 5; i++)
            {
                string lightName = "top light " + (i + 1); // "top light 1", "top light 2", ..., "top light 5"
                GameObject lightObject = GameObject.Find(lightName);

                if (lightObject != null)
                {
                    rightLights[i] = lightObject.GetComponent<Light>();  // Assign to the array
                    logger.Debug($"{lightName} assigned to rightLights[{i}]");
                }
                else
                {
                    logger.Debug($"{lightName} not found in the scene.");
                }
            }

            // Assign lights to the leftLights array (top light 6 to 10)
            for (int i = 0; i < 5; i++)
            {
                string lightName = "top light " + (i + 6); // "top light 6", "top light 7", ..., "top light 10"
                GameObject lightObject = GameObject.Find(lightName);

                if (lightObject != null)
                {
                    leftLights[i] = lightObject.GetComponent<Light>();  // Assign to the array
                    logger.Debug($"{lightName} assigned to leftLights[{i}]");
                }
                else
                {
                    logger.Debug($"{lightName} not found in the scene.");
                }
            }
        }
    }
}
