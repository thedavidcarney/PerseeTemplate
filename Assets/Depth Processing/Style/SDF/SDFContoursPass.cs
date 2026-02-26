using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class SDFContoursPass : StylePass
    {
        private ComputeShader compute;

        private int seedKernel;
        private int jumpKernel;
        private int distanceKernel;
        private int contourKernel;

        private RenderTexture maskRT;
        private RenderTexture seedRT;
        private RenderTexture jfaRT_A;
        private RenderTexture jfaRT_B;
        private RenderTexture distanceRT;
        private RenderTexture outputRT;
        private RenderTexture presentRT;

        private float frequency;
        private float lineWidth;
        private float animSpeed;
        private float maxDist;
        private Color insideColor;
        private Color outsideColor;
        private Color lineColor;

        private int frameSkipCounter = 0;
        private const int FRAME_SKIP = 4;

        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat; // THE FIX
        public RenderTexture FullResOutput => presentRT;

        public float Frequency { get => frequency; set { frequency = value; } }
        public float LineWidth { get => lineWidth; set { lineWidth = value; } }
        public float AnimSpeed { get => animSpeed; set { animSpeed = value; } }
        public float MaxDist { get => maxDist; set { maxDist = value; } }
        public Color InsideColor { get => insideColor; set { insideColor = value; } }
        public Color OutsideColor { get => outsideColor; set { outsideColor = value; } }
        public Color LineColor { get => lineColor; set { lineColor = value; } }

        public SDFContoursPass(string passName = "SDFContours")
        {
            name = passName;
            compute = Resources.Load<ComputeShader>("SDFCompute");
            if(compute == null)
            {
                Debug.LogError("SDFCompute not found in Resources folder");
                return;
            }
            seedKernel = compute.FindKernel("SeedKernel");
            jumpKernel = compute.FindKernel("JumpKernel");
            distanceKernel = compute.FindKernel("DistanceKernel");
            contourKernel = compute.FindKernel("ContourKernel");
            LoadPrefs();
            BuildParameters();
        }

        private void BuildParameters()
        {
            parameters = new List<DepthParameter>
            {
                new FloatParameter("Frequency", () => frequency, v => { Frequency = v; }, 0.5f, 0.5f, 50f),
                new FloatParameter("Line Width", () => lineWidth, v => { LineWidth = v; }, 0.02f, 0.01f, 1f),
                new FloatParameter("Anim Speed", () => animSpeed, v => { AnimSpeed = v; }, 0.1f, -5f, 5f),
                new FloatParameter("Max Dist", () => maxDist, v => { MaxDist = v; }, 0.02f, 0.01f, 2f),

                new FloatParameter("Inside R", () => insideColor.r, v => { InsideColor = new Color(v, insideColor.g, insideColor.b); }, 0.05f, 0f, 1f),
                new FloatParameter("Inside G", () => insideColor.g, v => { InsideColor = new Color(insideColor.r, v, insideColor.b); }, 0.05f, 0f, 1f),
                new FloatParameter("Inside B", () => insideColor.b, v => { InsideColor = new Color(insideColor.r, insideColor.g, v); }, 0.05f, 0f, 1f),

                new FloatParameter("Outside R", () => outsideColor.r, v => { OutsideColor = new Color(v, outsideColor.g, outsideColor.b); }, 0.05f, 0f, 1f),
                new FloatParameter("Outside G", () => outsideColor.g, v => { OutsideColor = new Color(outsideColor.r, v, outsideColor.b); }, 0.05f, 0f, 1f),
                new FloatParameter("Outside B", () => outsideColor.b, v => { OutsideColor = new Color(outsideColor.r, outsideColor.g, v); }, 0.05f, 0f, 1f),

                new FloatParameter("Line R", () => lineColor.r, v => { LineColor = new Color(v, lineColor.g, lineColor.b); }, 0.05f, 0f, 1f),
                new FloatParameter("Line G", () => lineColor.g, v => { LineColor = new Color(lineColor.r, v, lineColor.b); }, 0.05f, 0f, 1f),
                new FloatParameter("Line B", () => lineColor.b, v => { LineColor = new Color(lineColor.r, lineColor.g, v); }, 0.05f, 0f, 1f),
            };
        }

        private RenderTexture CreateRT(int width, int height, RenderTextureFormat format, bool randomWrite = false)
        {
            var rt = new RenderTexture(width, height, 0, format);
            rt.filterMode = FilterMode.Bilinear;
            rt.enableRandomWrite = randomWrite;
            rt.Create();
            return rt;
        }

        private void EnsureRTs(int srcWidth, int srcHeight)
        {
            int jfaWidth = srcWidth / 2;
            int jfaHeight = srcHeight / 2;

            if(seedRT == null || seedRT.width != jfaWidth || seedRT.height != jfaHeight)
            {
                ReleaseRTs();
                maskRT = CreateRT(jfaWidth, jfaHeight, RenderTextureFormat.RFloat, true);
                seedRT = CreateRT(jfaWidth, jfaHeight, RenderTextureFormat.ARGBFloat, true);
                jfaRT_A = CreateRT(jfaWidth, jfaHeight, RenderTextureFormat.ARGBFloat, true);
                jfaRT_B = CreateRT(jfaWidth, jfaHeight, RenderTextureFormat.ARGBFloat, true);
                distanceRT = CreateRT(jfaWidth, jfaHeight, RenderTextureFormat.RFloat, true);
                outputRT = CreateRT(jfaWidth, jfaHeight, RenderTextureFormat.ARGBFloat, true);
            }

            if(presentRT == null || presentRT.width != srcWidth || presentRT.height != srcHeight)
            {
                if(presentRT != null) presentRT.Release();
                presentRT = CreateRT(srcWidth, srcHeight, RenderTextureFormat.ARGBFloat, false);
            }
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            if(compute == null) return;
            EnsureRTs(src.width, src.height);

            int jfaWidth = seedRT.width;
            int jfaHeight = seedRT.height;
            int threadGroupsX = Mathf.CeilToInt(jfaWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(jfaHeight / 8.0f);

            Graphics.Blit(src, maskRT);

            frameSkipCounter++;
            if(frameSkipCounter >= FRAME_SKIP)
            {
                frameSkipCounter = 0;

                compute.SetTexture(seedKernel, "_MaskTex", maskRT);
                compute.SetTexture(seedKernel, "_SeedTex", seedRT);
                compute.SetInt("_Width", jfaWidth);
                compute.SetInt("_Height", jfaHeight);
                compute.Dispatch(seedKernel, threadGroupsX, threadGroupsY, 1);

                Graphics.Blit(seedRT, jfaRT_A);

                RenderTexture current = jfaRT_A;
                RenderTexture next = jfaRT_B;

                int maxDimension = Mathf.Max(jfaWidth, jfaHeight);
                int stepSize = Mathf.NextPowerOfTwo(maxDimension) / 2;
                int maxIterations = 20;
                int iterations = 0;

                while(stepSize >= 1 && iterations < maxIterations)
                {
                    compute.SetTexture(jumpKernel, "_JumpSrc", current);
                    compute.SetTexture(jumpKernel, "_JumpDst", next);
                    compute.SetInt("_StepSize", stepSize);
                    compute.SetInt("_Width", jfaWidth);
                    compute.SetInt("_Height", jfaHeight);
                    compute.Dispatch(jumpKernel, threadGroupsX, threadGroupsY, 1);

                    var tmp = current;
                    current = next;
                    next = tmp;
                    stepSize /= 2;
                    iterations++;
                }

                compute.SetTexture(distanceKernel, "_JumpSrc", current);
                compute.SetTexture(distanceKernel, "_DistanceTex", distanceRT);
                compute.SetInt("_Width", jfaWidth);
                compute.SetInt("_Height", jfaHeight);
                compute.SetFloat("_MaxDist", maxDist);
                compute.Dispatch(distanceKernel, threadGroupsX, threadGroupsY, 1);
            }

            compute.SetTexture(contourKernel, "_MaskTex", maskRT);
            compute.SetTexture(contourKernel, "_DistanceTex", distanceRT);
            compute.SetTexture(contourKernel, "_OutputTex", outputRT);
            compute.SetInt("_Width", jfaWidth);
            compute.SetInt("_Height", jfaHeight);
            compute.SetFloat("_Frequency", frequency);
            compute.SetFloat("_LineWidth", lineWidth);
            compute.SetFloat("_AnimSpeed", animSpeed);
            compute.SetFloat("_Time", Time.time);
            compute.SetVector("_InsideColor", new Vector4(insideColor.r, insideColor.g, insideColor.b, 1));
            compute.SetVector("_OutsideColor", new Vector4(outsideColor.r, outsideColor.g, outsideColor.b, 1));
            compute.SetVector("_LineColor", new Vector4(lineColor.r, lineColor.g, lineColor.b, 1));
            compute.Dispatch(contourKernel, threadGroupsX, threadGroupsY, 1);

            Graphics.Blit(outputRT, presentRT);
            Graphics.Blit(outputRT, dst);
        }

        public override void LoadPrefs()
        {
            Frequency = PlayerPrefs.GetFloat(PrefKey("frequency"), 10f);
            LineWidth = PlayerPrefs.GetFloat(PrefKey("lineWidth"), 0.3f);
            AnimSpeed = PlayerPrefs.GetFloat(PrefKey("animSpeed"), 0.5f);
            MaxDist = PlayerPrefs.GetFloat(PrefKey("maxDist"), 0.3f);
            InsideColor = LoadColor("insideColor", Color.black);
            OutsideColor = LoadColor("outsideColor", new Color(0f, 0.3f, 0.4f));
            LineColor = LoadColor("lineColor", new Color(0f, 0.8f, 1f));
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetFloat(PrefKey("frequency"), frequency);
            PlayerPrefs.SetFloat(PrefKey("lineWidth"), lineWidth);
            PlayerPrefs.SetFloat(PrefKey("animSpeed"), animSpeed);
            PlayerPrefs.SetFloat(PrefKey("maxDist"), maxDist);
            SaveColor("insideColor", insideColor);
            SaveColor("outsideColor", outsideColor);
            SaveColor("lineColor", lineColor);
        }

        private Color LoadColor(string key, Color defaultColor)
        {
            return new Color(
                PlayerPrefs.GetFloat(PrefKey(key + "_r"), defaultColor.r),
                PlayerPrefs.GetFloat(PrefKey(key + "_g"), defaultColor.g),
                PlayerPrefs.GetFloat(PrefKey(key + "_b"), defaultColor.b),
                1f
            );
        }

        private void SaveColor(string key, Color color)
        {
            PlayerPrefs.SetFloat(PrefKey(key + "_r"), color.r);
            PlayerPrefs.SetFloat(PrefKey(key + "_g"), color.g);
            PlayerPrefs.SetFloat(PrefKey(key + "_b"), color.b);
        }

        private void ReleaseRTs()
        {
            if(maskRT != null) { maskRT.Release(); maskRT = null; }
            if(seedRT != null) { seedRT.Release(); seedRT = null; }
            if(jfaRT_A != null) { jfaRT_A.Release(); jfaRT_A = null; }
            if(jfaRT_B != null) { jfaRT_B.Release(); jfaRT_B = null; }
            if(distanceRT != null) { distanceRT.Release(); distanceRT = null; }
            if(outputRT != null) { outputRT.Release(); outputRT = null; }
        }

        public void Dispose()
        {
            ReleaseRTs();
            if(presentRT != null) { presentRT.Release(); presentRT = null; }
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}