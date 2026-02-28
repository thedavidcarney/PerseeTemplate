using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class MotionVectorPass : DepthPass
    {
        private ComputeShader compute;
        private int kernel;

        private RenderTexture vectorFieldRT;
        public RenderTexture VectorField => vectorFieldRT;

        private RenderTexture previousFrame;

        private int   fieldDivisor;
        private int   searchRadius;
        private float motionThreshold;

        private int currentFieldWidth  = -1;
        private int currentFieldHeight = -1;

        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public int FieldDivisor
        {
            get => fieldDivisor;
            set { fieldDivisor = Mathf.Clamp(value, 1, 16); }
        }

        public int SearchRadius
        {
            get => searchRadius;
            set { searchRadius = Mathf.Clamp(value, 1, 32); }
        }

        public float MotionThreshold
        {
            get => motionThreshold;
            set { motionThreshold = Mathf.Clamp(value, 0f, 1f); }
        }

        public MotionVectorPass(string passName = "MotionVector")
        {
            name    = passName;
            compute = Resources.Load<ComputeShader>("MotionVectorCompute");
            kernel  = compute.FindKernel("CSMain");
            LoadPrefs();
            BuildParameters();
        }

        private void BuildParameters()
        {
            parameters = new List<DepthParameter>
            {
                new IntParameter  ("Field Divisor", () => fieldDivisor,    v => { FieldDivisor = v; },    1,     16),
                new IntParameter  ("Search Radius", () => searchRadius,    v => { SearchRadius = v; },    1,     32),
                new FloatParameter("Threshold",     () => motionThreshold, v => { MotionThreshold = v; }, 0.01f, 0f, 1f),
            };
        }

        private void EnsureRTs(int srcWidth, int srcHeight)
        {
            int fw = Mathf.Max(1, srcWidth  / fieldDivisor);
            int fh = Mathf.Max(1, srcHeight / fieldDivisor);

            if(fw == currentFieldWidth && fh == currentFieldHeight) return;

            ReleaseRTs();

            vectorFieldRT = new RenderTexture(fw, fh, 0, RenderTextureFormat.ARGBFloat);
            vectorFieldRT.enableRandomWrite = true;
            vectorFieldRT.filterMode = FilterMode.Bilinear;
            vectorFieldRT.Create();

            previousFrame = new RenderTexture(srcWidth, srcHeight, 0, RenderTextureFormat.RFloat);
            previousFrame.filterMode = FilterMode.Bilinear;
            previousFrame.Create();

            Graphics.Blit(Texture2D.blackTexture, previousFrame);

            currentFieldWidth  = fw;
            currentFieldHeight = fh;
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            if(compute == null)
            {
                Debug.LogError("MotionVectorPass: missing compute shader.");
                Graphics.Blit(src, dst);
                return;
            }

            EnsureRTs(src.width, src.height);

            int fw = vectorFieldRT.width;
            int fh = vectorFieldRT.height;

            compute.SetTexture(kernel, "_CurrentFrame",    src);
            compute.SetTexture(kernel, "_PreviousFrame",   previousFrame);
            compute.SetTexture(kernel, "_VectorField",     vectorFieldRT);
            compute.SetInt    ("_FieldWidth",              fw);
            compute.SetInt    ("_FieldHeight",             fh);
            compute.SetInt    ("_SrcWidth",                src.width);
            compute.SetInt    ("_SrcHeight",               src.height);
            compute.SetInt    ("_SearchRadius",            searchRadius);
            compute.SetFloat  ("_MotionThreshold",         motionThreshold);

            int groupsX = Mathf.CeilToInt(fw / 8.0f);
            int groupsY = Mathf.CeilToInt(fh / 8.0f);
            compute.Dispatch(kernel, groupsX, groupsY, 1);

            Graphics.Blit(src, previousFrame);
            Graphics.Blit(src, dst);
        }

        public override void LoadPrefs()
        {
            FieldDivisor    = PlayerPrefs.GetInt  (PrefKey("fieldDivisor"),    8);
            SearchRadius    = PlayerPrefs.GetInt  (PrefKey("searchRadius"),    8);
            MotionThreshold = PlayerPrefs.GetFloat(PrefKey("motionThreshold"), 0.05f);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetInt  (PrefKey("fieldDivisor"),    fieldDivisor);
            PlayerPrefs.SetInt  (PrefKey("searchRadius"),    searchRadius);
            PlayerPrefs.SetFloat(PrefKey("motionThreshold"), motionThreshold);
        }

        private void ReleaseRTs()
        {
            if(vectorFieldRT != null) { vectorFieldRT.Release(); vectorFieldRT = null; }
            if(previousFrame != null) { previousFrame.Release(); previousFrame = null; }
            currentFieldWidth  = -1;
            currentFieldHeight = -1;
        }

        public void Dispose() => ReleaseRTs();

        public override List<DepthParameter> GetParameters() => parameters;
    }
}
