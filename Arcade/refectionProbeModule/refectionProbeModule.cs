using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using WIGU;
using System.Collections;
using UnityEngine.Rendering;

namespace WIGUx.Modules.refectionProbeModule
{
    public class refectionProbeController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        private float probeRefreshTimer = 0f;
        private int refreshModeIndex = 0;
        private readonly float[] refreshIntervals = { 30.0f, 10.0f, 3.0f, 0.5f, 0f }; // 0f = every frame
        private int probeCycleIndex = 0;
        private ReflectionProbe[] sceneProbes = null;
        private Coroutine staggerCoroutine = null;

        void Start()
        {
            // Remove unwanted scene probes
            var outsideProbeGO = GameObject.Find("/Walls/Reflection Probe (Outside)");
            if (outsideProbeGO != null)
                Destroy(outsideProbeGO);

            var reflectionProbeGO = GameObject.Find("/Walls/Reflection Probe");
            if (reflectionProbeGO != null)
                Destroy(reflectionProbeGO);

            // Find the existing ReflectionProbe in UGC and activate it
            var localProbes = transform.GetComponentsInChildren<ReflectionProbe>(true);

            foreach (var probe in localProbes)
            {
                probe.RenderProbe();
            }
        }

        void Update()
        {

            // ----- KEYBINDS -----
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (shift && Input.GetKeyDown(KeyCode.P))
            {
                PrintPlayerPositionAndRotation();
            }
            /*           
            if (shift && Input.GetKeyDown(KeyCode.O))
            {
                ResetPlayerHeight();
            }
            if (shift && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                ResetPlayerToSpawn();
            }
            */
            // Handle cycling refresh level
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                refreshModeIndex = (refreshModeIndex + 1) % refreshIntervals.Length;
                probeRefreshTimer = 0f;

                // Cancel previous stagger if running
                if (staggerCoroutine != null)
                {
                    StopCoroutine(staggerCoroutine);
                    staggerCoroutine = null;
                }
                if (refreshIntervals[refreshModeIndex] == 0f)
                    logger.Debug("Reflection Refresh set to Realtime");
                else if (refreshIntervals[refreshModeIndex] == 1f)
                    logger.Debug("Reflection Refresh set to 1 Second");
                else
                    logger.Debug($"Reflection Refresh set to {refreshIntervals[refreshModeIndex]} Seconds");
            }

            // Get/refresh the probes array if needed
            if (sceneProbes == null)
                sceneProbes = transform.GetComponentsInChildren<ReflectionProbe>(true);

            float interval = refreshIntervals[refreshModeIndex];

            if (interval == 0f) // Realtime (every frame)
            {
                foreach (var probe in sceneProbes)
                {
                    if (probe != null && probe.enabled && probe.gameObject.activeInHierarchy)
                        probe.RenderProbe();
                }
            }
            else if (interval > 0f)
            {
                probeRefreshTimer += Time.deltaTime;
                if (probeRefreshTimer >= interval)
                {
                    probeRefreshTimer = 0f;
                    // Stagger all probes over the interval
                    if (sceneProbes != null && sceneProbes.Length > 0)
                    {
                        float delay = interval / sceneProbes.Length;
                        if (staggerCoroutine != null)
                            StopCoroutine(staggerCoroutine);
                        staggerCoroutine = StartCoroutine(RefreshProbesStaggered(delay));
                    }
                }
            }
        }

        private static readonly string[] refreshModeLabels =
        {
            "30s staggered",
            "10s staggered",
            "3s staggered",
            "0.5s staggered",
            "Realtime (every frame)"
        };

        private IEnumerator RefreshProbesStaggered(float delayBetween)
        {
            if (sceneProbes == null)
                sceneProbes = transform.GetComponentsInChildren<ReflectionProbe>(true);

            foreach (var probe in sceneProbes)
            {
                if (probe != null && probe.enabled && probe.gameObject.activeInHierarchy)
                {
                    probe.RenderProbe();
                }
                if (delayBetween > 0f)
                    yield return new WaitForSeconds(delayBetween);
            }
            staggerCoroutine = null;
        }

        string GetEmuVRRoot()
        {
            var dataPath = Application.dataPath;
            var root = Path.GetDirectoryName(dataPath);
            return root;
        }

        // ---- Player debug hotkeys ----

        // Utility: Find type in any loaded assembly by simple name
        private static Type FindType(string typeName) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == typeName);

        // Utility: Find static or instance field, public or nonpublic
        private static FieldInfo FindField(Type t, string name) =>
            t?.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        // Utility: Find static or instance property, public or nonpublic
        private static PropertyInfo FindProp(Type t, string name) =>
            t?.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        // Utility: Find static or instance method, public or nonpublic
        private static MethodInfo FindMethod(Type t, string name) =>
            t?.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        // Utility: Find static field value by type+field name
        private static object GetStaticFieldValue(Type t, string fieldName) =>
            FindField(t, fieldName)?.GetValue(null);

        // Utility: Find static property value by type+prop name
        private static object GetStaticPropValue(Type t, string propName) =>
            FindProp(t, propName)?.GetValue(null);

        // 1. Print player global position and rotation to 4 decimals
        private void PrintPlayerPositionAndRotation()
        {
            Type tVR = FindType("PlayerVRSetup");
            Type tController = FindType("PlayerController");

            // Try all likely fields
            var playerRig = GetStaticFieldValue(tVR, "PlayerRig") as Transform;
            var playerCamera = GetStaticFieldValue(tVR, "PlayerCamera") as GameObject;
            var heightContainer = GetStaticFieldValue(tVR, "HeightContainer") as Transform;
            var instance = GetStaticFieldValue(tVR, "Instance") as MonoBehaviour;

            // Print everything we can find, with full precision
            if (instance != null)
            {
                var tr = instance.transform;
                logger.Debug($"Instance localPosition:({tr.localPosition.x:F4}, {tr.localPosition.y:F4}, {tr.localPosition.z:F4})");
                logger.Debug($"Instance localRotation:({tr.localRotation.x:F4}, {tr.localRotation.y:F4}, {tr.localRotation.z:F4}, {tr.localRotation.w:F4})");
            }
            if (playerRig != null)
            {
                logger.Debug($"PlayerRig localPosition:({playerRig.localPosition.x:F4}, {playerRig.localPosition.y:F4}, {playerRig.localPosition.z:F4})");
                logger.Debug($"PlayerRig localRotation:({playerRig.localRotation.x:F4}, {playerRig.localRotation.y:F4}, {playerRig.localRotation.z:F4}, {playerRig.localRotation.w:F4})");
                logger.Debug($"PlayerRig worldPosition:({playerRig.position.x:F4}, {playerRig.position.y:F4}, {playerRig.position.z:F4})");
                logger.Debug($"PlayerRig worldRotation:({playerRig.rotation.eulerAngles.x:F4}, {playerRig.rotation.eulerAngles.y:F4}, {playerRig.rotation.eulerAngles.z:F4})");
            }
            if (playerCamera != null)
            {
                var tr = playerCamera.transform;
                logger.Debug($"PlayerCamera localPosition:({tr.localPosition.x:F4}, {tr.localPosition.y:F4}, {tr.localPosition.z:F4})");
                logger.Debug($"PlayerCamera localRotation:({tr.localRotation.x:F4}, {tr.localRotation.y:F4}, {tr.localRotation.z:F4}, {tr.localRotation.w:F4})");
            }
            if (heightContainer != null)
            {
                logger.Debug($"HeightContainer localPosition:({heightContainer.localPosition.x:F4}, {heightContainer.localPosition.y:F4}, {heightContainer.localPosition.z:F4})");
            }

            // Print main world pos/rot for user
            if (playerRig != null)
            {
                Vector3 pos = playerRig.position;
                Vector3 rot = playerRig.rotation.eulerAngles;
                logger.Debug($"[PLAYER POS] {pos.x:F4}, {pos.y:F4}, {pos.z:F4} | ROT: {rot.x:F4}, {rot.y:F4}, {rot.z:F4}");
            }
            else if (instance != null)
            {
                Vector3 pos = instance.transform.position;
                Vector3 rot = instance.transform.rotation.eulerAngles;
                logger.Debug($"[PLAYER POS] {pos.x:F4}, {pos.y:F4}, {pos.z:F4} | ROT: {rot.x:F4}, {rot.y:F4}, {rot.z:F4}");
            }
            else
            {
                logger.Debug("[PLAYER POS] No player rig or instance found!");
            }
        }

        // 2. Reset player height (mimics HeightReset/Q+E logic)
        private void ResetPlayerHeight()
        {
            Type tController = FindType("PlayerController");
            Type tVR = FindType("PlayerVRSetup");
            var playerController = GameObject.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(mb => mb.GetType() == tController);
            var heightContainer = GetStaticFieldValue(tVR, "HeightContainer") as Transform;

            if (playerController != null && heightContainer != null)
            {
                var initialHeightField = FindField(tController, "initialHeight");
                float initialHeight = (float)(initialHeightField?.GetValue(null) ?? 0f);

                var heightDirectionField = FindField(tController, "heightDirection");
                float delta = initialHeight - heightContainer.localPosition.y;
                heightDirectionField?.SetValue(playerController, delta);

                var updateHeightMethod = FindMethod(tController, "UpdateHeight");
                updateHeightMethod?.Invoke(playerController, null);

                logger.Debug("[PLAYER HEIGHT] Height reset triggered.");
            }
            else
            {
                logger.Debug("[PLAYER HEIGHT] Could not find player controller or height container.");
            }
        }

        // 3. Reset player to spawn (default 0,0,0)
        private void ResetPlayerToSpawn()
        {
            Type tVR = FindType("PlayerVRSetup");
            var instance = GetStaticFieldValue(tVR, "Instance") as MonoBehaviour;
            var playerRig = GetStaticFieldValue(tVR, "PlayerRig") as Transform;
            var playerCamera = GetStaticFieldValue(tVR, "PlayerCamera") as GameObject;
            var heightContainer = GetStaticFieldValue(tVR, "HeightContainer") as Transform;

            if (instance != null)
                instance.transform.localPosition = Vector3.zero;
            if (playerRig != null)
            {
                playerRig.localPosition = Vector3.zero;
                playerRig.localRotation = Quaternion.identity;
            }
            if (playerCamera != null)
            {
                var tr = playerCamera.transform;
                tr.localPosition = Vector3.zero;
                tr.localRotation = Quaternion.identity;
            }
            if (heightContainer != null)
            {
                heightContainer.localPosition = new Vector3(heightContainer.localPosition.x, 0f, heightContainer.localPosition.z);
            }
            logger.Debug("[PLAYER SPAWN] Player teleported to (0,0,0)");
        }
    }
}