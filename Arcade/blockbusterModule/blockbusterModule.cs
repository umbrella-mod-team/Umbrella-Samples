using UnityEngine;
using UnityEngine.XR;
using WIGU;
using System.Collections.Generic;
using EmuVR.InputManager;
using System.Collections;

namespace WIGUx.Modules.blockbusterModule
{
    public class blockbusterModule : MonoBehaviour
    {
        // Fog settings
        public bool enableFog = true; // Default fog state
        public Color fogColor = Color.gray; // Fog color
        public float fogDensity = 0.01f; // Fog density (lower values = lighter fog)

        void Start()
        {
            // Initialize fog based on default settings
            ApplyFogSettings();
        }
        /// <summary>
        /// Toggles fog on or off.
        /// </summary>
        /// <param name="state">If true, enables fog. If false, disables fog.</param>
        /// 
        public void ToggleFog(bool state)
        {
            enableFog = state;
            RenderSettings.fog = enableFog;

            if (enableFog)
            {
                ApplyFogSettings();
            }
        }

        /// <summary>
        /// Sets the fog settings (color, density, etc.).
        /// </summary>
        /// 
        private void ApplyFogSettings()
        {
            if (enableFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential; // Change to FogMode.Linear if preferred
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogDensity;
            }
        }

        // For testing purposes, toggles fog on/off with the "F" key
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleFog(!enableFog);
            }
        }
    }
}