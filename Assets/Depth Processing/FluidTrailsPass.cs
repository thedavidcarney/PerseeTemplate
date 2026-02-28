using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class FluidTrailsPass : StylePass
    {
        private ComputeShader compute;
        private int kernelInject;
        private int kernelAdvectVel;
        private int kernelAdvectDye;
        private int kernelDiffuseVel;

        private Material displayMat;
        private Material debugMat;

        // Fluid simulation RTs — double-buffered for read/write
        private RenderTexture velocityA, velocityB;
        private RenderTexture dyeA, dyeB;

        // Full res output for display
        private RenderTexture fullResRT;
        public override RenderTexture FullResOutput => fullResRT;
        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        // Motion vector source
        public MotionVectorPass MotionVectorSource { get; set; }

        // Simulation resolution
        private int simDivisor;
        private int currentSimWidth  = -1;
        private int currentSimHeight = -1;

        // Parameters
        private float velocityDecay;
        private float dyeDecay;
        private float velocityScale;
        private float injectionThreshold;
        private float turbulenceScale;
        private float turbulenceSpeed;
        private float turbulenceStrength;
        private bool  showDebug;

        private List<DepthParameter> parameters;

        public int SimDivisor
        {
            get => simDivisor;
            set { simDivisor = Mathf.Clamp(value, 1, 8); ReleaseSimRTs(); }
        }

        public float VelocityDecay
        {
            get => velocityDecay;
            set { velocityDecay = Mathf.Clamp(value, 0f, 1f); }
        }

        public float DyeDecay
        {
            get => dyeDecay;
            set { dyeDecay = Mathf.Clamp(value, 0f, 1f); }
        }

        public float VelocityScale
        {
            get => velocityScale;
            set { velocityScale = value; }
        }

        public float InjectionThreshold
        {
            get => injectionThreshold;
            set { injectionThreshold = Mathf.Clamp(value, 0f, 1f); }
        }

        public float TurbulenceScale
        {
            get => turbulenceScale;
            set { turbulenceScale = Mathf.Max(0f, value); }
        }

        public float TurbulenceSpeed
        {
            get => turbulenceSpeed;
            set { turbulenceSpeed = Mathf.Max(0f, value); }
        }

        public float TurbulenceStrength
        {
            get => turbulenceStrength;
            set { turbulenceStrength = value; }
        }

        public bool ShowDebug
        {
            get => showDebug;
            set { showDebug = value; }
        }

        public FluidTrailsPass(string passName = "FluidTrails")
        {
            name       = passName;
            compute    = Resources.Load<ComputeShader>("FluidTrailsCompute");
            displayMat = new Material(Shader.Find("Custom/FluidTrails"));
            debugMat   = new Material(Shader.Find("Custom/MotionVectorViewer"));

            kernelInject     = compute.FindKernel("InjectVelocity");
            kernelAdvectVel  = compute.FindKernel("AdvectAndDecay");
            kernelAdvectDye  = compute.FindKernel("AdvectDye");
            kernelDiffuseVel = compute.FindKernel("DiffuseVelocity");

            LoadPrefs();
            BuildParameters();
        }

        private void BuildParameters()
        {
            parameters = new List<DepthParameter>
            {
                new IntParameter  ("Sim Divisor",       () => simDivisor,          v => { SimDivisor = v; },           1,      8),
                new FloatParameter("Velocity Decay",    () => velocityDecay,       v => { VelocityDecay = v; },        0.005f, 0f,    1f),
                new FloatParameter("Dye Decay",         () => dyeDecay,            v => { DyeDecay = v; },             0.005f, 0f,    1f),
                new FloatParameter("Velocity Scale",    () => velocityScale,       v => { VelocityScale = v; },        0.1f,  -100f,  100f),
                new FloatParameter("Inject Thresh",     () => injectionThreshold,  v => { InjectionThreshold = v; },   0.01f,  0f,    1f),
                new FloatParameter("Turb Scale",        () => turbulenceScale,     v => { TurbulenceScale = v; },      0.5f,   0f,    20f),
                new FloatParameter("Turb Speed",        () => turbulenceSpeed,     v => { TurbulenceSpeed = v; },      0.1f,   0f,    5f),
                new FloatParameter("Turb Strength",     () => turbulenceStrength,  v => { TurbulenceStrength = v; },   0.01f, -1f,    1f),
                new BoolParameter ("Debug Velocity",    () => showDebug,           v => { ShowDebug = v; }),
            };
        }

        private RenderTexture CreateSimRT(int w, int h)
        {
            var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat);
            rt.enableRandomWrite = true;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode   = TextureWrapMode.Clamp;
            rt.Create();
            return rt;
        }

        private void EnsureSimRTs(int srcWidth, int srcHeight)
        {
            int sw = Mathf.Max(1, srcWidth  / simDivisor);
            int sh = Mathf.Max(1, srcHeight / simDivisor);

            if(sw == currentSimWidth && sh == currentSimHeight) return;

            ReleaseSimRTs();

            velocityA = CreateSimRT(sw, sh);
            velocityB = CreateSimRT(sw, sh);
            dyeA      = CreateSimRT(sw, sh);
            dyeB      = CreateSimRT(sw, sh);

            Graphics.Blit(Texture2D.blackTexture, dyeA);
            Graphics.Blit(Texture2D.blackTexture, dyeB);

            var neutralVel = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
            neutralVel.SetPixel(0, 0, new Color(0.5f, 0.5f, 0f, 0f));
            neutralVel.Apply();
            Graphics.Blit(neutralVel, velocityA);
            Graphics.Blit(neutralVel, velocityB);
            Object.Destroy(neutralVel);

            currentSimWidth  = sw;
            currentSimHeight = sh;
        }

        private void EnsureOutputRT()
        {
            if(fullResRT != null && fullResRT.width == OutputWidth && fullResRT.height == OutputHeight) return;
            if(fullResRT != null) fullResRT.Release();
            fullResRT = new RenderTexture(OutputWidth, OutputHeight, 0, RenderTextureFormat.ARGB32);
            fullResRT.filterMode = FilterMode.Bilinear;
            fullResRT.Create();
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            if(compute == null)
            {
                Debug.LogError("FluidTrailsPass: missing compute shader.");
                Graphics.Blit(src, dst);
                return;
            }

            if(MotionVectorSource == null || MotionVectorSource.VectorField == null)
            {
                Debug.LogWarning("FluidTrailsPass: no MotionVectorPass source assigned.");
                Graphics.Blit(src, dst);
                return;
            }

            EnsureSimRTs(src.width, src.height);
            EnsureOutputRT();

            int sw = currentSimWidth;
            int sh = currentSimHeight;
            int gx = Mathf.CeilToInt(sw / 8.0f);
            int gy = Mathf.CeilToInt(sh / 8.0f);

            // Shared uniforms set once
            compute.SetInt  ("_Width",    sw);
            compute.SetInt  ("_Height",   sh);
            compute.SetInt  ("_SrcWidth",  src.width);
            compute.SetInt  ("_SrcHeight", src.height);
            compute.SetFloat("_Time",      Time.time);
            compute.SetFloat("_TurbulenceScale",    turbulenceScale);
            compute.SetFloat("_TurbulenceSpeed",    turbulenceSpeed);
            compute.SetFloat("_TurbulenceStrength", turbulenceStrength);

            // --- Kernel 1: Inject velocity (read A, write B) ---
            compute.SetTexture(kernelInject, "_DepthMask",         src);
            compute.SetTexture(kernelInject, "_MotionVectors",     MotionVectorSource.VectorField);
            compute.SetTexture(kernelInject, "_VelocityFieldRead", velocityA);
            compute.SetTexture(kernelInject, "_VelocityField",     velocityB);
            compute.SetFloat  ("_VelocityScale",        velocityScale);
            compute.SetFloat  ("_InjectionThreshold",   injectionThreshold);
            compute.Dispatch(kernelInject, gx, gy, 1);

            // Swap: B has injected velocity
            var tmp = velocityA; velocityA = velocityB; velocityB = tmp;

            // --- Kernel 4: Diffuse velocity (read A, write B) ---
            // Spreads energy outward from injection points
            compute.SetTexture(kernelDiffuseVel, "_VelocityFieldRead", velocityA);
            compute.SetTexture(kernelDiffuseVel, "_VelocityField",     velocityB);
            compute.Dispatch(kernelDiffuseVel, gx, gy, 1);

            // Swap: B has diffused velocity
            var tmp4 = velocityA; velocityA = velocityB; velocityB = tmp4;

            // --- Kernel 2: Advect and decay velocity (read A, write B) ---
            compute.SetTexture(kernelAdvectVel, "_VelocityFieldRead", velocityA);
            compute.SetTexture(kernelAdvectVel, "_VelocityField",     velocityB);
            compute.SetFloat  ("_VelocityDecay", velocityDecay);
            compute.Dispatch(kernelAdvectVel, gx, gy, 1);

            // Swap: B has advected velocity
            var tmp2 = velocityA; velocityA = velocityB; velocityB = tmp2;

            // --- Kernel 3: Advect dye ---
            compute.SetTexture(kernelAdvectDye, "_DepthMask",         src);
            compute.SetTexture(kernelAdvectDye, "_MotionVectors",     MotionVectorSource.VectorField);
            compute.SetTexture(kernelAdvectDye, "_VelocityFieldRead", velocityA);
            compute.SetTexture(kernelAdvectDye, "_DyeField",          dyeB);
            compute.SetTexture(kernelAdvectDye, "_DyeFieldRead",      dyeA);
            compute.SetFloat  ("_DyeDecay", dyeDecay);
            compute.Dispatch(kernelAdvectDye, gx, gy, 1);

            // Swap dye buffers
            var tmp3 = dyeA; dyeA = dyeB; dyeB = tmp3;

            // Display
            if(showDebug)
                Graphics.Blit(velocityA, fullResRT, debugMat);
            else
                Graphics.Blit(dyeA, fullResRT, displayMat);
            Graphics.Blit(src, dst);
        }

        public override void LoadPrefs()
        {
            SimDivisor          = PlayerPrefs.GetInt  (PrefKey("simDivisor"),          2);
            VelocityDecay       = PlayerPrefs.GetFloat(PrefKey("velocityDecay"),       0.98f);
            DyeDecay            = PlayerPrefs.GetFloat(PrefKey("dyeDecay"),            0.97f);
            VelocityScale       = PlayerPrefs.GetFloat(PrefKey("velocityScale"),       3.0f);
            InjectionThreshold  = PlayerPrefs.GetFloat(PrefKey("injectionThreshold"),  0.05f);
            TurbulenceScale     = PlayerPrefs.GetFloat(PrefKey("turbulenceScale"),     4.0f);
            TurbulenceSpeed     = PlayerPrefs.GetFloat(PrefKey("turbulenceSpeed"),     0.5f);
            TurbulenceStrength  = PlayerPrefs.GetFloat(PrefKey("turbulenceStrength"),  0.0f);  // default off
            ShowDebug           = PlayerPrefs.GetInt  (PrefKey("showDebug"),           0) == 1;
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetInt  (PrefKey("simDivisor"),          simDivisor);
            PlayerPrefs.SetFloat(PrefKey("velocityDecay"),       velocityDecay);
            PlayerPrefs.SetFloat(PrefKey("dyeDecay"),            dyeDecay);
            PlayerPrefs.SetFloat(PrefKey("velocityScale"),       velocityScale);
            PlayerPrefs.SetFloat(PrefKey("injectionThreshold"),  injectionThreshold);
            PlayerPrefs.SetFloat(PrefKey("turbulenceScale"),     turbulenceScale);
            PlayerPrefs.SetFloat(PrefKey("turbulenceSpeed"),     turbulenceSpeed);
            PlayerPrefs.SetFloat(PrefKey("turbulenceStrength"),  turbulenceStrength);
            PlayerPrefs.SetInt  (PrefKey("showDebug"),           showDebug ? 1 : 0);
        }

        private void ReleaseSimRTs()
        {
            if(velocityA != null) { velocityA.Release(); velocityA = null; }
            if(velocityB != null) { velocityB.Release(); velocityB = null; }
            if(dyeA      != null) { dyeA.Release();      dyeA      = null; }
            if(dyeB      != null) { dyeB.Release();      dyeB      = null; }
            currentSimWidth  = -1;
            currentSimHeight = -1;
        }

        public void Dispose()
        {
            ReleaseSimRTs();
            if(fullResRT != null) { fullResRT.Release(); fullResRT = null; }
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}
