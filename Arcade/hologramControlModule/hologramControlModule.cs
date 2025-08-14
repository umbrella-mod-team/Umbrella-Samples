using UnityEngine;
using System.Reflection;
using WIGU;
using System.Linq;
using System.IO;

namespace WIGUx.Modules.hologramControlModule
{
    [RequireComponent(typeof(ScreenController))]
    public class hologramVideoController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();
        [Header("Rom Check")]
        private bool inFocusMode = false;  // Flag to track focus mode state
        private GameSystem gameSystem;  // Cached GameSystem for this cabinet.
        private string insertedGameName = string.Empty;
        private string controlledGameName = string.Empty;
        private Renderer screenRenderer;
        private Texture defaultMainTexture;
        private Texture originalEmissionMap;
        private Shader originalShader;
        private Shader holoShader;
        private Transform HologramObject;
        private Transform screenObject;
        private GameSystemState systemState; //systemstate

        void Awake()
        {
            // Find "screen_mesh 5" in children
            screenObject = GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "screen_mesh 5");
            if (screenObject == null)
            {
                logger.Debug($"{gameObject.name}: 'screen_mesh 5' not found");
                return;
            }
            logger.Debug($"{gameObject.name}: '{screenObject.name}' found in Awake");

            screenRenderer = screenObject.GetComponent<Renderer>();
            if (screenRenderer == null)
            {
                logger.Debug($"{gameObject.name}: Renderer not found on 'screen_mesh 5'");
                return;
            }

            // Find HologramObject and extract shader
            HologramObject = GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Hologram");
            if (HologramObject == null)
            {
                logger.Debug($"{gameObject.name}: 'Hologram' not found");
                return;
            }
            logger.Debug($"{gameObject.name}: '{HologramObject.name}' found in Awake");

            var holoRend = HologramObject.GetComponent<Renderer>();
            if (holoRend == null || holoRend.sharedMaterial == null)
            {
                logger.Debug($"{gameObject.name}: HologramObject has no Renderer or Material");
                return;
            }
            // DEBUG: print material and shader names
            string matName = holoRend.sharedMaterial.name;
            string shName = holoRend.sharedMaterial.shader != null ? holoRend.sharedMaterial.shader.name : "<null>";
            logger.Debug($"HologramObject material: {matName}, shader: {shName}");

            holoShader = holoRend.sharedMaterial.shader;
            // Fallback: if pulled shader is Standard or null, attempt Shader.Find
            if (holoShader == null || holoShader.name == "Standard")
            {
                holoShader = Shader.Find("Custom/Hologram");
                logger.Debug($"Fallback-loaded Hologram shader: {(holoShader != null ? holoShader.name : "<null>")}");
            }
            logger.Debug($"Using holoShader: {(holoShader != null ? holoShader.name : "<null>")}");
            if (holoShader == null || screenRenderer == null) return;

            // Operate directly on the shared material to avoid instancing
            var sharedMat = screenRenderer.sharedMaterial;
            // Reassign the dynamic video texture slots
            if (originalEmissionMap != null)
            {
                sharedMat.mainTexture = originalEmissionMap;
                sharedMat.SetTexture("_EmissionMap", originalEmissionMap);
            }

            // Swap shader in place
            sharedMat.shader = holoShader;
            sharedMat.SetFloat("_KeyThreshold", 0.1f);
            sharedMat.SetFloat("_Opacity", 0.7f);

            // DEBUG: confirm shader
            string appliedName = sharedMat.shader != null ? sharedMat.shader.name : "<null>";
            logger.Debug($"Applied shader now: {appliedName}");
            logger.Debug($"[HoloCtrl] Applied shader: {appliedName}");
        }

        void Start()
        {
            // Cache original shader and textures from the shared material
            if (screenRenderer != null && screenRenderer.sharedMaterial != null)
            {
                var sharedMat = screenRenderer.sharedMaterial;
                originalShader = sharedMat.shader;
                defaultMainTexture = sharedMat.mainTexture;
                originalEmissionMap = sharedMat.GetTexture("_EmissionMap");
                logger.Debug("Original shader cached: " + originalShader.name);
            }
        }
   
        // Helper: build full transform path of a child
        private static string GetTransformPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

    }
}
