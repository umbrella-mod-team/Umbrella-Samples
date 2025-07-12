using System;
using System.IO;
using UnityEngine;
using WIGU;
using System.Collections;
using UnityEngine.Rendering;

namespace WIGUx.Modules.refectionProbeModule
{
    public class refectionProbeController : MonoBehaviour
    {
        static IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        //   private JObject saveJson;
        private float probeRefreshTimer = 0f;
        private int refreshModeIndex = 0;
        private readonly float[] refreshIntervals = { 30.0f, 10.0f, 3.0f, 0.5f, 0f }; // 0f = every frame
        private int probeCycleIndex = 0;
        private ReflectionProbe[] sceneProbes = null;
        private GameObject playerCamera;   // Reference to the Player Camera
        private GameObject playerVRSetup;   // Reference to the VR Camera
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
                //  logger.Debug($"[ReflectionProbe] Activated probe: {probe.gameObject.name} in scene: {probe.gameObject.scene.name}");
            }
        }
        void Update()
        {
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
                    // You can add debug here if you want
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
    }
}
