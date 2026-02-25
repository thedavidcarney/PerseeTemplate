using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class ThresholdPass : DepthPass
    {
        private Material mat;
        private float threshold;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public float Threshold
        {
            get => threshold;
            set { threshold = value; mat?.SetFloat("_Threshold", value); }
        }

        public ThresholdPass(string passName = "Threshold")
        {
            name = passName;
            mat = new Material(Shader.Find("Custom/Threshold"));
            LoadPrefs();
            parameters = new List<DepthParameter>
            {
                new FloatParameter("Threshold", () => threshold, v => { Threshold = v; }, 0.01f, 0f, 1f)
            };
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            mat.SetFloat("_Threshold", threshold);
            Graphics.Blit(src, dst, mat);
        }

        public override void LoadPrefs()
        {
            Threshold = PlayerPrefs.GetFloat(PrefKey("threshold"), 0.0f);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetFloat(PrefKey("threshold"), threshold);
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}