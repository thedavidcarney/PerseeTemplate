using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class UpsamplePass : DepthPass
    {
        private Material mat;
        private RenderTexture doubleRT;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public UpsamplePass(string passName = "Upsample")
        {
            name = passName;
            mat = new Material(Shader.Find("Custom/Upsample"));
            LoadPrefs();
            parameters = new List<DepthParameter>();
        }

        private void EnsureRT(int srcWidth, int srcHeight)
        {
            int w = srcWidth * 2;
            int h = srcHeight * 2;
            if (doubleRT == null || doubleRT.width != w || doubleRT.height != h)
            {
                if (doubleRT != null) doubleRT.Release();
                doubleRT = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat);
                doubleRT.filterMode = FilterMode.Bilinear;
                doubleRT.Create();
            }
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            EnsureRT(src.width, src.height);
            // Blit src -> doubleRT with bilinear filtering, then to dst
            Graphics.Blit(src, doubleRT, mat);
            Graphics.Blit(doubleRT, dst);
        }

        public override void LoadPrefs() { }
        public override void SavePrefs() { }
        public override List<DepthParameter> GetParameters() => parameters;

        public void Dispose()
        {
            if (doubleRT != null) { doubleRT.Release(); doubleRT = null; }
        }
    }
}
