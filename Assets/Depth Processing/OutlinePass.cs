using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class OutlinePass : DepthPass
    {
        private Material mat;
        private int thickness;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public int Thickness
        {
            get => thickness;
            set { thickness = Mathf.Clamp(value, 1, 8); }
        }

        public OutlinePass(string passName = "Outline")
        {
            name = passName;
            mat  = new Material(Shader.Find("Custom/Outline"));
            LoadPrefs();
            parameters = new List<DepthParameter>
            {
                new IntParameter("Thickness", () => thickness, v => { Thickness = v; }, 1, 8),
            };
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            mat.SetInt   ("_Thickness", thickness);
            mat.SetVector("_TexelSize", new Vector4(1f / src.width, 1f / src.height, 0, 0));
            Graphics.Blit(src, dst, mat);
        }

        public override void LoadPrefs()
        {
            Thickness = PlayerPrefs.GetInt(PrefKey("thickness"), 2);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetInt(PrefKey("thickness"), thickness);
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}
