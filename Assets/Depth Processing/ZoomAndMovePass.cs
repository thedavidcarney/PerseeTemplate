using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public class ZoomAndMovePass : DepthPass
    {
        private Material mat;
        private float offsetX;
        private float offsetY;
        private float zoom;
        private float scaleX;
        private float scaleY;
        private float rotation;
        private List<DepthParameter> parameters;

        public override RenderTextureFormat OutputFormat => RenderTextureFormat.RFloat;

        public float OffsetX
        {
            get => offsetX;
            set { offsetX = value; mat?.SetFloat("_OffsetX", value); }
        }

        public float OffsetY
        {
            get => offsetY;
            set { offsetY = value; mat?.SetFloat("_OffsetY", value); }
        }

        public float Zoom
        {
            get => zoom;
            set { zoom = Mathf.Max(0.01f, value); mat?.SetFloat("_Zoom", zoom); }
        }

        public float ScaleX
        {
            get => scaleX;
            set { scaleX = Mathf.Max(0.01f, value); mat?.SetFloat("_ScaleX", scaleX); }
        }

        public float ScaleY
        {
            get => scaleY;
            set { scaleY = Mathf.Max(0.01f, value); mat?.SetFloat("_ScaleY", scaleY); }
        }

        public float Rotation
        {
            get => rotation;
            set { rotation = value; mat?.SetFloat("_Rotation", value); }
        }

        public ZoomAndMovePass(string passName = "ZoomAndMove")
        {
            name = passName;
            mat = new Material(Shader.Find("Custom/ZoomAndMove"));
            LoadPrefs();
            parameters = new List<DepthParameter>
            {
                new FloatParameter("Offset X",  () => offsetX,  v => { OffsetX = v; },   0.005f, -1f,    1f),
                new FloatParameter("Offset Y",  () => offsetY,  v => { OffsetY = v; },   0.005f, -1f,    1f),
                new FloatParameter("Zoom",      () => zoom,     v => { Zoom = v; },       0.01f,  0.1f,   5f),
                new FloatParameter("Scale X",   () => scaleX,   v => { ScaleX = v; },    0.01f,  0.1f,   3f),
                new FloatParameter("Scale Y",   () => scaleY,   v => { ScaleY = v; },    0.01f,  0.1f,   3f),
                new FloatParameter("Rotation",  () => rotation, v => { Rotation = v; },  0.1f,  -180f, 180f),
            };
        }

        public override void Process(RenderTexture src, RenderTexture dst)
        {
            mat.SetFloat("_OffsetX",   offsetX);
            mat.SetFloat("_OffsetY",   offsetY);
            mat.SetFloat("_Zoom",      zoom);
            mat.SetFloat("_ScaleX",    scaleX);
            mat.SetFloat("_ScaleY",    scaleY);
            mat.SetFloat("_Rotation",  rotation);
            Graphics.Blit(src, dst, mat);
        }

        public override void LoadPrefs()
        {
            OffsetX  = PlayerPrefs.GetFloat(PrefKey("offsetX"),   0f);
            OffsetY  = PlayerPrefs.GetFloat(PrefKey("offsetY"),   0f);
            Zoom     = PlayerPrefs.GetFloat(PrefKey("zoom"),      1f);
            ScaleX   = PlayerPrefs.GetFloat(PrefKey("scaleX"),   1f);
            ScaleY   = PlayerPrefs.GetFloat(PrefKey("scaleY"),   1f);
            Rotation = PlayerPrefs.GetFloat(PrefKey("rotation"), 0f);
        }

        public override void SavePrefs()
        {
            PlayerPrefs.SetFloat(PrefKey("offsetX"),   offsetX);
            PlayerPrefs.SetFloat(PrefKey("offsetY"),   offsetY);
            PlayerPrefs.SetFloat(PrefKey("zoom"),      zoom);
            PlayerPrefs.SetFloat(PrefKey("scaleX"),    scaleX);
            PlayerPrefs.SetFloat(PrefKey("scaleY"),    scaleY);
            PlayerPrefs.SetFloat(PrefKey("rotation"),  rotation);
        }

        public override List<DepthParameter> GetParameters() => parameters;
    }
}
