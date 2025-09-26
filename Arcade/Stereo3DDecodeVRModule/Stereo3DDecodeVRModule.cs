// =============================
// Name: Stereo3DDecodeVRModule (one script to drive the universal shader)
// Targets ALL children whose name starts with "screen_mesh" and writes shader params.
//
// HOTKEYS:
//   Home+~   → OFF           (_Layout=0)  | passthrough 2D
//   Home+1   → SBS           (_Layout=1)  | press again while in SBS → MONO toggle (3D off/on)
//   Home+2   → OU            (_Layout=2)  | press again while in OU  → MONO toggle (3D off/on)
//   Home+3   → Checkerboard  (_Layout=3)  | press again while in CB  → MONO toggle (3D off/on)
//   Home+4   → Interleave    (_Layout=4)  | press again while in INT → toggle Rows/Cols/Mono
//   Home+5   → Lenticular    (_Layout=5)  | press again while in Lin → MONO toggle (3D off/on)
//   Home+6   → Cycle SourceRows mode: Auto → 1080 → 720 → (repeat)     
//   Home+7   → Swap eyes (_Swap)
//   Home+8   → Left eye odd (_LeftOdd)
//
//   Lenticular-specific (NUMPAD):
//   Home+Num1 → Adjust Lenticular Pitch (−/+)               // uses Shift for + (increase)
//   Home+Num2 → Adjust Lenticular Slant (−/+)               // uses Shift for + (increase)
//   Home+Num3 → Adjust Lenticular Phase (−/+)               // uses Shift for + (increase)
//   Home+Num4 → Adjust Lenticular View Count (cycle)        // cycles common values (8/9/16)
//   Home+Num5 → Adjust Lenticular Center/Pair (ViewA/ViewB) // Shift modifies ViewB; no Shift modifies ViewA
//   Home+Num6 → Toggle Lenticular RGB Order (RGB↔BGR)
//   Home+Num7 → Toggle Row Phase   (_RowPhase)             
//   Home+Num8 → Toggle Column Phase (_ColPhase)      
//   Home+Left/Right → Stereo separation −/+ (pixels)
//   Home+Up/Down    → Change row thickness (_RowGroup) 1→2→3… (Interleaved Rows)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace WIGUx.Modules.Stereo3DDecodeVRModule
{
    public enum UniversalLayout
    {
        Off = 0,
        SBS = 1,
        OU = 2,
        Checkerboard = 3,
        Interleaved = 4,
        Lenticular = 5
    }
    public enum InterleaveMode { Rows = 0, Columns = 1 }
    public enum SourceRowsMode { Auto = 0, Manual1080 = 1, Manual720 = 2 }

    [RequireComponent(typeof(ScreenController))]
    public class Stereo3DDecodeVRScreenController : MonoBehaviour
    {
        static WIGU.IWiguLogger logger = WIGU.ServiceProvider.Instance.GetService<WIGU.IWiguLogger>();

        [Header("Targeting")]
        public string MeshNamePrefix = "screen_mesh"; // targets all screen meshes

        [Header("Layout")]
        public UniversalLayout Layout = UniversalLayout.Off;
        public InterleaveMode Mode = InterleaveMode.Rows; // used when Layout=Interleaved

        [Header("Options")]
        public bool SwapEyes = false;      // _Swap
        public bool LeftUsesOdd = true;    // _LeftOdd
        public int RowGroup = 1;           // _RowGroup (row thickness)
        public bool ForceMono = false;     // _Mono (force mono for the active layout)
        public bool RowPhase = false;      // _RowPhase (rows)
        public bool ColPhase = false;      // _ColPhase (columns)
        [Range(0, 128)] public float SeparationPixels = 0f; // _SepPixels

        [Header("Source Size Control")]
        public SourceRowsMode RowsMode = SourceRowsMode.Auto; // Auto → 1080 → 720 cycling
        public int SourceRows = 1080;                         // active value (derived by mode or texture)
        public int SourceCols = 1920;                         // active value (derived by mode or texture)

        // ============================
        // Lenticular (Autostereo) Params (NEW)
        // ============================
        [Header("Lenticular Decode (NEW)")]
        public int Lin_Views = 9;
        public float Lin_PitchPx = 192.0f;
        public float Lin_SlantPerRow = 0.0f;
        public float Lin_Phase = 0.05f;
        public bool Lin_BGR = false;
        public int Lin_ViewA = 4;
        public int Lin_ViewB = 5;
        public int Lin_AdjustStepInt = 1;
        public float Lin_AdjustStep = 0.02f;
        public float Lin_AdjustBigStep = 0.10f;

        private readonly List<Material> _mats = new List<Material>();
        private Shader _shader;
        private ScreenReceiver _sr;
        private PropertyInfo _piTexture;

        void Awake()
        {
            _sr = GetComponent<ScreenReceiver>();
            if (_sr != null)
            {
                var srType = _sr.GetType();
                _piTexture = srType.GetProperty("Texture", BindingFlags.Instance | BindingFlags.Public);
            }

            var sc = GetComponent<ScreenController>();
            string screens = "<none>";
            try
            {
                if (sc != null && sc.screens != null)
                {
                    var names = sc.screens.Where(s => s != null).Select(s => s.name).ToArray();
                    screens = names.Length > 0 ? string.Join(", ", names) : "<none>";
                }
            }
            catch { }
            logger?.Debug("[Stereo3D] Awake on '" + name + "' SC=" + (sc != null) + " SR=" + (_sr != null) + " screens=[" + screens + "] prefix='" + MeshNamePrefix + "'");

            var cacheT = transform.Find("3D");
            if (cacheT != null)
            {
                var cacheR = cacheT.GetComponent<Renderer>();
                if (cacheR != null && cacheR.sharedMaterials != null)
                {
                    foreach (var m in cacheR.sharedMaterials)
                    {
                        if (m != null && m.shader != null && m.shader.name == "Custom/Stereo3DDecodeVR")
                        { _shader = m.shader; break; }
                    }
                    logger?.Info("[Stereo3D] Shader cache on '3D' → " + (_shader != null ? _shader.name : "<none>"));
                }
                else logger?.Warning("[Stereo3D] Child '3D' has no Renderer.");
            }
            else logger?.Warning("[Stereo3D] Cache child '3D' NOT FOUND under '" + name + "'.");

            if (_shader == null)
            { logger?.Error("[Stereo3D] Cache shader not found on child '3D'."); enabled = false; return; }

            var targeted = new List<string>();
            foreach (var tr in GetComponentsInChildren<Transform>(true))
            {
                if (!string.IsNullOrEmpty(tr.name) && tr.name.StartsWith(MeshNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var r = tr.GetComponent<Renderer>();
                    if (r != null && r.sharedMaterial != null)
                    {
                        var mat = r.sharedMaterial;
                        mat.shader = _shader;
                        _mats.Add(mat);
                        targeted.Add(tr.name);
                    }
                }
            }
            if (_mats.Count == 0)
            {
                logger?.Warning("[Stereo3D] No meshes with prefix '" + MeshNamePrefix + "'.");
                enabled = false; return;
            }
            else
            {
                logger?.Info("[Stereo3D] Assigned shader to " + _mats.Count + " material(s): " + string.Join(", ", targeted.ToArray()));
            }

            if (_mats.Count > 0)
            {
                var mat0 = _mats[0];
                Layout = (UniversalLayout)mat0.GetFloat("_Layout");
                Mode = (InterleaveMode)mat0.GetFloat("_Mode");
                SwapEyes = mat0.GetFloat("_Swap") > 0.5f;
                LeftUsesOdd = mat0.GetFloat("_LeftOdd") > 0.5f;
                SourceRows = (int)mat0.GetFloat("_SourceRows");
                SourceCols = (int)mat0.GetFloat("_SourceCols");
                RowGroup = (int)mat0.GetFloat("_RowGroup");
                RowPhase = mat0.GetFloat("_RowPhase") > 0.5f;
                ColPhase = mat0.GetFloat("_ColPhase") > 0.5f;
                ForceMono = mat0.GetFloat("_Mono") > 0.5f;
                SeparationPixels = mat0.GetFloat("_SepPixels");

                Lin_Views = (int)mat0.GetFloat("_Views");
                Lin_PitchPx = mat0.GetFloat("_PitchPx");
                Lin_SlantPerRow = mat0.GetFloat("_SlantPxPerRow");
                Lin_Phase = mat0.GetFloat("_Phase");
                Lin_BGR = mat0.GetFloat("_RGBOrder") > 0.5f;
                Lin_ViewA = (int)mat0.GetFloat("_ViewA");
                Lin_ViewB = (int)mat0.GetFloat("_ViewB");
            }
        }

        bool HomePlus(params KeyCode[] any)
        {
            if (!Input.GetKey(KeyCode.Home)) return false;
            foreach (var k in any) if (Input.GetKeyDown(k)) return true;
            return false;
        }
        bool ShiftHeld() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        void Update()
        {
            bool changed = false;

            if (HomePlus(KeyCode.Alpha1))
            {
                if (Layout == UniversalLayout.SBS) { ForceMono = !ForceMono; Log("SBS Mono=" + ForceMono); }
                else { Layout = UniversalLayout.SBS; ForceMono = false; Log("Layout=SBS (3D on)"); }
                changed = true;
            }
            if (HomePlus(KeyCode.Alpha2))
            {
                if (Layout == UniversalLayout.OU) { ForceMono = !ForceMono; Log("OU Mono=" + ForceMono); }
                else { Layout = UniversalLayout.OU; ForceMono = false; Log("Layout=OU (3D on)"); }
                changed = true;
            }
            if (HomePlus(KeyCode.Alpha3))
            {
                if (Layout == UniversalLayout.Checkerboard) { ForceMono = !ForceMono; Log("Checkerboard Mono=" + ForceMono); }
                else { Layout = UniversalLayout.Checkerboard; ForceMono = false; Log("Layout=Checkerboard (3D on)"); }
                changed = true;
            }
            if (HomePlus(KeyCode.Alpha4))
            {
                if (Layout != UniversalLayout.Interleaved)
                {
                    Layout = UniversalLayout.Interleaved;
                    Mode = InterleaveMode.Rows;
                    ForceMono = false;
                    Log("Layout=Interleaved Rows (3D on)");
                }
                else
                {
                    if (!ForceMono && Mode == InterleaveMode.Rows)
                    {
                        Mode = InterleaveMode.Columns;
                        Log("Layout=Interleaved Columns (3D on)");
                    }
                    else if (!ForceMono && Mode == InterleaveMode.Columns)
                    {
                        ForceMono = true;
                        Log("Layout=Interleaved Mono");
                    }
                    else
                    {
                        Mode = InterleaveMode.Rows;
                        ForceMono = false;
                        Log("Layout=Interleaved Rows (3D on)");
                    }
                }
                changed = true;
            }
            if (HomePlus(KeyCode.Alpha5))
            {
                if (Layout == UniversalLayout.Lenticular) { ForceMono = !ForceMono; Log("Lenticular Mono=" + ForceMono); }
                else { Layout = UniversalLayout.Lenticular; ForceMono = false; Log("Layout=Lenticular (3D on)"); }
                changed = true;
            }

            if (HomePlus(KeyCode.Alpha6)) { CycleRowsMode(); changed = true; }
            if (HomePlus(KeyCode.Alpha7)) { SwapEyes = !SwapEyes; changed = true; Log("SwapEyes=" + SwapEyes); }
            if (HomePlus(KeyCode.Alpha8)) { LeftUsesOdd = !LeftUsesOdd; changed = true; Log("LeftUsesOdd=" + LeftUsesOdd); }

            if (HomePlus(KeyCode.UpArrow)) { RowGroup = Mathf.Clamp(RowGroup + 1, 1, 32); changed = true; Log("RowGroup=" + RowGroup); }
            if (HomePlus(KeyCode.DownArrow)) { RowGroup = Mathf.Clamp(RowGroup - 1, 1, 32); changed = true; Log("RowGroup=" + RowGroup); }

            if (HomePlus(KeyCode.LeftArrow)) { SeparationPixels = Mathf.Max(0f, SeparationPixels - 1f); changed = true; Log("SeparationPixels=" + SeparationPixels); }
            if (HomePlus(KeyCode.RightArrow)) { SeparationPixels = Mathf.Min(128f, SeparationPixels + 1f); changed = true; Log("SeparationPixels=" + SeparationPixels); }

            // Lenticular NUMPAD controls
            if (HomePlus(KeyCode.Keypad1))
            {
                float step = ShiftHeld() ? Lin_AdjustBigStep : Lin_AdjustStep;
                Lin_PitchPx = Mathf.Max(0.01f, Lin_PitchPx + (ShiftHeld() ? +step : -step));
                changed = true; Log("Lin_PitchPx=" + Lin_PitchPx.ToString("F4"));
            }
            if (HomePlus(KeyCode.Keypad2))
            {
                float step = ShiftHeld() ? Lin_AdjustBigStep : Lin_AdjustStep;
                Lin_SlantPerRow = Lin_SlantPerRow + (ShiftHeld() ? +step : -step);
                changed = true; Log("Lin_SlantPerRow=" + Lin_SlantPerRow.ToString("F4"));
            }
            if (HomePlus(KeyCode.Keypad3))
            {
                float step = ShiftHeld() ? Lin_AdjustBigStep : Lin_AdjustStep;
                Lin_Phase = Mathf.Repeat(Lin_Phase + (ShiftHeld() ? +step : -step), 1.0f);
                changed = true; Log("Lin_Phase=" + Lin_Phase.ToString("F4"));
            }
            if (HomePlus(KeyCode.Keypad4))
            {
                Lin_Views = (Lin_Views == 8) ? 9 : (Lin_Views == 9 ? 16 : 8);
                Lin_ViewA = Mathf.Clamp(Lin_ViewA, 0, Mathf.Max(0, Lin_Views - 1));
                Lin_ViewB = Mathf.Clamp(Lin_ViewB, 0, Mathf.Max(0, Lin_Views - 1));
                changed = true; Log("Lin_Views=" + Lin_Views);
            }
            if (HomePlus(KeyCode.Keypad5))
            {
                if (!ShiftHeld())
                {
                    Lin_ViewA = Mathf.Clamp(Lin_ViewA + Lin_AdjustStepInt, 0, Mathf.Max(0, Lin_Views - 1));
                    Log("Lin_ViewA=" + Lin_ViewA);
                }
                else
                {
                    Lin_ViewB = Mathf.Clamp(Lin_ViewB + Lin_AdjustStepInt, 0, Mathf.Max(0, Lin_Views - 1));
                    Log("Lin_ViewB=" + Lin_ViewB);
                }
                changed = true;
            }
            if (HomePlus(KeyCode.Keypad6)) { Lin_BGR = !Lin_BGR; changed = true; Log("Lin_RGBOrder=" + (Lin_BGR ? "BGR" : "RGB")); }
            if (HomePlus(KeyCode.Keypad7)) { RowPhase = !RowPhase; changed = true; Log("RowPhase=" + RowPhase); }
            if (HomePlus(KeyCode.Keypad8)) { ColPhase = !ColPhase; changed = true; Log("ColPhase=" + ColPhase); }

            if (changed) ApplyParams();
        }

        void CycleRowsMode()
        {
            RowsMode = (SourceRowsMode)(((int)RowsMode + 1) % 3);
            switch (RowsMode)
            {
                case SourceRowsMode.Auto: Log("RowsMode=Auto (Cols=Auto)"); break;
                case SourceRowsMode.Manual1080: SourceRows = 1080; SourceCols = 1920; Log("RowsMode=1080p (1920x1080)"); break;
                case SourceRowsMode.Manual720: SourceRows = 720; SourceCols = 1280; Log("RowsMode=720p (1280x720)"); break;
            }
        }
        void ApplyParams()
        {
            int capturedRows = SourceRows;
            int capturedCols = SourceCols;
            Texture texObj = null;
            if (_sr != null && _piTexture != null)
            {
                texObj = _piTexture.GetValue(_sr, null) as Texture;
                var tex2D = texObj as Texture2D;
                var rt = texObj as RenderTexture;
                int texH = 0, texW = 0;
                if (tex2D != null) { texH = tex2D.height; texW = tex2D.width; }
                if (rt != null) { texH = rt.height; texW = rt.width; }
                if (RowsMode == SourceRowsMode.Auto && texH > 0) SourceRows = texH;
                if (RowsMode == SourceRowsMode.Auto && texW > 0) SourceCols = texW;
                if (texH > 0) capturedRows = texH;
                if (texW > 0) capturedCols = texW;
            }

            foreach (var m in _mats)
            {
                if (m == null) continue;

                var emTex = m.GetTexture("_EmissionMap");
                if (emTex != null)
                {
                    emTex.filterMode = FilterMode.Point;
                    emTex.wrapMode = TextureWrapMode.Clamp;
                    emTex.anisoLevel = 0;
                }

                m.SetFloat("_Layout", (float)Layout);
                m.SetFloat("_Mode", (float)Mode);
                m.SetFloat("_Swap", SwapEyes ? 1f : 0f);
                m.SetFloat("_LeftOdd", LeftUsesOdd ? 1f : 0f);
                m.SetFloat("_SourceRows", Mathf.Max(2, SourceRows));
                m.SetFloat("_CapturedRows", Mathf.Max(2, capturedRows));
                m.SetFloat("_SourceCols", Mathf.Max(2, SourceCols));
                m.SetFloat("_CapturedCols", Mathf.Max(2, capturedCols));
                m.SetFloat("_RowGroup", Mathf.Max(1, RowGroup));
                m.SetFloat("_RowPhase", RowPhase ? 1f : 0f);
                m.SetFloat("_ColPhase", ColPhase ? 1f : 0f);
                m.SetFloat("_Mono", ForceMono ? 1f : 0f);
                m.SetFloat("_SepPixels", SeparationPixels);

                // Lenticular uniforms
                m.SetFloat("_Views", Mathf.Max(1, Lin_Views));
                m.SetFloat("_PitchPx", Mathf.Max(0.001f, Lin_PitchPx));
                m.SetFloat("_SlantPxPerRow", Lin_SlantPerRow);
                m.SetFloat("_Phase", Mathf.Repeat(Lin_Phase, 1f));
                m.SetFloat("_RGBOrder", Lin_BGR ? 1f : 0f);
                m.SetFloat("_ViewA", Mathf.Clamp(Lin_ViewA, 0, Mathf.Max(0, Lin_Views - 1)));
                m.SetFloat("_ViewB", Mathf.Clamp(Lin_ViewB, 0, Mathf.Max(0, Lin_Views - 1)));

                // Checkerboard uniforms (block size + mode)
                m.SetFloat("_CB_BlockW", 1); // default 1x1 unless you add UI/keys for block control
                m.SetFloat("_CB_BlockH", 1);
                m.SetFloat("_CB_Mode", 1);   // 0 = pixel parity, 1 = block parity
            }
        }

        void Log(string msg) { logger?.Info("[Stereo3D] " + msg); }
    }
}
