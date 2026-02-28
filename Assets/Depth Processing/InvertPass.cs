using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class InvertPass : DepthPass
    {
        private Material mat;
        private bool ignoreBlack;
        private float blackThresh;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public bool IgnoreBlack
        {
            get => ignoreBlack;
            set { ignoreBlack = value; }
        }

        public float BlackThresh
        {
            get => blackThresh;
            set { blackThresh = Mathf.Clamp(value, 0f, 1f); }
        }

        public InvertPass(string passName = "Invert")
        {
            name = passName;
            mat  = new Material(Shader.Find("Custom/Invert"));
            LoadPrefs();
            parameters = new List<DepthParameter>
            {
                new BoolParameter ("Ignore Black",     () => ignoreBlack,  v => { IgnoreBlack = v; }),
                new FloatParameter("Black Threshold",  () => blackThresh,  v => { BlackThresh = v; }, 0.01f, 0f, 0.5f),
            };
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            mat.SetFloat("_IgnoreBlack", ignoreBlack ? 1f : 0f);
            mat.SetFloat("_BlackThresh", blackThresh);
            Graphics.Blit(src, dst, mat);
        }

        public override void LoadPrefs()
        {
            IgnoreBlack = PlayerPrefs.GetInt  (PrefKey("ignoreBlack"), 0) == 1;
            BlackThresh = PlayerPrefs.GetFloat(PrefKey("blackThresh"), 0.01f);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetInt  (PrefKey("ignoreBlack"), ignoreBlack ? 1 : 0);
            PlayerPrefs.SetFloat(PrefKey("blackThresh"), blackThresh);
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}
