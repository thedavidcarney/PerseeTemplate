using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class BoxBlurPass : DepthPass
    {
        private Material mat;
        private int radius;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public int Radius
        {
            get => radius;
            set { radius = Mathf.Clamp(value, 1, 8); }
        }

        public BoxBlurPass(string passName = "BoxBlur")
        {
            name = passName;
            mat  = new Material(Shader.Find("Custom/BoxBlur"));
            LoadPrefs();
            parameters = new List<DepthParameter>
            {
                new IntParameter("Radius", () => radius, v => { Radius = v; }, 1, 8),
            };
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            mat.SetInt("_Radius", radius);
            mat.SetVector("_TexelSize", new Vector4(1f / src.width, 1f / src.height, 0, 0));
            Graphics.Blit(src, dst, mat);
        }

        public override void LoadPrefs()
        {
            Radius = PlayerPrefs.GetInt(PrefKey("radius"), 1);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetInt(PrefKey("radius"), radius);
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}
