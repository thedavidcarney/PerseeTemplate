using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class TemporalNoisePass : DepthPass
    {
        private Material mat;
        private float historyWeight;
        private RenderTexture historyRT;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public float HistoryWeight
        {
            get => historyWeight;
            set { historyWeight = Mathf.Clamp(value, 0f, 1f); mat?.SetFloat("_HistoryWeight", value); }
        }

        public TemporalNoisePass(string passName = "TemporalNoise")
        {
            name = passName;
            mat = new Material(Shader.Find("Custom/TemporalNoise"));
            LoadPrefs();
            parameters = new List<DepthParameter>
            {
                new FloatParameter("History Weight", () => historyWeight, v => { HistoryWeight = v; }, 0.05f, 0f, 1f)
            };
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            if(historyRT == null)
            {
                historyRT = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.RFloat);
                historyRT.Create();
                Graphics.Blit(src, historyRT);
            }
            mat.SetFloat("_HistoryWeight", historyWeight);
            mat.SetTexture("_HistoryTex", historyRT);
            Graphics.Blit(src, dst, mat);
            Graphics.Blit(dst, historyRT);
        }

        public override void LoadPrefs()
        {
            HistoryWeight = PlayerPrefs.GetFloat(PrefKey("historyWeight"), 0.1f);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetFloat(PrefKey("historyWeight"), historyWeight);
        }

        public override List<DepthParameter> GetParameters() => parameters;

        public void Dispose()
        {
            if(historyRT != null)
            {
                historyRT.Release();
                historyRT = null;
            }
        }
    }
}