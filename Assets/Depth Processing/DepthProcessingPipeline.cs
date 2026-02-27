using System.Collections.Generic;
using UnityEngine;

namespace DepthProcessing
{
    public class DepthProcessingPipeline : MonoBehaviour
    {
        public List<DepthPass> passes = new List<DepthPass>();

        private RenderTexture rtA;
        private RenderTexture rtB;
        private int rtWidth;
        private int rtHeight;
        private float saveTimer = 0f;
        private const float SAVE_INTERVAL = 1.0f;
        private bool dirty = false;

        private const string PASS_LIST_KEY = "Pipeline_PassList";

        public RenderTexture Output { get; private set; }

        public void Initialize(int width, int height)
        {
            rtWidth = width;
            rtHeight = height;
            rtA = CreateRT(RenderTextureFormat.RFloat);
            rtB = CreateRT(RenderTextureFormat.RFloat);

            foreach(var pass in passes)
            {
                pass.LoadPrefs();
                pass.LoadEnabled();
            }
        }

        // Returns true if a saved pass list was found and loaded, false if not
        public bool TryLoadPassList()
        {
            string saved = PlayerPrefs.GetString(PASS_LIST_KEY, "");
            if(string.IsNullOrEmpty(saved)) return false;

            string[] names = saved.Split(',');
            passes.Clear();

            foreach(var name in names)
            {
                var pass = CreatePassFromName(name.Trim());
                if(pass != null)
                    passes.Add(pass);
                else
                    Debug.LogWarning($"Pipeline: could not restore unknown pass type '{name}'");
            }

            return passes.Count > 0;
        }

        public void SavePassList()
        {
            var names = new List<string>();
            foreach(var pass in passes)
                names.Add(pass.name);
            PlayerPrefs.SetString(PASS_LIST_KEY, string.Join(",", names));
        }

        // Derives pass type from name prefix (handles "Erode", "Erode 2", etc.)
        private DepthPass CreatePassFromName(string name)
        {
            DepthPass pass = null;

            if     (name.StartsWith("DepthNormalize"))   pass = new DepthNormalizePass(name);
            else if(name.StartsWith("DepthCrop"))        pass = new DepthCropPass(name);
            else if(name.StartsWith("TemporalNoise"))    pass = new TemporalNoisePass(name);
            else if(name.StartsWith("Threshold"))        pass = new ThresholdPass(name);
            else if(name.StartsWith("Erode"))            pass = new ErodePass(name);
            else if(name.StartsWith("Dilate"))           pass = new DilatePass(name);
            else if(name.StartsWith("ZoomAndMove"))      pass = new ZoomAndMovePass(name);
            else if(name.StartsWith("Downsample"))       pass = new DownsamplePass(name);
            else if(name.StartsWith("Upsample"))         pass = new UpsamplePass(name);
            else if(name.StartsWith("UpscaleAndCenter")) pass = new UpscaleAndCenterPass(name);
            else if(name.StartsWith("SDFContours"))      pass = new SDFContoursPass(name);
            else if(name.StartsWith("RainbowTrails"))    pass = new RainbowTrailsPass(name);

            return pass;
        }

        private RenderTexture CreateRT(RenderTextureFormat format)
        {
            var rt = new RenderTexture(rtWidth, rtHeight, 0, format);
            rt.Create();
            return rt;
        }

        private void EnsureFormat(ref RenderTexture rt, RenderTextureFormat format)
        {
            if(rt.format != format)
            {
                rt.Release();
                rt = CreateRT(format);
            }
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        public RenderTexture Process(Texture input)
        {
            if(rtA == null) return null;

            Graphics.Blit(input, rtA);

            RenderTexture current = rtA;
            RenderTexture next = rtB;

            foreach(var pass in passes)
            {
                if(!pass.enabled) continue;

                EnsureFormat(ref next, pass.OutputFormat);
                pass.Process(current, next);

                var tmp = current;
                current = next;
                next = tmp;
            }

            Output = current;
            return Output;
        }

        void Update()
        {
            if(dirty)
            {
                saveTimer += Time.deltaTime;
                if(saveTimer >= SAVE_INTERVAL)
                {
                    SavePassList();
                    foreach(var pass in passes)
                    {
                        pass.SavePrefs();
                        pass.SaveEnabled();
                    }
                    PlayerPrefs.Save();
                    dirty = false;
                    saveTimer = 0f;
                    Debug.Log("Pipeline settings saved");
                }
            }
        }

        void OnDestroy()
        {
            if(rtA != null) rtA.Release();
            if(rtB != null) rtB.Release();

            foreach(var pass in passes)
            {
                if(pass is TemporalNoisePass temporal)
                    temporal.Dispose();
                if(pass is SDFContoursPass sdf)
                    sdf.Dispose();
                if(pass is DownsamplePass downsample)
                    downsample.Dispose();
                if(pass is UpsamplePass upsample)
                    upsample.Dispose();
                if(pass is UpscaleAndCenterPass upscaleandcenter)
                    upscaleandcenter.Dispose();
                if(pass is RainbowTrailsPass rainbow)
                    rainbow.Dispose();
            }
        }
    }
}
