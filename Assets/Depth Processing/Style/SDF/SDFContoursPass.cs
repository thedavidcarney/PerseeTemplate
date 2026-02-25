using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class SDFContoursPass : StylePass
    {
        private Material seedMat;
        private Material jumpMat;
        private Material distanceMat;
        private Material contourMat;

        private RenderTexture seedRT;
        private RenderTexture jfaRT_A;
        private RenderTexture jfaRT_B;
        private RenderTexture distanceRT;
        private RenderTexture fullResRT;
        private RenderTexture fullResMaskRT;

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

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.Default;
        public RenderTexture FullResOutput => fullResRT;

        public float Frequency
        {
            get => frequency;
            set { frequency = value; contourMat?.SetFloat("_Frequency", value); }
        }

        public float LineWidth
        {
            get => lineWidth;
            set { lineWidth = value; contourMat?.SetFloat("_LineWidth", value); }
        }

        public float AnimSpeed
        {
            get => animSpeed;
            set { animSpeed = value; contourMat?.SetFloat("_AnimSpeed", value); }
        }

        public float MaxDist
        {
            get => maxDist;
            set { maxDist = value; distanceMat?.SetFloat("_MaxDist", value); }
        }

        public Color InsideColor
        {
            get => insideColor;
            set { insideColor = value; contourMat?.SetColor("_InsideColor", value); }
        }

        public Color OutsideColor
        {
            get => outsideColor;
            set { outsideColor = value; contourMat?.SetColor("_OutsideColor", value); }
        }

        public Color LineColor
        {
            get => lineColor;
            set { lineColor = value; contourMat?.SetColor("_LineColor", value); }
        }

        public SDFContoursPass(string passName = "SDFContours")
        {
            name = passName;
            seedMat = new Material(Shader.Find("Custom/SDFSeed"));
            jumpMat = new Material(Shader.Find("Custom/SDFJump"));
            distanceMat = new Material(Shader.Find("Custom/SDFDistance"));
            contourMat = new Material(Shader.Find("Custom/SDFContours"));
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

        private RenderTexture CreateRT(int width, int height, RenderTextureFormat format)
        {
            var rt = new RenderTexture(width, height, 0, format);
            rt.filterMode = FilterMode.Bilinear;
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
                seedRT = CreateRT(jfaWidth, jfaHeight, RenderTextureFormat.RGFloat);
                jfaRT_A = CreateRT(jfaWidth, jfaHeight, RenderTextureFormat.RGFloat);
                jfaRT_B = CreateRT(jfaWidth, jfaHeight, RenderTextureFormat.RGFloat);
                distanceRT = CreateRT(jfaWidth, jfaHeight, RenderTextureFormat.RFloat);
            }

            if(fullResRT == null || fullResRT.width != OutputWidth || fullResRT.height != OutputHeight)
            {
                if(fullResRT != null) fullResRT.Release();
                if(fullResMaskRT != null) fullResMaskRT.Release();
                fullResRT = CreateRT(OutputWidth, OutputHeight, RenderTextureFormat.Default);
                fullResMaskRT = CreateRT(srcWidth, srcHeight, RenderTextureFormat.RFloat);
            }
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            EnsureRTs(src.width, src.height);

            frameSkipCounter++;
            if(frameSkipCounter >= FRAME_SKIP)
            {
                frameSkipCounter = 0;

                // 1. Seed pass at half res
                jumpMat.SetVector("_TexelSize", new Vector4(1.0f / seedRT.width, 1.0f / seedRT.height, 0, 0));
                Graphics.Blit(src, seedRT, seedMat);

                // 2. JFA passes
                Graphics.Blit(seedRT, jfaRT_A);

                RenderTexture current = jfaRT_A;
                RenderTexture next = jfaRT_B;

                int maxDimension = Mathf.Max(seedRT.width, seedRT.height);
                int stepSize = Mathf.NextPowerOfTwo(maxDimension) / 2;
                int maxIterations = 20;
                int iterations = 0;

                while(stepSize >= 1 && iterations < maxIterations)
                {
                    jumpMat.SetFloat("_StepSize", stepSize);
                    Graphics.Blit(current, next, jumpMat);
                    var tmp = current;
                    current = next;
                    next = tmp;
                    stepSize /= 2;
                    iterations++;
                }

                if(iterations >= maxIterations)
                    Debug.LogError("JFA loop hit max iterations - something is wrong");

                // 3. Distance pass
                distanceMat.SetFloat("_MaxDist", maxDist);
                Graphics.Blit(current, distanceRT, distanceMat);
            }

            // 4. Contour pass runs every frame using cached distanceRT
            Graphics.Blit(src, fullResMaskRT);

            contourMat.SetFloat("_Frequency", frequency);
            contourMat.SetFloat("_LineWidth", lineWidth);
            contourMat.SetFloat("_AnimSpeed", animSpeed);
            contourMat.SetColor("_InsideColor", insideColor);
            contourMat.SetColor("_OutsideColor", outsideColor);
            contourMat.SetColor("_LineColor", lineColor);
            contourMat.SetTexture("_MaskTex", fullResMaskRT);
            Graphics.Blit(distanceRT, fullResRT, contourMat);

            Graphics.Blit(fullResRT, dst);
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
            if(seedRT != null) { seedRT.Release(); seedRT = null; }
            if(jfaRT_A != null) { jfaRT_A.Release(); jfaRT_A = null; }
            if(jfaRT_B != null) { jfaRT_B.Release(); jfaRT_B = null; }
            if(distanceRT != null) { distanceRT.Release(); distanceRT = null; }
        }

        public void Dispose()
        {
            ReleaseRTs();
            if(fullResRT != null) { fullResRT.Release(); fullResRT = null; }
            if(fullResMaskRT != null) { fullResMaskRT.Release(); fullResMaskRT = null; }
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}