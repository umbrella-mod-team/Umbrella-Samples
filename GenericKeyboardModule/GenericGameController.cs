using UnityEngine;

namespace WIGU.Modules.GenericGameController
{
    public class GenericGameController : MonoBehaviour
    {
        // Reference to the animation component
        private Animation animationComponent;

        void Start()
        {
            // Get the animation component from the GameObject
            animationComponent = GetComponent<Animation>();

            // Check if the animation component was found
            if (animationComponent == null)
            {
                Debug.LogError("The animation component was not found on the GameObject.");
            }
        }

        void Update()
        {
            // Check for Oculus input to trigger animation
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickRight))
            {
                // Call the Play() method to start the "MoverPalancaDerecha" animation
                animationComponent.Play("MoveJoystickRight");
            }
            else if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickLeft))
            {
                // Call the Play() method to start the "MoverPalancaIzquierda" animation
                animationComponent.Play("MoveJoystickLeft");
            }
            else if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickUp))
            {
                // Call the Play() method to start the "MoverPalancaArriba" animation
                animationComponent.Play("MoveJoystickUp");
            }
            else if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickDown))
            {
                // Call the Play() method to start the "MoverPalancaAbajo" animation
                animationComponent.Play("MoveJoystickDown");
            }
        }
    }
}
