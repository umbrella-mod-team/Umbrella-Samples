using UnityEngine;
using WIGU;
using System.IO;

namespace WIGUx.Modules.arcadeControllerMotionSim
{
    public class arcadeControllerSimController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        // Controller animation 
        // Speeds for the animation of the in game flight stick or wheel
        private readonly float keyboardControllerVelocityX = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityY = 150.5f;  // Velocity for keyboard input
        private readonly float keyboardControllerVelocityZ = 150.5f;  // Velocity for keyboard input
        private readonly float vrControllerVelocity = 200.5f;        // Velocity for VR/Controller input

        private float primaryThumbstickRotationMultiplier = 10f; // Multiplier for primary thumbstick rotation intensity
        private float secondaryThumbstickRotationMultiplier = 25f; // Multiplier for secondary thumbstick rotation intensity
        private float triggerRotationMultiplier = 20f; // Multiplier for trigger rotation intensity
        private float adjustSpeed = 1.0f;  // Adjust this adjustment speed as needed a lower number will lead to smaller adustments

        // sticks
        private Transform p1StickObject; // Reference to the p1 stick mirroring object
        private Transform p2StickObject; // Reference to the p2 stick mirroring object

        //p1 button movement limits
        private float p1positionLimitstart = 0.003f;  // position limit  for player 1 start button
        private float p1positionLimit1 = 0.003f;  // position limit  for player 1 button 1
        private float p1positionLimit2 = 0.003f;  // position limit  for player 1 button 2
        private float p1positionLimit3 = 0.003f;  // position limit  for player 1 button 3
        private float p1positionLimit4 = 0.003f;  // position limit  for player 1 button 4
        private float p1positionLimit5 = 0.003f;  // position limit  for player 1 button 5
        private float p1positionLimit6 = 0.003f;  // position limit  for player 1 button 6

        //p2 button movement limits
        private float p2positionLimitstart = .003f;  // position limit  for player 2 start button
        private float p2positionLimit1 = 0.003f;  // position limit for player 2 button 1
        private float p2positionLimit2 = 0.003f;  // position limit for player 2 button 2
        private float p2positionLimit3 = 0.003f;  // position limit for player 2 button 3
        private float p2positionLimit4 = 0.003f;  // position limit for player 2 button 4
        private float p2positionLimit5 = 0.003f;  // position limit for player 2 button 5
        private float p2positionLimit6 = 0.003f;  // position limit for player 2 button 6

        // Current postion of buttons for player 1
        private float p1currentStartButtonPosition = 0f;  
        private float p1currentButton1Position = 0f;  // Current position for player 1 Button 1
        private float p1currentButton2Position = 0f;  // Current position for player 1 Button 2
        private float p1currentButton3Position = 0f;  // Current position for player 1 Button 3
        private float p1currentButton4Position = 0f;  // Current position for player 1 Button 4
        private float p1currentButton5Position = 0f;  // Current position for player 1 Button 5
        private float p1currentButton6Position = 0f;  // Current position for player 1 Button 6

        // Current postion of buttons for player 2
        private float p2currentStartButtonPosition = 0f;  // Current postion for start Button for player 2
        private float p2currentButton1Position = 0f;  // Current position for player 2 Button 1
        private float p2currentButton2Position = 0f;  // Current position for player 2 Button 2
        private float p2currentButton3Position = 0f;  // Current position for player 2 Button 3
        private float p2currentButton4Position = 0f;  // Current position for player 2 Button 4
        private float p2currentButton5Position = 0f;  // Current position for player 2 Button 5
        private float p2currentButton6Position = 0f;  // Current position for player 2 Button 6

        private Transform p1StartObject; // Reference to start button on player 1
        private Transform p1Button1Object; // Reference to Button1 on player 1
        private Transform p1Button2Object; // Reference to Button2 on player 1
        private Transform p1Button3Object; // Reference to Button3 on player 1
        private Transform p1Button4Object; // Reference to Button4 on player 1
        private Transform p1Button5Object; // Reference to Button5 on player 1
        private Transform p1Button6Object; // Reference to Button6 on player 1

        private Transform p2StartObject; // Reference to start button on player 2
        private Transform p2Button1Object; // Reference to Button1 on player 2
        private Transform p2Button2Object; // Reference to Button2 on player 2
        private Transform p2Button3Object; // Reference to Button3 on player 2
        private Transform p2Button4Object; // Reference to Button4 on player 2
        private Transform p2Button5Object; // Reference to Button5 on player 2
        private Transform p2Button6Object; // Reference to Button6 on player 2

        private Vector3 p1StartObjectStartPosition;
        private Quaternion p1StartObjectStartRotation;
        private Vector3 p1Button1ObjectStartPosition;
        private Quaternion p1Button1ObjectStartRotation;
        private Vector3 p1Button2ObjectStartPosition;
        private Quaternion p1Button2ObjectStartRotation;
        private Vector3 p1Button3ObjectStartPosition;
        private Quaternion p1Button3ObjectStartRotation;
        private Vector3 p1Button4ObjectStartPosition;
        private Quaternion p1Button4ObjectStartRotation;
        private Vector3 p1Button5ObjectStartPosition;
        private Quaternion p1Button5ObjectStartRotation;
        private Vector3 p1Button6ObjectStartPosition;
        private Quaternion p1Button6ObjectStartRotation;

        //p2 buttons
        private Vector3 p2StartObjectStartPosition;
        private Quaternion p2StartObjectStartRotation;
        private Vector3 p2Button1ObjectStartPosition;
        private Quaternion p2Button1ObjectStartRotation;
        private Vector3 p2Button2ObjectStartPosition;
        private Quaternion p2Button2ObjectStartRotation;
        private Vector3 p2Button3ObjectStartPosition;
        private Quaternion p2Button3ObjectStartRotation;
        private Vector3 p2Button4ObjectStartPosition;
        private Quaternion p2Button4ObjectStartRotation;
        private Vector3 p2Button5ObjectStartPosition;
        private Quaternion p2Button5ObjectStartRotation;
        private Vector3 p2Button6ObjectStartPosition;
        private Quaternion p2Button6ObjectStartRotation;
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private bool inFocusMode = false;  // Flag to track focus mode state
        private string controlledGameName;
        private string insertedGameName;


        void Start()
        {
            gameSystem = GetComponent<GameSystem>();    // Get the GameSystem component from this GameObject.
            ShowInsertedGameName();
            ShowControlledGameName();

            // Find p1Start button object in hierarchy
            p1StartObject = transform.Find("p1Start");
            if (p1StartObject != null)
            {
                logger.Debug("p1startObject found.");
                p1StartObjectStartPosition = p1StartObject.position;
                p1StartObjectStartRotation = p1StartObject.rotation;
            }

            // Find p1 button1 object in hierarchy
            p1Button1Object = transform.Find("p1Button1");
            if (p1Button1Object != null)
            {
                logger.Debug("p1Button1Object found.");
                p1Button1ObjectStartPosition = p1Button1Object.position;
                p1Button1ObjectStartRotation = p1Button1Object.rotation;

            }

            // Find p1 button2 object in hierarchy
            p1Button2Object = transform.Find("p1Button2");
            if (p1Button2Object != null)
            {
                logger.Debug("p1Button2Object found.");
                p1Button2ObjectStartPosition = p1Button2Object.position;
                p1Button2ObjectStartRotation = p1Button2Object.rotation;

            }

            // Find p1 button3 object in hierarchy
            p1Button3Object = transform.Find("p1Button3");
            if (p1Button3Object != null)
            {
                logger.Debug("p1Button3Object found.");
                p1Button3ObjectStartPosition = p1Button3Object.position;
                p1Button3ObjectStartRotation = p1Button3Object.rotation;

            }

            // Find p1 button4 object in hierarchy
            p1Button4Object = transform.Find("p1Button4");
            if (p1Button4Object != null)
            {
                logger.Debug("p1Button4Object found.");
                p1Button4ObjectStartPosition = p1Button4Object.position;
                p1Button4ObjectStartRotation = p1Button4Object.rotation;
            }

            // Find p1 button5 object in hierarchy
            p1Button5Object = transform.Find("p1Button5");
            if (p1Button5Object != null)
            {
                logger.Debug("p1Button5Object found.");
                p1Button5ObjectStartPosition = p1Button5Object.position;
                p1Button5ObjectStartRotation = p1Button5Object.rotation;

            }

            // Find p1 button6 object in hierarchy
            p1Button6Object = transform.Find("p1Button6");
            if (p1Button6Object != null)
            {
                logger.Debug("p1Button6Object found.");
                p1Button6ObjectStartPosition = p1Button6Object.position;
                p1Button6ObjectStartRotation = p1Button6Object.rotation;

            }
            // Find p2 start button object in hierarchy
            p2StartObject = transform.Find("p2Start");
            if (p2StartObject != null)
            {
                logger.Debug("p2startObject found.");
                p2StartObjectStartPosition = p2StartObject.position;
                p2StartObjectStartRotation = p2StartObject.rotation;
            }

            // Find p2 button1 object in hierarchy
            p2Button1Object = transform.Find("p2Button1");
            if (p2Button1Object != null)
            {
                logger.Debug("p2Button1Object found.");
                p2Button1ObjectStartPosition = p2Button1Object.position;
                p2Button1ObjectStartRotation = p2Button1Object.rotation;

            }

            // Find p2 button2 object in hierarchy
            p2Button2Object = transform.Find("p2Button2");
            if (p2Button2Object != null)
            {
                logger.Debug("p2Button2Object found.");
                p2Button2ObjectStartPosition = p2Button2Object.position;
                p2Button2ObjectStartRotation = p2Button2Object.rotation;

            }

            // Find p2 button3 object in hierarchy
            p2Button3Object = transform.Find("p2Button3");
            if (p2Button3Object != null)
            {
                logger.Debug("p2Button3Object found.");
                p2Button3ObjectStartPosition = p2Button3Object.position;
                p2Button3ObjectStartRotation = p2Button3Object.rotation;

            }

            // Find p2 button4 object in hierarchy
            p2Button4Object = transform.Find("p2Button4");
            if (p2Button4Object != null)
            {
                logger.Debug("p2Button4Object found.");
                p2Button4ObjectStartPosition = p2Button4Object.position;
                p2Button4ObjectStartRotation = p2Button4Object.rotation;
            }

            // Find p2 button5 object in hierarchy
            p2Button5Object = transform.Find("p2Button5");
            if (p2Button5Object != null)
            {
                logger.Debug("p2Button5Object found.");
                p2Button5ObjectStartPosition = p2Button5Object.position;
                p2Button5ObjectStartRotation = p2Button5Object.rotation;

            }

            // Find p2 button6 object in hierarchy
            p2Button6Object = transform.Find("p2Button6");
            if (p2Button6Object != null)
            {
                logger.Debug("p2Button6Object found.");
                p2Button6ObjectStartPosition = p2Button6Object.position;
                p2Button6ObjectStartRotation = p2Button6Object.rotation;

            }
        }

        void Update()
        {
            if (insertedGameName == controlledGameName && !inFocusMode)
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
            logger.Debug("focus starting...");

            //Buttons

            // Reset ps1startObject object to initial position and rotation
            if (p1StartObject != null)
            {
                p1StartObject.position = p1StartObjectStartPosition;
                p1StartObject.rotation = p1StartObjectStartRotation;
            }
            // Reset p1Button1Object to initial positions and rotations
            if (p1Button1Object != null)
            {
                p1Button1Object.position = p1Button1ObjectStartPosition;
                p1Button1Object.rotation = p1Button1ObjectStartRotation;
            }
            // Reset p1Button2Object to initial positions and rotations
            if (p1Button1Object != null)
            {
                p1Button2Object.position = p1Button2ObjectStartPosition;
                p1Button2Object.rotation = p1Button2ObjectStartRotation;
            }
            // Reset p1Button3Object to initial positions and rotations
            if (p1Button3Object != null)
            {
                p1Button3Object.position = p1Button3ObjectStartPosition;
                p1Button3Object.rotation = p1Button3ObjectStartRotation;
            }
            // Reset p1Button4Object to initial positions and rotations
            if (p1Button4Object != null)
            {
                p1Button4Object.position = p1Button4ObjectStartPosition;
                p1Button4Object.rotation = p1Button4ObjectStartRotation;
            }
            // Reset p1Button5Object to initial positions and rotations
            if (p1Button5Object != null)
            {
                p1Button5Object.position = p1Button5ObjectStartPosition;
                p1Button5Object.rotation = p1Button5ObjectStartRotation;
            }
            // Reset p1Button6Object to initial positions and rotations
            if (p1Button6Object != null)
            {
                p1Button6Object.position = p1Button6ObjectStartPosition;
                p1Button6Object.rotation = p1Button6ObjectStartRotation;
            }

            // Reset ps2startObject object to initial position and rotation
            if (p2StartObject != null)
            {
                p2StartObject.position = p2StartObjectStartPosition;
                p2StartObject.rotation = p2StartObjectStartRotation;
            }
            // Reset p1Button1Object to initial positions and rotations
            if (p2Button1Object != null)
            {
                p2Button1Object.position = p2Button1ObjectStartPosition;
                p2Button1Object.rotation = p2Button1ObjectStartRotation;
            }
            // Reset p1Button2Object to initial positions and rotations
            if (p2Button2Object != null)
            {
                p2Button2Object.position = p2Button2ObjectStartPosition;
                p2Button2Object.rotation = p2Button2ObjectStartRotation;
            }
            // Reset p1Button3Object to initial positions and rotations
            if (p2Button3Object != null)
            {
                p2Button3Object.position = p2Button3ObjectStartPosition;
                p2Button3Object.rotation = p2Button3ObjectStartRotation;
            }
            // Reset p1Button4Object to initial positions and rotations
            if (p2Button4Object != null)
            {
                p2Button4Object.position = p2Button4ObjectStartPosition;
                p2Button4Object.rotation = p2Button4ObjectStartRotation;
            }
            // Reset p1Button5Object to initial positions and rotations
            if (p2Button5Object != null)
            {
                p2Button5Object.position = p2Button5ObjectStartPosition;
                p2Button5Object.rotation = p2Button5ObjectStartRotation;
            }
            // Reset p1Button6Object to initial positions and rotations
            if (p2Button6Object != null)
            {
                p2Button6Object.position = p2Button6ObjectStartPosition;
                p2Button6Object.rotation = p2Button6ObjectStartRotation;
            }

            // Reset buttons current values
            p1currentStartButtonPosition = 0f;
            p1currentButton1Position = 0f;
            p1currentButton2Position = 0f;
            p1currentButton3Position = 0f;
            p1currentButton4Position = 0f;
            p1currentButton5Position = 0f;
            p1currentButton6Position = 0f;
            p1currentStartButtonPosition = 0f;
            p2currentButton1Position = 0f;
            p2currentButton2Position = 0f;
            p2currentButton3Position = 0f;
            p2currentButton4Position = 0f;
            p2currentButton5Position = 0f;
            p2currentButton6Position = 0f;

            inFocusMode = true;  // Set focus mode flag
        }

        void EndFocusMode()
        {
            logger.Debug("Exiting Focus Mode...");
            inFocusMode = false;  // Clear focus mode flag
        }

        private void MapThumbsticks()
        {
            if (!inFocusMode) return;

            if (p1StickObject == null) return;


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
            // Map primary thumbstick to player 1 stick
            Quaternion primaryRotation = Quaternion.Euler(primaryThumbstick.y * primaryThumbstickRotationMultiplier, 0f, -primaryThumbstick.x * primaryThumbstickRotationMultiplier);
            p1StickObject.localRotation = primaryRotation;
            // Calculate a new rotation from the thumbstick input.
            /*
            // Map secondary thumbstick to right stick to player 2 stick
            Quaternion secondaryRotation = Quaternion.Euler(-secondaryThumbstick.y * secondaryThumbstickRotationMultiplier, 0f, 0f);
            p2StickObject.localRotation = secondaryRotation;
           */
        }
        private void MapButtons()
        {
            if (!inFocusMode) return;

            // Handle Start button press for Button position
            if ((XInput.GetDown(XInput.Button.Start) || Input.GetKeyDown(KeyCode.JoystickButton7)) && p1currentStartButtonPosition < p1positionLimitstart)
            {
                float p1StartObjectStartPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1StartObject.position += new Vector3(0, -p1positionLimitstart, 0);
                p1currentStartButtonPosition += p1StartObjectStartPosition;
            }
            // Reset position on Start button release
            if (XInput.GetUp(XInput.Button.Start) || Input.GetKeyUp(KeyCode.JoystickButton7))
            {
                p1StartObject.position = p1StartObjectStartPosition;
                p1currentStartButtonPosition = 0f; // Reset the current position
            }
            // Fire1
            if (Input.GetButtonDown("Fire1") && p1currentButton1Position < p1positionLimit1)
            {
                float p1Button1ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1Button1Object.position += new Vector3(0, -p1positionLimit1, 0);
                p1currentButton1Position += p1Button1ObjectPosition;
            }
            // Reset position on button release
            if (Input.GetButtonUp("Fire1"))
            {
                p1Button1Object.position = p1Button1ObjectStartPosition;
                p1currentButton1Position = 0f; // Reset the current position
            }
            // Fire2
            if (Input.GetButtonDown("Fire2") && p1currentButton2Position < p1positionLimit2)
            {
                float p1Button2ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1Button2Object.position += new Vector3(0, -p1positionLimit2, 0);
                p1currentButton2Position += p1Button2ObjectPosition;
            }
            // Reset position on button release
            if (Input.GetButtonUp("Fire2"))
            {
                p1Button2Object.position = p1Button2ObjectStartPosition;
                p1currentButton2Position = 0f; // Reset the current position    
            }
            // Fire3
            if (Input.GetButtonDown("Fire3") && p1currentButton3Position < p1positionLimit3)
            {
                float p1Button3ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1Button3Object.position += new Vector3(0, -p1positionLimit3, 0);
                p1currentButton3Position += p1Button3ObjectPosition;
            }
            // Reset position on button release
            if (Input.GetButtonUp("Fire3"))
            {
                p1Button3Object.position = p1Button3ObjectStartPosition;
                p1currentButton3Position = 0f; // Reset the current position
            }
            // Jump
            if (Input.GetButtonDown("Jump") && p1currentButton4Position < p1positionLimit4)
            {
                float p1Button4ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1Button4Object.position += new Vector3(0, -p1positionLimit4, 0);
                p1currentButton4Position += p1Button4ObjectPosition;
            }
            // Reset position on button release
            if (Input.GetButtonUp("Jump"))
            {
                p1Button4Object.position = p1Button4ObjectStartPosition;
                p1currentButton4Position = 0f; // Reset the current position
            }
            // Handle LB button press for Button position
            if ((XInput.GetDown(XInput.Button.LShoulder) || Input.GetKeyDown(KeyCode.JoystickButton4)) && p1currentButton5Position < p1positionLimit5)
            {
                float p1Button5ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1Button5Object.position += new Vector3(0, -p1positionLimit5, 0);
                p1currentButton6Position += p1Button5ObjectPosition;
            }
            // Reset position on button release
            if (XInput.GetUp(XInput.Button.LShoulder) || Input.GetKeyUp(KeyCode.JoystickButton4))
            {
                p1Button5Object.position = p1Button5ObjectStartPosition;
                p1currentButton5Position = 0f; // Reset the current position
            }

            // Handle RB button press for Button position
            if ((XInput.GetDown(XInput.Button.RShoulder) || Input.GetKeyDown(KeyCode.JoystickButton6)) && p1currentButton6Position < p1positionLimit6)
            {
                float p1Button6ObjectPosition = (Input.GetKey(KeyCode.X) ? keyboardControllerVelocityX : vrControllerVelocity) * Time.deltaTime;
                p1Button6Object.position += new Vector3(0, -p1positionLimit6, 0);
                p1currentButton6Position += p1Button6ObjectPosition;
            }
            // Reset position on button release
            if (XInput.GetUp(XInput.Button.RShoulder) || Input.GetKeyUp(KeyCode.JoystickButton5))
            {
                p1Button6Object.position = p1Button6ObjectStartPosition;
                p1currentButton6Position = 0f; // Reset the current position
            }
        }

        private void ShowInsertedGameName()
        {
            // Instead of using GameSystem.ControlledSystem, use the GameSystem attached to this GameObject.
            if (gameSystem != null && gameSystem.Game != null && !string.IsNullOrEmpty(gameSystem.Game.path))
            {
                // Fixed: use GameSystem.Game.path rather than another system instance.
                string insertedGameName = FileNameHelper.GetFileName(gameSystem.Game.path);
                // Log out the safe file name for this system.
                logger.Debug("[" + gameObject.name + "] Inserted Game is: " + insertedGameName);
            }
            else
            {
                logger.Debug("[" + gameObject.name + "] Game system or its game path is null.");
            }
        }
        private void ShowControlledGameName()
        {
            // Instead of using GameSystem attached to this GameObject this is the overall ControlledSystem.   
            if (GameSystem.ControlledSystem != null && GameSystem.ControlledSystem.Game != null && !string.IsNullOrEmpty(GameSystem.ControlledSystem.Game.path))
            {
                string controlledGameName = FileNameHelper.GetFileName(GameSystem.ControlledSystem.Game.path);
                // Log out the safe file name for this system.
                logger.Debug("[" + gameObject.name + "] Current Controlled Game is: " + controlledGameName);
            }
            else
            {
                logger.Debug("[" + gameObject.name + "] Controlled Game System or its game path is null.");
            }
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
    }
}
