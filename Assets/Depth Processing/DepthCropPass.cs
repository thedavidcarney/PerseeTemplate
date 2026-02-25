using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class DepthCropPass : DepthPass
    {
        private Material mat;
        private float minDepth;
        private float maxDepth;
        private List<DepthParameter> parameters;

        public float MinDepth
        {
            get => minDepth;
            set { minDepth = value; mat?.SetFloat("_MinDepth", value); }
        }

        public float MaxDepth
        {
            get => maxDepth;
            set { maxDepth = value; mat?.SetFloat("_MaxDepth", value); }
        }

        public DepthCropPass(string passName = "DepthCrop")
        {
            name = passName;
            mat = new Material(Shader.Find("Custom/DepthCrop"));
            LoadPrefs();
            parameters = new List<DepthParameter>
            {
                new FloatParameter("Min Depth", () => minDepth, v => { MinDepth = v; }, 0.01f, 0f, 1f),
                new FloatParameter("Max Depth", () => maxDepth, v => { MaxDepth = v; }, 0.01f, 0f, 1f)
            };
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            mat.SetFloat("_MinDepth", minDepth);
            mat.SetFloat("_MaxDepth", maxDepth);
            Graphics.Blit(src, dst, mat);
        }

        public override void LoadPrefs()
        {
            MinDepth = PlayerPrefs.GetFloat(PrefKey("minDepth"), 0.0f);
            MaxDepth = PlayerPrefs.GetFloat(PrefKey("maxDepth"), 1.0f);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetFloat(PrefKey("minDepth"), minDepth);
            PlayerPrefs.SetFloat(PrefKey("maxDepth"), maxDepth);
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}