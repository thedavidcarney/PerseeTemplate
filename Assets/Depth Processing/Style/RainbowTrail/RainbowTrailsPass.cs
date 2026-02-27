using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class RainbowTrailsPass : StylePass
    {
        private Material rainbowFillMat;
        private Material accumMat;

        // RTs
        private RenderTexture accumRT_A;    // ping
        private RenderTexture accumRT_B;    // pong
        private RenderTexture compositeRT;  // rainbow fill composited onto accum
        private RenderTexture fullResRT;    // final output presented to display

        // Parameters
        private float hueSpeed;
        private float saturation;
        private float brightness;
        private float fadeSpeed;
        private float trailBlur;
        private float smearX;
        private float smearY;
        private float smearAmount;
        private float turbulenceScale;
        private float turbulenceSpeed;
        private float turbulenceAmount;

        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;
        public override RenderTexture FullResOutput => fullResRT;

        // --- Properties ---

        public float HueSpeed
        {
            get => hueSpeed;
            set { hueSpeed = value; rainbowFillMat?.SetFloat("_HueSpeed", value); }
        }

        public float Saturation
        {
            get => saturation;
            set { saturation = value; rainbowFillMat?.SetFloat("_Saturation", value); }
        }

        public float Brightness
        {
            get => brightness;
            set { brightness = value; rainbowFillMat?.SetFloat("_Brightness", value); }
        }

        public float FadeSpeed
        {
            get => fadeSpeed;
            set { fadeSpeed = value; accumMat?.SetFloat("_FadeSpeed", value); }
        }

        public float TrailBlur
        {
            get => trailBlur;
            set { trailBlur = value; accumMat?.SetFloat("_TrailBlur", value); }
        }

        public float SmearX
        {
            get => smearX;
            set { smearX = value; accumMat?.SetFloat("_SmearX", value); }
        }

        public float SmearY
        {
            get => smearY;
            set { smearY = value; accumMat?.SetFloat("_SmearY", value); }
        }

        public float SmearAmount
        {
            get => smearAmount;
            set { smearAmount = value; accumMat?.SetFloat("_SmearAmount", value); }
        }

        public float TurbulenceScale
        {
            get => turbulenceScale;
            set { turbulenceScale = value; accumMat?.SetFloat("_TurbulenceScale", value); }
        }

        public float TurbulenceSpeed
        {
            get => turbulenceSpeed;
            set { turbulenceSpeed = value; accumMat?.SetFloat("_TurbulenceSpeed", value); }
        }

        public float TurbulenceAmount
        {
            get => turbulenceAmount;
            set { turbulenceAmount = value; accumMat?.SetFloat("_TurbulenceAmount", value); }
        }

        public RainbowTrailsPass(string passName = "RainbowTrails")
        {
            name = passName;
            rainbowFillMat = new Material(Shader.Find("Custom/RainbowFill"));
            accumMat       = new Material(Shader.Find("Custom/RainbowAccum"));
            LoadPrefs();
            BuildParameters();
        }

        private void BuildParameters()
        {
            parameters = new List<DepthParameter>
            {
                new FloatParameter("Hue Speed",        () => hueSpeed,         v => { HueSpeed = v; },         0.05f,  0f,    5f),
                new FloatParameter("Saturation",       () => saturation,       v => { Saturation = v; },       0.05f,  0f,    1f),
                new FloatParameter("Brightness",       () => brightness,       v => { Brightness = v; },       0.05f,  0f,    1f),
                new FloatParameter("Fade Speed",       () => fadeSpeed,        v => { FadeSpeed = v; },        0.005f, 0.001f,0.5f),
                new FloatParameter("Trail Blur",       () => trailBlur,        v => { TrailBlur = v; },        0.05f,  0f,    2f),
                new FloatParameter("Smear X",          () => smearX,           v => { SmearX = v; },           0.1f,  -5f,    5f),
                new FloatParameter("Smear Y",          () => smearY,           v => { SmearY = v; },           0.1f,  -5f,    5f),
                new FloatParameter("Smear Amount",     () => smearAmount,      v => { SmearAmount = v; },      0.05f,  0f,    2f),
                new FloatParameter("Turbulence Scale", () => turbulenceScale,  v => { TurbulenceScale = v; },  0.5f,   0.1f,  20f),
                new FloatParameter("Turbulence Speed", () => turbulenceSpeed,  v => { TurbulenceSpeed = v; },  0.1f,   0f,    5f),
                new FloatParameter("Turbulence Amt",   () => turbulenceAmount, v => { TurbulenceAmount = v; }, 0.05f,  0f,    2f),
            };
        }

        private RenderTexture CreateRT(int width, int height, RenderTextureFormat format)
        {
            var rt = new RenderTexture(width, height, 0, format);
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode   = TextureWrapMode.Clamp;
            rt.Create();
            return rt;
        }

        private void EnsureRTs(int srcWidth, int srcHeight)
        {
            if (fullResRT == null || fullResRT.width != OutputWidth || fullResRT.height != OutputHeight)
            {
                if (fullResRT    != null) fullResRT.Release();
                if (compositeRT  != null) compositeRT.Release();
                if (accumRT_A    != null) accumRT_A.Release();
                if (accumRT_B    != null) accumRT_B.Release();

                fullResRT   = CreateRT(OutputWidth, OutputHeight, RenderTextureFormat.ARGB32);
                compositeRT = CreateRT(OutputWidth, OutputHeight, RenderTextureFormat.ARGB32);
                accumRT_A   = CreateRT(OutputWidth, OutputHeight, RenderTextureFormat.ARGB32);
                accumRT_B   = CreateRT(OutputWidth, OutputHeight, RenderTextureFormat.ARGB32);

                // Clear accum buffers to black
                Graphics.Blit(Texture2D.blackTexture, accumRT_A);
                Graphics.Blit(Texture2D.blackTexture, accumRT_B);
            }
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            EnsureRTs(src.width, src.height);

            // Step 1: Fade + smear + turbulate the current accum buffer into accumRT_B
            accumMat.SetFloat("_FadeSpeed",         fadeSpeed);
            accumMat.SetFloat("_TrailBlur",         trailBlur);
            accumMat.SetFloat("_SmearX",            smearX);
            accumMat.SetFloat("_SmearY",            smearY);
            accumMat.SetFloat("_SmearAmount",       smearAmount);
            accumMat.SetFloat("_TurbulenceScale",   turbulenceScale);
            accumMat.SetFloat("_TurbulenceSpeed",   turbulenceSpeed);
            accumMat.SetFloat("_TurbulenceAmount",  turbulenceAmount);
            accumMat.SetVector("_TexelSize", new Vector4(1.0f / OutputWidth, 1.0f / OutputHeight, 0, 0));
            Graphics.Blit(accumRT_A, accumRT_B, accumMat);

            // Step 2: Composite current rainbow fill onto faded accum
            rainbowFillMat.SetFloat("_HueSpeed",   hueSpeed);
            rainbowFillMat.SetFloat("_Saturation", saturation);
            rainbowFillMat.SetFloat("_Brightness", brightness);
            rainbowFillMat.SetTexture("_AccumTex", accumRT_B);
            Graphics.Blit(src, compositeRT, rainbowFillMat);

            // Step 3: Copy composite into accumRT_A for next frame
            Graphics.Blit(compositeRT, accumRT_A);

            // Step 4: Output to fullResRT and dst
            Graphics.Blit(compositeRT, fullResRT);
            Graphics.Blit(fullResRT, dst);
        }

        public override void LoadPrefs()
        {
            HueSpeed         = PlayerPrefs.GetFloat(PrefKey("hueSpeed"),         0.3f);
            Saturation       = PlayerPrefs.GetFloat(PrefKey("saturation"),       1.0f);
            Brightness       = PlayerPrefs.GetFloat(PrefKey("brightness"),       1.0f);
            FadeSpeed        = PlayerPrefs.GetFloat(PrefKey("fadeSpeed"),        0.02f);
            TrailBlur        = PlayerPrefs.GetFloat(PrefKey("trailBlur"),        0.0f);
            SmearX           = PlayerPrefs.GetFloat(PrefKey("smearX"),           0.0f);
            SmearY           = PlayerPrefs.GetFloat(PrefKey("smearY"),           0.0f);
            SmearAmount      = PlayerPrefs.GetFloat(PrefKey("smearAmount"),      0.0f);
            TurbulenceScale  = PlayerPrefs.GetFloat(PrefKey("turbulenceScale"),  5.0f);
            TurbulenceSpeed  = PlayerPrefs.GetFloat(PrefKey("turbulenceSpeed"),  1.0f);
            TurbulenceAmount = PlayerPrefs.GetFloat(PrefKey("turbulenceAmount"), 0.0f);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetFloat(PrefKey("hueSpeed"),         hueSpeed);
            PlayerPrefs.SetFloat(PrefKey("saturation"),       saturation);
            PlayerPrefs.SetFloat(PrefKey("brightness"),       brightness);
            PlayerPrefs.SetFloat(PrefKey("fadeSpeed"),        fadeSpeed);
            PlayerPrefs.SetFloat(PrefKey("trailBlur"),        trailBlur);
            PlayerPrefs.SetFloat(PrefKey("smearX"),           smearX);
            PlayerPrefs.SetFloat(PrefKey("smearY"),           smearY);
            PlayerPrefs.SetFloat(PrefKey("smearAmount"),      smearAmount);
            PlayerPrefs.SetFloat(PrefKey("turbulenceScale"),  turbulenceScale);
            PlayerPrefs.SetFloat(PrefKey("turbulenceSpeed"),  turbulenceSpeed);
            PlayerPrefs.SetFloat(PrefKey("turbulenceAmount"), turbulenceAmount);
        }

        public void Dispose()
        {
            if (fullResRT   != null) { fullResRT.Release();   fullResRT   = null; }
            if (compositeRT != null) { compositeRT.Release(); compositeRT = null; }
            if (accumRT_A   != null) { accumRT_A.Release();   accumRT_A   = null; }
            if (accumRT_B   != null) { accumRT_B.Release();   accumRT_B   = null; }
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}
