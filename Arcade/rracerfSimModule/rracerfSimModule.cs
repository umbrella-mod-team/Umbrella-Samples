using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.rracerfSim
{
    public class rracerfSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private readonly float keyboardVelocityX = 100.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityY = 100.5f;  // Velocity for keyboard input
        private readonly float keyboardVelocityZ = 100.5f;  // Velocity for keyboard input
        private readonly float vrVelocity = 75.5f;        // Velocity for VR controller input

        private readonly float centeringVelocityX = 75.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityY = 75.5f;  // Velocity for centering rotation
        private readonly float centeringVelocityZ = 75.5f;  // Velocity for centering rotation

        // Controller animation 
        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 300.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 300.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 300.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 250.5f;        // Velocity for VR controller input

        private float controllerrotationLimitX = 20f;  // Rotation limit for X-axis (stick or wheel)
        private float controllerrotationLimitY = 20f;  // Rotation limit for Y-axis (stick or wheel)
        private float controllerrotationLimitZ = 20f;  // Rotation limit for Z-axis (stick or wheel)

        private float currentControllerRotationX = 0f;  // Current rotation for X-axis (stick or wheel)
        private float currentControllerRotationY = 0f;  // Current rotation for Y-axis (stick or wheel)
        private float currentControllerRotationZ = 0f;  // Current rotation for Z-axis (stick or wheel)

        private readonly float centeringControllerVelocityX = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityY = 400.5f;  // Velocity for centering rotation (stick or wheel)
        private readonly float centeringControllerVelocityZ = 400.5f;  // Velocity for centering rotation (stick or wheel)

        private Transform rracerfControllerX; // Reference to the main animated controller (wheel)
        private Vector3 rracerfControllerXStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion rracerfControllerXStartRotation; // Initial controlller positions and rotations for resetting
        private Transform rracerfControllerY; // Reference to the main animated controller (wheel)
        private Vector3 rracerfControllerYStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion rracerfControllerYStartRotation; // Initial controlller positions and rotations for resetting
        private Transform rracerfControllerZ; // Reference to the main animated controller (wheel)
        private Vector3 rracerfControllerZStartPosition; // Initial controller positions and rotations for resetting
        private Quaternion rracerfControllerZStartRotation; // Initial controlller positions and rotations for resetting

        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        //lights
        private Transform rrlightObject;
        public Renderer[] frontEmissiveObjects;
        public Renderer[] rrleftEmissiveObjects;
        public Renderer[] rrrightEmissiveObjects;
        private Coroutine rrfrontCoroutine;
        private Coroutine rrleftCoroutine;
        private Coroutine rrrightCoroutine;
        private float frontFlashDuration = 0.4f;
        private float frontFlashDelay = 0.17f;
        private float sideFlashDuration = 0.3f;
        private float sideFlashDelay = 0.05f;
        private float frontDangerDuration = 0.2f;
        private float frontDangerDelay = 0.2f;
        private float sideDangerDuration = 0.1f;
        private float sideDangerDelay = 0.2f;
        private Transform fireemissiveObject;
        public Light fire1_light;
        public Light fire2_light;
        public float lightDuration = 0.35f; // Duration during which the lights will be on

        private Light[] lights;
        private bool inFocusMode = false;  // Flag to track focus mode state

        private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>();  // Dictionary to store original parents of objects

        void Start()
        {
            // Find rracerfControllerX under rracerfZ
            rracerfControllerX = transform.Find("rracerfControllerX");
            if (rracerfControllerX != null)
            {
                // Store initial position and rotation of the stick
                rracerfControllerXStartPosition = rracerfControllerX.transform.position; // these could cause the controller to mess up
                rracerfControllerXStartRotation = rracerfControllerX.transform.rotation;

                // Find rracerfControllerY under rracerfControllerX
                rracerfControllerY = rracerfControllerX.Find("rracerfControllerY");
                if (rracerfControllerY != null)
                {
                    logger.Info("rracerfControllerY object found.");
                    // Store initial position and rotation of the stick
                    rracerfControllerYStartPosition = rracerfControllerY.transform.position;// these could cause the controller to mess up
                    rracerfControllerYStartRotation = rracerfControllerY.transform.rotation;

                    // Find rracerfControllerZ under rracerfControllerY
                    rracerfControllerZ = rracerfControllerY.Find("rracerfControllerZ");
                    if (rracerfControllerZ != null)
                    {
                        logger.Info("rracerfControllerZ object found.");
                        // Store initial position and rotation of the stick
                        rracerfControllerZStartPosition = rracerfControllerZ.transform.position;// these could cause the controller to mess up
                        rracerfControllerZStartRotation = rracerfControllerZ.transform.rotation;
                    }
                    else
                    {
                        logger.Error("rracerfControllerZ object not found under rracerfControllerY!");
                    }
                }
                else
                {
                    logger.Error("rracerfControllerY object not found under rracerfControllerX!");
                }
            }
            else
            {
                logger.Error("rracerfControllerX object not found");
            }
            InitializeEmissiveArrays();
            // Store the filtered lights
            StartAttractPattern();
        }

        void Update()
        {
            bool inputDetected = false; // Initialize at the beginning of the Update method
            if (Input.GetKey(KeyCode.O))
            {
                logger.Info("Resetting Positions");
                ResetPositions();
            }
        }
        /*
        void StartFocusMode()
        {
            string controlledSystemGamePathString = GameSystem.ControlledSystem.Game.path != null ? GameSystem.ControlledSystem.Game.path.ToString() : null;
            logger.Info($"Controlled System Game path String: {controlledSystemGamePathString}");
            logger.Info("Compatible Rom Dectected, Lower Safty Bar...");
            logger.Info("Sega rracerf Motion Sim starting...");
            logger.Info("Vomit bags are below the seat...");
            StartDangerPattern();
            // Set objects as children of cockpit cam for focus mode
            if (cockpitCam != null)
            {
                if (playerCamera != null)
                {
                    // Store initial position, rotation, and scale of PlayerCamera
                    playerCameraStartPosition = playerCamera.transform.position;
                    playerCameraStartRotation = playerCamera.transform.rotation;
                    playerCameraStartScale = playerCamera.transform.localScale; // Store initial scale
                    SaveOriginalParent(playerCamera);  // Save original parent of PlayerCamera

                    // Set PlayerCamera as child of cockpit cam and maintain scale
                    playerCamera.transform.SetParent(cockpitCam.transform, false);
                    playerCamera.transform.localScale = playerCameraStartScale;  // Reapply initial scale
                    playerCamera.transform.localRotation = Quaternion.identity;
                    logger.Info("PlayerCamera set as child of CockpitCam.");
                }

                if (playerVRSetup != null)
                {
                    // Store initial position, rotation, and scale of PlayerVRSetup
                    playerVRSetupStartPosition = playerVRSetup.transform.position;
                    playerVRSetupStartRotation = playerVRSetup.transform.rotation;
                    playerVRSetupStartScale = playerVRSetup.transform.localScale; // Store initial scale
                    SaveOriginalParent(playerVRSetup);  // Save original parent of PlayerVRSetup

                    // Set PlayerVRSetup as child of cockpit cam and maintain scale
                    playerVRSetup.transform.SetParent(cockpitCam.transform, false);
                    playerVRSetup.transform.localScale = playerVRSetupStartScale;
                    playerVRSetup.transform.localRotation = Quaternion.identity;
                    logger.Info("PlayerVRSetup.PlayerRig set as child of CockpitCam.");
                }
            }
            else
            {
                logger.Error("CockpitCam object not found under rracerfZ!");
            }
            playerCamera.transform.localScale = playerCameraStartScale;
            //        cockpitCam.transform.position = cockpitCamStartPosition; // new hotness
            //        cockpitCam.transform.rotation = cockpitCamStartRotation; // new hotness
            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Info("Exiting Focus Mode...");
            // Restore original parents of objects
            RestoreOriginalParent(playerCamera, "PlayerCamera");
            RestoreOriginalParent(playerVRSetup, "PlayerVRSetup.PlayerRig");

            // Reset rracerfX to initial positions and rotations
            if (rracerfXObject != null)
            {
                rracerfXObject.position = rracerfXStartPosition;
                rracerfXObject.rotation = rracerfXStartRotation;
            }

            // Reset rracerfY object to initial position and rotation
            if (rracerfYObject != null)
            {
                rracerfYObject.position = rracerfYStartPosition;
                rracerfYObject.rotation = rracerfYStartRotation;
            }
            // Reset rracerfZ object to initial position and rotation
            if (rracerfZObject != null)
            {
                rracerfZObject.position = rracerfZStartPosition;
                rracerfZObject.rotation = rracerfZStartRotation;
            }
            if (rracerfControllerX != null)
            {
                rracerfControllerX.position = rracerfControllerXStartPosition;
                rracerfControllerX.rotation = rracerfControllerXStartRotation;
            }
            // Reset rracerfY to initial positions and rotations
            if (rracerfControllerY != null)
            {
                rracerfControllerY.position = rracerfControllerYStartPosition;
                rracerfControllerY.rotation = rracerfControllerYStartRotation;
            }
            if (rracerfControllerZ != null)
            {
                rracerfControllerZ.position = rracerfControllerZStartPosition;
                rracerfControllerZ.rotation = rracerfControllerZStartRotation;
            }

            // Reset rotation allowances and current rotation values
            logger.Info("Resetting Positions");
            ResetPositions();
            StartAttractPattern();
            inFocusMode = false;  // Clear focus mode flag
        }

        void HandleKeyboardInput(ref bool inputDetected)
        {
            if (!inFocusMode) return;
            /*
            // Handle keyboard input for pitch and roll
            if (Input.GetKey(KeyCode.DownArrow) && currentRotationX > -rotationLimitX)
            {
                float rotateX = keyboardVelocityX * Time.deltaTime;
                rracerfXObject.Rotate(rotateX, 0, 0);
                currentRotationX -= rotateX;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.UpArrow) && currentRotationX < rotationLimitX)
            {
                float rotateX = keyboardVelocityX * Time.deltaTime;
                rracerfXObject.Rotate(-rotateX, 0, 0);
                currentRotationX += rotateX;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.LeftArrow) && currentRotationY > -rotationLimitY)
            {
                float rotateY = keyboardVelocityY * Time.deltaTime;
                rracerfYObject.Rotate(0, rotateY, 0);
                currentRotationY -= rotateY;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.RightArrow) && currentRotationY < rotationLimitY)
            {
                float rotateY = keyboardVelocityY * Time.deltaTime;
                rracerfYObject.Rotate(0, -rotateY, 0);
                currentRotationY += rotateY;
                inputDetected = true;
            }
            /*
            if (Input.GetKey(KeyCode.DownArrow) && currentRotationZ > -rotationLimitZ)
            {
                float rotateZ = keyboardVelocityZ * Time.deltaTime;
                rracerfZObject.Rotate(0, 0, rotateZ);
                currentRotationZ -= rotateZ;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.UpArrow) && currentRotationZ < rotationLimitZ)
            {
                float rotateZ = keyboardVelocityZ * Time.deltaTime;
                rracerfZObject.Rotate(0, 0, -rotateZ);
                currentRotationZ += rotateZ;
                inputDetected = true;
            }
            */
            /*
            // Stick Rotations 
            // Stick Y Rotation
            if (Input.GetKey(KeyCode.RightArrow) && currentControllerRotationY > -controllerrotationLimitY)
            {
                float rotateY = keyboardControllerVelocityY * Time.deltaTime;
                rracerfControllerY.Rotate(0, rotateY, 0);
                currentControllerRotationY -= rotateY;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.LeftArrow) && currentControllerRotationY < controllerrotationLimitY)
            {
                float rotateY = keyboardControllerVelocityY * Time.deltaTime;
                rracerfControllerY.Rotate(0, -rotateY, 0);
                currentControllerRotationY += rotateY;
                inputDetected = true;
            }


            // Stick X Rotation
            if (Input.GetKey(KeyCode.UpArrow) && currentControllerRotationX > -controllerrotationLimitX)
            {
                float rotateX = keyboardControllerVelocityX * Time.deltaTime;
                rracerfControllerZ.Rotate(rotateX, 0, 0);
                currentControllerRotationX -= rotateX;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.DownArrow) && currentControllerRotationX < controllerrotationLimitX)
            {
                float rotateX = keyboardControllerVelocityX * Time.deltaTime;
                rracerfControllerZ.Rotate(-rotateX, 0, 0);
                currentControllerRotationX += rotateX;
                inputDetected = true;
            }
            /*
            // Stick Z Rotation
            if (Input.GetKey(KeyCode.LeftArrow) && currentControllerRotationZ > -controllerrotationLimitZ)
            {
                float rotateZ = keyboardControllerVelocityZ * Time.deltaTime;
                rracerfControllerZ.Rotate(0, 0, rotateZ);
                currentControllerRotationZ -= rotateZ;
                inputDetected = true;
            }

            if (Input.GetKey(KeyCode.RightArrow) && currentControllerRotationZ < controllerrotationLimitZ)
            {
                float rotateZ = keyboardControllerVelocityZ * Time.deltaTime;
                rracerfControllerZ.Rotate(0, 0, -rotateZ);
                currentControllerRotationZ += rotateZ;
                inputDetected = true;
            }


            // Center the rotation if no input is detected
            if (!inputDetected)
            {
                CenterRotation();
            }
        }
        */

        void ResetPositions()
        {
            currentControllerRotationX = 0f;
            currentControllerRotationY = 0f;
            currentControllerRotationZ = 0f;
        }

        void CenterRotation()
        {
            
            //Centering for contoller
            // Center Y-axis Controller rotation
            if (currentControllerRotationY > 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, currentControllerRotationY);
                rracerfControllerY.Rotate(0, unrotateY, 0);   // Rotating to reduce the rotation
                currentControllerRotationY -= unrotateY;    // Reducing the positive rotation
            }
            else if (currentControllerRotationY < 0)
            {
                float unrotateY = Mathf.Min(centeringControllerVelocityY * Time.deltaTime, -currentControllerRotationY);
                rracerfControllerY.Rotate(0, -unrotateY, 0);  // Rotating to reduce the rotation
                currentControllerRotationY += unrotateY;    // Reducing the negative rotation
            }

            // Center X-Axis Controller rotation //swapped to Z
            if (currentControllerRotationX > 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, currentControllerRotationX);
                rracerfControllerZ.Rotate(unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX -= unrotateX;    // Reducing the positive rotation
            }
            else if (currentControllerRotationX < 0)
            {
                float unrotateX = Mathf.Min(centeringControllerVelocityX * Time.deltaTime, -currentControllerRotationX);
                rracerfControllerZ.Rotate(-unrotateX, 0, 0);   // Rotating to reduce the rotation
                currentControllerRotationX += unrotateX;    // Reducing the positive rotation
            }

            // Center Z-axis Controller rotation // Swapped to X
            if (currentControllerRotationZ > 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, currentControllerRotationZ);
                rracerfControllerX.Rotate(0, 0, unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ -= unrotateZ;    // Reducing the positive rotation
            }
            else if (currentControllerRotationZ < 0)
            {
                float unrotateZ = Mathf.Min(centeringControllerVelocityZ * Time.deltaTime, -currentControllerRotationZ);
                rracerfControllerX.Rotate(0, 0, -unrotateZ);   // Rotating to reduce the rotation
                currentControllerRotationZ += unrotateZ;    // Reducing the positive rotation
            }
        }

        // Initialize the emissive arrays with the appropriate objects
        private void InitializeEmissiveArrays()
        {
            /*
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
            */
            // Initialize left and right arrays from rrlightObject
            rrleftEmissiveObjects = new Renderer[15];
            rrrightEmissiveObjects = new Renderer[15];
            rrlightObject = transform.Find("rrlight");
            if (rrlightObject != null)
            {
                // Left side
                rrleftEmissiveObjects[0] = rrlightObject.Find("lllight1")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[1] = rrlightObject.Find("lllight2")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[2] = rrlightObject.Find("lllight3")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[3] = rrlightObject.Find("lllight4")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[4] = rrlightObject.Find("lllight5")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[5] = rrlightObject.Find("lllight6")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[6] = rrlightObject.Find("lllight7")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[7] = rrlightObject.Find("lllight8")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[8] = rrlightObject.Find("lllight9")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[9] = rrlightObject.Find("lllight10")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[10] = rrlightObject.Find("lllight11")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[11] = rrlightObject.Find("lllight12")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[12] = rrlightObject.Find("lllight13")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[13] = rrlightObject.Find("lllight14")?.GetComponent<Renderer>();
                rrleftEmissiveObjects[14] = rrlightObject.Find("lllight15")?.GetComponent<Renderer>();

                // Right side
                rrrightEmissiveObjects[0] = rrlightObject.Find("rllight1")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[1] = rrlightObject.Find("rllight2")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[2] = rrlightObject.Find("rllight3")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[3] = rrlightObject.Find("rllight4")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[4] = rrlightObject.Find("rllight5")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[5] = rrlightObject.Find("rllight6")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[6] = rrlightObject.Find("rllight7")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[7] = rrlightObject.Find("rllight8")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[8] = rrlightObject.Find("rllight9")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[9] = rrlightObject.Find("rllight10")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[10] = rrlightObject.Find("rllight11")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[11] = rrlightObject.Find("rllight12")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[12] = rrlightObject.Find("rllight13")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[13] = rrlightObject.Find("rllight14")?.GetComponent<Renderer>();
                rrrightEmissiveObjects[14] = rrlightObject.Find("rllight15")?.GetComponent<Renderer>();
            }

        //    LogMissingObject(frontEmissiveObjects, "frontEmissiveObjects");
            LogMissingObject(rrleftEmissiveObjects, "rrleftEmissiveObjects");
            LogMissingObject(rrrightEmissiveObjects, "rrrightEmissiveObjects");
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
                    Debug.Log("Renderer component not found on one of the emissive objects.");
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
        /*
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
        */
        // Attract pattern for the side
        IEnumerator SideAttractPattern(Renderer[] emissiveObjects)
        {
            int totalLights = emissiveObjects.Length;
            int frameCount = 4; // Since the pattern repeats every 4 frames

            while (true)
            {
                for (int frame = 0; frame < frameCount; frame++)
                {
                    // Turn all lights on
                    for (int i = 0; i < totalLights; i++)
                    {
                        ToggleEmissive(emissiveObjects[i], true);
                    }

                    // Turn off specific lights based on the current frame
                    for (int i = frame; i < totalLights; i += frameCount)
                    {
                        ToggleEmissive(emissiveObjects[i], false);
                    }

                    // Wait before moving to the next frame
                    yield return new WaitForSeconds(sideFlashDuration);
                }

                // Add a small delay between cycles
                yield return new WaitForSeconds(sideFlashDelay);
            }
        }

        /*
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
        */
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
         //   ToggleAll(frontEmissiveObjects, false);
            ToggleAll(rrleftEmissiveObjects, false);
            ToggleAll(rrrightEmissiveObjects, false);
        }

        public void StartAttractPattern()
        {
            StopCurrentPatterns();

            // frontCoroutine = StartCoroutine(FrontAttractPattern());
            rrleftCoroutine = StartCoroutine(SideAttractPattern(rrleftEmissiveObjects));
            rrrightCoroutine = StartCoroutine(SideAttractPattern(rrrightEmissiveObjects));
        }

        public void StartDangerPattern()
        {
            StopCurrentPatterns();

            // frontCoroutine = StartCoroutine(FrontDangerPattern());
            rrleftCoroutine = StartCoroutine(SideDangerPattern(rrleftEmissiveObjects));
            rrrightCoroutine = StartCoroutine(SideDangerPattern(rrrightEmissiveObjects));
        }

        private void StopCurrentPatterns()
        {
           // if (frontCoroutine != null)
          //  {
          //      StopCoroutine(frontCoroutine);
         //       frontCoroutine = null;
          //  }

            if (rrleftCoroutine != null)
            {
                StopCoroutine(rrleftCoroutine);
                rrleftCoroutine = null;
            }

            if (rrrightCoroutine != null)
            {
                StopCoroutine(rrrightCoroutine);
                rrrightCoroutine = null;
            }
        }

        // Method to toggle the lights
        void ToggleLights(bool isActive)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].enabled = isActive;
            }

            logger.Info($"Lights turned {(isActive ? "on" : "off")}.");
        }

        // Method to toggle the fireemissive object
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
                    logger.Info($"fireemissive object emission turned {(isActive ? "on" : "off")}.");
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

        // Check if object is found and log appropriate message
        void CheckObject(GameObject obj, string name)
        {
            if (obj == null)
            {
                logger.Error($"{name} not found!");
            }
            else
            {
                logger.Info($"{name} found.");
            }
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
                logger.Info($"{name} restored to original parent.");
            }
        }

        // Unset parent of object and log appropriate message
        void UnsetParentObject(GameObject obj, string name)
        {
            if (obj != null)
            {
                obj.transform.SetParent(null);
                logger.Info($"{name} unset from parent.");
            }
        }

        // Method to check if VR input is active
        bool VRInputActive()
        {
            // Assuming you have methods to check VR input
            return GetPrimaryThumbstick() != Vector2.zero || GetSecondaryThumbstick() != Vector2.zero;
        }

        // Placeholder methods to get VR thumbstick input (to be implemented with actual VR input handling)
        Vector2 GetPrimaryThumbstick()
        {
            // Implement VR primary thumbstick input handling here
            return Vector2.zero;
        }

        Vector2 GetSecondaryThumbstick()
        {
            // Implement VR secondary thumbstick input handling here
            return Vector2.zero;
        }
    }
}
