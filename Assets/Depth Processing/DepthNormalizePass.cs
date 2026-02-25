using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class DepthNormalizePass : DepthPass
    {
        private Material mat;
        private float maxDepthMM;
        private bool flipX;
        private bool flipY;
        private List<DepthParameter> parameters;

        public float MaxDepthMM
        {
            get => maxDepthMM;
            set { maxDepthMM = value; mat?.SetFloat("_MaxDepthMM", value); }
        }

        public bool FlipX
        {
            get => flipX;
            set { flipX = value; mat?.SetInt("_FlipX", value ? 1 : 0); }
        }

        public bool FlipY
        {
            get => flipY;
            set { flipY = value; mat?.SetInt("_FlipY", value ? 1 : 0); }
        }

        public DepthNormalizePass(string passName = "DepthNormalize")
        {
            name = passName;
            mat = new Material(Shader.Find("Custom/DepthNormalize"));
            LoadPrefs();
            parameters = new List<DepthParameter>
            {
                new FloatParameter("Max Depth MM", () => maxDepthMM, v => { MaxDepthMM = v; }, 100f, 100f, 20000f),
                new BoolParameter("Flip X", () => flipX, v => { FlipX = v; }),
                new BoolParameter("Flip Y", () => flipY, v => { FlipY = v; })
            };
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            mat.SetFloat("_MaxDepthMM", maxDepthMM);
            mat.SetInt("_FlipX", flipX ? 1 : 0);
            mat.SetInt("_FlipY", flipY ? 1 : 0);
            Graphics.Blit(src, dst, mat);
        }

        public override void LoadPrefs()
        {
            MaxDepthMM = PlayerPrefs.GetFloat(PrefKey("maxDepthMM"), 10000f);
            FlipX = PlayerPrefs.GetInt(PrefKey("flipX"), 1) == 1;
            FlipY = PlayerPrefs.GetInt(PrefKey("flipY"), 1) == 1;
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetFloat(PrefKey("maxDepthMM"), maxDepthMM);
            PlayerPrefs.SetInt(PrefKey("flipX"), flipX ? 1 : 0);
            PlayerPrefs.SetInt(PrefKey("flipY"), flipY ? 1 : 0);
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}