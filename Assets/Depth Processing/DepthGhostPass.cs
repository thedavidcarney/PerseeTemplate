using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class DepthGhostPass : DepthPass
    {
        private Material mat;
        private float blendFactor;
        private RenderTexture historyRT;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public float BlendFactor
        {
            get => blendFactor;
            set { blendFactor = Mathf.Clamp(value, 0.01f, 1f); mat?.SetFloat("_BlendFactor", blendFactor); }
        }

        public DepthGhostPass(string passName = "DepthGhost")
        {
            name = passName;
            mat  = new Material(Shader.Find("Custom/DepthGhost"));
            LoadPrefs();
            parameters = new List<DepthParameter>
            {
                new FloatParameter("Blend Factor", () => blendFactor, v => { BlendFactor = v; }, 0.01f, 0.01f, 1f),
            };
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            if(historyRT == null || historyRT.width != src.width || historyRT.height != src.height)
            {
                if(historyRT != null) historyRT.Release();
                historyRT = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.RFloat);
                historyRT.filterMode = FilterMode.Bilinear;
                historyRT.Create();
                Graphics.Blit(src, historyRT);
            }

            mat.SetFloat("_BlendFactor", blendFactor);
            mat.SetTexture("_HistoryTex", historyRT);
            Graphics.Blit(src, dst, mat);

            // Store result as next frame's history
            Graphics.Blit(dst, historyRT);
        }

        public override void LoadPrefs()
        {
            BlendFactor = PlayerPrefs.GetFloat(PrefKey("blendFactor"), 0.2f);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetFloat(PrefKey("blendFactor"), blendFactor);
        }

        public void Dispose()
        {
            if(historyRT != null) { historyRT.Release(); historyRT = null; }
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}
