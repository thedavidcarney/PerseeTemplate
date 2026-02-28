using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class ContrastPass : DepthPass
    {
        private Material mat;
        private float contrast;
        private float brightness;
        private bool ignoreBlack;
        private float blackThresh;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public float Contrast
        {
            get => contrast;
            set { contrast = Mathf.Clamp(value, 0f, 5f); }
        }

        public float Brightness
        {
            get => brightness;
            set { brightness = Mathf.Clamp(value, -1f, 1f); }
        }

        public bool IgnoreBlack
        {
            get => ignoreBlack;
            set { ignoreBlack = value; }
        }

        public float BlackThresh
        {
            get => blackThresh;
            set { blackThresh = Mathf.Clamp(value, 0f, 0.5f); }
        }

        public ContrastPass(string passName = "Contrast")
        {
            name = passName;
            mat  = new Material(Shader.Find("Custom/Contrast"));
            LoadPrefs();
            parameters = new List<DepthParameter>
            {
                new FloatParameter("Contrast",        () => contrast,     v => { Contrast = v; },     0.05f, 0f,  5f),
                new FloatParameter("Brightness",      () => brightness,   v => { Brightness = v; },   0.05f, -1f, 1f),
                new BoolParameter ("Ignore Black",    () => ignoreBlack,  v => { IgnoreBlack = v; }),
                new FloatParameter("Black Threshold", () => blackThresh,  v => { BlackThresh = v; },  0.01f, 0f,  0.5f),
            };
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            mat.SetFloat("_Contrast",    contrast);
            mat.SetFloat("_Brightness",  brightness);
            mat.SetFloat("_IgnoreBlack", ignoreBlack ? 1f : 0f);
            mat.SetFloat("_BlackThresh", blackThresh);
            Graphics.Blit(src, dst, mat);
        }

        public override void LoadPrefs()
        {
            Contrast    = PlayerPrefs.GetFloat(PrefKey("contrast"),    1.0f);
            Brightness  = PlayerPrefs.GetFloat(PrefKey("brightness"),  0.0f);
            IgnoreBlack = PlayerPrefs.GetInt  (PrefKey("ignoreBlack"), 0) == 1;
            BlackThresh = PlayerPrefs.GetFloat(PrefKey("blackThresh"), 0.01f);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetFloat(PrefKey("contrast"),    contrast);
            PlayerPrefs.SetFloat(PrefKey("brightness"),  brightness);
            PlayerPrefs.SetInt  (PrefKey("ignoreBlack"), ignoreBlack ? 1 : 0);
            PlayerPrefs.SetFloat(PrefKey("blackThresh"), blackThresh);
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}
