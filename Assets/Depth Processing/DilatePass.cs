using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class DilatePass : DepthPass
    {
        private Material mat;
        private int kernelSize;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public int KernelSize
        {
            get => kernelSize;
            set { kernelSize = Mathf.Clamp(value, 1, 10); mat?.SetInt("_KernelSize", kernelSize); }
        }

        public DilatePass(string passName = "Dilate")
        {
            name = passName;
            mat = new Material(Shader.Find("Custom/Dilate"));
            LoadPrefs();
            parameters = new List<DepthParameter>
            {
                new IntParameter("Kernel Size", () => kernelSize, v => { KernelSize = v; }, 1, 10)
            };
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            mat.SetInt("_KernelSize", kernelSize);
            mat.SetVector("_TexelSize", new Vector4(1.0f / src.width, 1.0f / src.height, 0, 0));
            Graphics.Blit(src, dst, mat);
        }

        public override void LoadPrefs()
        {
            KernelSize = PlayerPrefs.GetInt(PrefKey("kernelSize"), 1);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetInt(PrefKey("kernelSize"), kernelSize);
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}