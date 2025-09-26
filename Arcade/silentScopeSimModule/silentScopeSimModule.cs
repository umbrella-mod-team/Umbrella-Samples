using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace WIGUx.Modules.silentScopeSimModule
{
    public class HalfScreenAimMask : MonoBehaviour
    {
        [Tooltip("Only adjust when core name contains this (case-insensitive). Leave blank to always adjust when attached.")]
        public string coreSubstringFilter = "mame";

        [Tooltip("Clamp final X to the left half (0..0.5) after scaling.")]
        public bool clampLeftHalf = true;

        [Range(0f, 1f), Tooltip("Scale factor applied to X before optional clamp. 0.5 maps full width to left half.")]
        public float xScale = 0.5f;

        [Header("Debug")] public bool verbose = false;

        private Component retroarch; // the Retroarch component for this screen

        private void Awake()
        {
            var sc = FindComponentByTypeNameInHierarchy(gameObject, "ScreenController");
            if (sc == null)
            {
                Debug.LogWarning($"[HalfScreenAimMask] No ScreenController found under {name}.");
                return;
            }

            retroarch = GetFieldOrProp<Component>(sc, "retroarch");
            if (retroarch == null)
            {
                Debug.LogWarning($"[HalfScreenAimMask] ScreenController found but no Retroarch reference on {name}.");
                return;
            }

            AimMaskManager.Register(retroarch, this);
            if (verbose)
                Debug.Log($"[HalfScreenAimMask] Registered Retroarch instance on {name}.");
        }

        private void OnDestroy()
        {
            if (retroarch != null)
                AimMaskManager.Unregister(retroarch);
        }

        internal bool ShouldAdjustForCore(string core)
        {
            if (string.IsNullOrEmpty(coreSubstringFilter)) return true;
            if (string.IsNullOrEmpty(core)) return false;
            return core.IndexOf(coreSubstringFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal Vector2 Adjust(Vector2 uv)
        {
            float x = uv.x * xScale;
            if (clampLeftHalf)
                x = Mathf.Clamp(x, 0.0f, 0.5f);
            return new Vector2(x, uv.y);
        }

        private static Component FindComponentByTypeNameInHierarchy(GameObject root, string typeName)
        {
            var q = new Queue<Transform>();
            q.Enqueue(root.transform);
            while (q.Count > 0)
            {
                var t = q.Dequeue();
                var comps = t.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] != null && comps[i].GetType().Name == typeName)
                        return comps[i];
                }
                for (int i = 0; i < t.childCount; i++) q.Enqueue(t.GetChild(i));
            }
            return null;
        }

        private static T GetFieldOrProp<T>(object target, string name) where T : class
        {
            if (target == null) return null;
            var tp = target.GetType();
            var f = tp.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null) return f.GetValue(target) as T;
            var p = tp.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p != null) return p.GetValue(target, null) as T;
            return null;
        }
    }

    public static class AimMaskManager
    {
        internal static readonly Dictionary<UnityEngine.Object, HalfScreenAimMask> ActiveMasks = new Dictionary<UnityEngine.Object, HalfScreenAimMask>();

        private static readonly Regex GunCmdRegex = new Regex(
            @"^(?<prefix>VRCMD\s+GUN(?:\s+AIM)?\s+)(?<x>-?\d+(?:\.\d+)?)(\s+)(?<y>-?\d+(?:\.\d+)?)(?<suffix>.*)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static void Register(Component retro, HalfScreenAimMask mask)
        {
            if (retro == null || mask == null) return;
            ActiveMasks[retro] = mask;

            var relay = (retro as Component).gameObject.GetComponent<RetroarchCommandRelay>();
            if (relay == null)
                relay = (retro as Component).gameObject.AddComponent<RetroarchCommandRelay>();
            relay.TargetRetroarch = retro;
        }

        public static void Unregister(Component retro)
        {
            if (retro == null) return;
            ActiveMasks.Remove(retro);
        }

        internal static string MaybeAdjustCommand(object retroarch, string cmd)
        {
            if (!ActiveMasks.TryGetValue(retroarch as UnityEngine.Object, out var mask) || mask == null)
                return cmd;

            string core = null;
            try
            {
                var coreField = retroarch.GetType().GetField("game", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var gameObj = (coreField != null) ? coreField.GetValue(retroarch) : null;
                if (gameObj != null)
                {
                    var coreProp = gameObj.GetType().GetField("core", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (coreProp != null)
                        core = coreProp.GetValue(gameObj) as string;
                }
            }
            catch { }

            if (!mask.ShouldAdjustForCore(core)) return cmd;

            var m = GunCmdRegex.Match(cmd);
            if (!m.Success) return cmd;

            if (!float.TryParse(m.Groups["x"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                !float.TryParse(m.Groups["y"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                return cmd;

            if (x < 0f || y < 0f) return cmd;

            Vector2 adjusted = mask.Adjust(new Vector2(x, y));

            string rebuilt = m.Groups["prefix"].Value
                           + adjusted.x.ToString("0.####", CultureInfo.InvariantCulture)
                           + m.Groups[3].Value
                           + adjusted.y.ToString("0.####", CultureInfo.InvariantCulture)
                           + m.Groups["suffix"].Value;

            if (mask.verbose)
                Debug.Log($"[HalfScreenAimFix] {m.Value} → {rebuilt}");

            return rebuilt;
        }
    }

    public class RetroarchCommandRelay : MonoBehaviour
    {
        public Component TargetRetroarch;

        public void SendCommand(string cmd)
        {
            string adjusted = AimMaskManager.MaybeAdjustCommand(TargetRetroarch, cmd);

            var tp = TargetRetroarch.GetType();
            var sendCmd = tp.GetMethod("SendCommand", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (sendCmd != null)
                sendCmd.Invoke(TargetRetroarch, new object[] { adjusted });
        }
    }
}