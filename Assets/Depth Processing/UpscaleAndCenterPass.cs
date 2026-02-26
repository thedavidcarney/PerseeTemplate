using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class UpscaleAndCenterPass : StylePass
    {
        private Material mat;
        private RenderTexture outputRT;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public UpscaleAndCenterPass(string passName = "UpscaleAndCenter")
        {
            name = passName;
            mat = new Material(Shader.Find("Custom/UpscaleAndCenter"));
            LoadPrefs();
            parameters = new List<DepthParameter>();
        }

        private void EnsureRT()
        {
            if (outputRT == null || outputRT.width != OutputWidth || outputRT.height != OutputHeight)
            {
                if (outputRT != null) outputRT.Release();
                outputRT = new RenderTexture(OutputWidth, OutputHeight, 0, RenderTextureFormat.RFloat);
                outputRT.filterMode = FilterMode.Bilinear;
                outputRT.Create();
            }
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            EnsureRT();

            // Scale to fit vertically: srcHeight maps to OutputHeight
            float scale = (float)OutputHeight / src.height;
            float scaledWidth = src.width * scale;

            // Width and height in dest UV space
            float rectW = scaledWidth / OutputWidth;
            float rectH = 1.0f; // fills full height

            // Center horizontally
            float rectX = (1.0f - rectW) * 0.5f;
            float rectY = 0.0f;

            mat.SetVector("_SrcRect", new Vector4(rectX, rectY, rectW, rectH));
            Graphics.Blit(src, dst, mat);
        }

        public override void LoadPrefs() { }
        public override void SavePrefs() { }
        public override List<DepthParameter> GetParameters() => parameters;

        public void Dispose()
        {
            if (outputRT != null)
            {
                outputRT.Release();
                outputRT = null;
            }
        }
    }
}
