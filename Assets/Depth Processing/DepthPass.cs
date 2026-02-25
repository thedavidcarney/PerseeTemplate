using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public abstract class DepthPass
    {
        public string name;
        public bool enabled = true;

        public virtual RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        protected string PrefKey(string param) => $"{name}_{param}";

        public abstract void Process(RenderTexture src, RenderTexture dst);
        public abstract void LoadPrefs();
        public abstract void SavePrefs();
        public abstract List<DepthParameter> GetParameters();
    }
}