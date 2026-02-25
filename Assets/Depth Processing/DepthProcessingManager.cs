using UnityEngine;
using DepthProcessing;
using Orbbec;
using OrbbecUnity;

public class DepthProcessingManager : MonoBehaviour
{
    public OrbbecFrameSource frameSource;
    public Renderer displayQuad;

    private DepthProcessingPipeline pipeline;
    public DepthProcessingPipeline Pipeline => pipeline;
    private Texture2D depthTexture;
    private Material displayMat;
    private bool pipelineReady = false;
    public bool PipelineReady => pipelineReady;

    void Start()
    {
        pipeline = gameObject.AddComponent<DepthProcessingPipeline>();
        displayMat = new Material(Shader.Find("Custom/DepthDisplay"));
        displayQuad.material = displayMat;
    }

    void Update()
    {
        var obDepthFrame = frameSource.GetDepthFrame();

        if(obDepthFrame == null || obDepthFrame.data == null || obDepthFrame.data.Length == 0)
            return;
        if(obDepthFrame.frameType != FrameType.OB_FRAME_DEPTH)
            return;

        if(!pipelineReady)
        {
            depthTexture = new Texture2D(obDepthFrame.width, obDepthFrame.height, TextureFormat.R16, false);

            pipeline.passes.Add(new DepthNormalizePass());
            pipeline.passes.Add(new DepthCropPass());
            pipeline.passes.Add(new ThresholdPass());
            pipeline.passes.Add(new ErodePass());
            pipeline.passes.Add(new DilatePass());
            pipeline.passes.Add(new SDFContoursPass());

            pipeline.Initialize(obDepthFrame.width, obDepthFrame.height);
            pipelineReady = true;
        }

        depthTexture.LoadRawTextureData(obDepthFrame.data);
        depthTexture.Apply();

        var output = pipeline.Process(depthTexture);

        // Show quad only when no active stylize pass
        bool hasActiveStylePass = HasActiveStylePass();
        displayQuad.enabled = !hasActiveStylePass;

        if(!hasActiveStylePass && output != null)
            displayMat.mainTexture = output;

        if(Time.frameCount % 60 == 0)
        {
            Debug.Log($"FPS: {1.0f / Time.deltaTime:F1} | Passes: {pipeline.passes.Count}");
        }
    }

    private bool HasActiveStylePass()
    {
        foreach(var pass in pipeline.passes)
        {
            if(pass is StylePass && pass.enabled)
                return true;
        }
        return false;
    }

    void OnEnable()
    {
        Application.logMessageReceived += OnLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= OnLog;
    }

    void OnLog(string condition, string stackTrace, LogType type)
    {
        if(type == LogType.Error || type == LogType.Exception)
            Debug.Log($"ERROR CAUGHT: {condition} | {stackTrace}");
    }
}