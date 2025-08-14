using UnityEngine;
using System.Reflection;
using WIGU;
using System.Linq;
using System.IO;

namespace WIGUx.Modules.matteScreenControlModule
{
    [RequireComponent(typeof(ScreenController))]
    public class matteScreenVideoController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private Renderer screenRenderer;
        private Texture defaultMainTexture;
        private Texture originalEmissionMap;
        private Shader originalShader;
        private Shader matteShader;
        private Transform MatteObject;
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

            // Find MatteObject and extract shader
            MatteObject = GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Matte");
            if (MatteObject == null)
            {
                logger.Debug($"{gameObject.name}: 'Matte' not found");
                return;
            }
            logger.Debug($"{gameObject.name}: '{MatteObject.name}' found in Awake");

            var matteRend = MatteObject.GetComponent<Renderer>();
            if (matteRend == null || matteRend.sharedMaterial == null)
            {
                logger.Debug($"{gameObject.name}: MatteObject has no Renderer or Material");
                return;
            }
            // DEBUG: print material and shader names
            string matName = matteRend.sharedMaterial.name;
            string shName = matteRend.sharedMaterial.shader != null ? matteRend.sharedMaterial.shader.name : "<null>";
            logger.Debug($"MatteObject material: {matName}, shader: {shName}");

            matteShader = matteRend.sharedMaterial.shader;
            // Fallback: if shader is Standard or null, attempt Shader.Find("Custom/MatteScreen_NoEmission")
            if (matteShader == null || matteShader.name == "Standard")
            {
                matteShader = Shader.Find("Custom/MatteScreen");
                logger.Debug($"Fallback-loaded Matte shader: {(matteShader != null ? matteShader.name : "<null>")}");
            }
            logger.Debug($"Using matteShader: {(matteShader != null ? matteShader.name : "<null>")}");
            if (matteShader == null || screenRenderer == null) return;

            // Operate directly on the shared material to avoid instancing
            var sharedMat = screenRenderer.sharedMaterial;
            // Reassign the dynamic video texture slots
            if (originalEmissionMap != null)
            {
                sharedMat.mainTexture = originalEmissionMap;
                sharedMat.SetTexture("_EmissionMap", originalEmissionMap);
            }

            // Swap shader in place
            sharedMat.shader = matteShader;
            // No special float properties needed for matte
            // DEBUG: confirm shader
            string appliedName = sharedMat.shader != null ? sharedMat.shader.name : "<null>";
            logger.Debug($"Applied shader now: {appliedName}");
            logger.Debug($"[MatteCtrl] Applied shader: {appliedName}");
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
