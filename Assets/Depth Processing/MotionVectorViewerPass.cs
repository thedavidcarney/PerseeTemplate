using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class MotionVectorViewerPass : StylePass
    {
        private Material mat;
        private RenderTexture fullResRT;

        public MotionVectorPass Source { get; set; }

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;
        public override RenderTexture FullResOutput => fullResRT;

        public MotionVectorViewerPass(string passName = "MotionVectorViewer")
        {
            name = passName;
            mat  = new Material(Shader.Find("Custom/MotionVectorViewer"));
        }

        private void EnsureRT()
        {
            if(fullResRT != null && fullResRT.width == OutputWidth && fullResRT.height == OutputHeight) return;
            if(fullResRT != null) fullResRT.Release();
            fullResRT = new RenderTexture(OutputWidth, OutputHeight, 0, RenderTextureFormat.ARGB32);
            fullResRT.filterMode = FilterMode.Bilinear;
            fullResRT.Create();
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            EnsureRT();

            if(Source == null || Source.VectorField == null)
            {
                Graphics.Blit(Texture2D.blackTexture, fullResRT);
                Graphics.Blit(src, dst);
                return;
            }

            Graphics.Blit(Source.VectorField, fullResRT, mat);
            Graphics.Blit(src, dst);
        }

        public override void LoadPrefs() { }
        public override void SavePrefs() { }

        public void Dispose()
        {
            if(fullResRT != null) { fullResRT.Release(); fullResRT = null; }
        }

        public override List<DepthParameter> GetParameters() => new List<DepthParameter>();
    }
}
