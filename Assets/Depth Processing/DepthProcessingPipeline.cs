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

        public RenderTexture Output { get; private set; }

        public void Initialize(int width, int height)
        {
            rtWidth = width;
            rtHeight = height;
            rtA = CreateRT(RenderTextureFormat.RFloat);
            rtB = CreateRT(RenderTextureFormat.RFloat);

            foreach(var pass in passes)
                pass.LoadPrefs();
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
                    foreach(var pass in passes)
                        pass.SavePrefs();
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
            }
        }
    }
}