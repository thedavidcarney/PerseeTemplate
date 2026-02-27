using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class DownsamplePass : DepthPass
    {
        private Material mat;
        private RenderTexture halfRT;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public DownsamplePass(string passName = "Downsample")
        {
            name = passName;
            mat = new Material(Shader.Find("Custom/Downsample"));
            LoadPrefs();
            parameters = new List<DepthParameter>();
        }

        private void EnsureRT(int srcWidth, int srcHeight)
        {
            int w = Mathf.Max(1, srcWidth / 2);
            int h = Mathf.Max(1, srcHeight / 2);
            if (halfRT == null || halfRT.width != w || halfRT.height != h)
            {
                if (halfRT != null) halfRT.Release();
                halfRT = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat);
                halfRT.filterMode = FilterMode.Bilinear;
                halfRT.Create();
            }
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            EnsureRT(src.width, src.height);
            mat.SetVector("_TexelSize", new Vector4(1.0f / src.width, 1.0f / src.height, 0, 0));
            // Blit src -> halfRT (half res), then halfRT -> dst so pipeline ping-pong gets the small RT
            Graphics.Blit(src, halfRT, mat);
            Graphics.Blit(halfRT, dst);
        }

        public override void LoadPrefs() { }
        public override void SavePrefs() { }
        public override List<DepthParameter> GetParameters() => parameters;

        public void Dispose()
        {
            if (halfRT != null) { halfRT.Release(); halfRT = null; }
        }
    }
}
