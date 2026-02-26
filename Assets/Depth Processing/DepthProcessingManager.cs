using UnityEngine;
using DepthProcessing;
using Orbbec;
using OrbbecUnity;

public class DepthProcessingManager : MonoBehaviour
{
    public OrbbecFrameSource frameSource;
    public Renderer displayQuad;

    private static readonly Vector3 QUAD_SCALE_DEPTH = new Vector3(16f, 12f, 1f);
    private static readonly Vector3 QUAD_SCALE_FULLSCREEN = new Vector3(20.5f, 12f, 1f);

    private DepthProcessingPipeline pipeline;
    public DepthProcessingPipeline Pipeline => pipeline;
    private Texture2D depthTexture;
    private Material displayMat;
    private Material stylizeMat;
    private bool pipelineReady = false;
    public bool PipelineReady => pipelineReady;
    private bool wasStyleActive = false;

    void Start()
    {
        pipeline = gameObject.AddComponent<DepthProcessingPipeline>();
        displayMat = new Material(Shader.Find("Custom/DepthDisplay"));
        stylizeMat = new Material(Shader.Find("Custom/PassThrough"));
        displayQuad.material = displayMat;
        displayQuad.transform.localScale = QUAD_SCALE_DEPTH;
    }

    void Update()
    {
        float fetchStart = Time.realtimeSinceStartup;
        var obDepthFrame = frameSource.GetDepthFrame();
        float fetchTime = Time.realtimeSinceStartup - fetchStart;

        if(fetchTime > 0.1f)
            Debug.LogWarning($"GetDepthFrame took {fetchTime:F3}s - potential block");

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

        float t1 = Time.realtimeSinceStartup;
        depthTexture.LoadRawTextureData(obDepthFrame.data);
        depthTexture.Apply();
        float t2 = Time.realtimeSinceStartup;

        var output = pipeline.Process(depthTexture);
        float t3 = Time.realtimeSinceStartup;

        bool hasActiveStylePass = HasActiveStylePass();
        SDFContoursPass sdf = hasActiveStylePass ? GetActiveSDFPass() : null;

        if(hasActiveStylePass != wasStyleActive)
        {
            wasStyleActive = hasActiveStylePass;
            if(hasActiveStylePass)
            {
                displayQuad.material = stylizeMat;
                displayQuad.transform.localScale = QUAD_SCALE_FULLSCREEN;
            }
            else
            {
                displayQuad.material = displayMat;
                displayQuad.transform.localScale = QUAD_SCALE_DEPTH;
            }
        }

        if(hasActiveStylePass && sdf?.FullResOutput != null && sdf.FullResOutput.IsCreated())
            stylizeMat.mainTexture = sdf.FullResOutput;
        else if(!hasActiveStylePass && output != null)
            displayMat.mainTexture = output;

        float t4 = Time.realtimeSinceStartup;

        if(Time.frameCount % 60 == 0)
        {
            Debug.Log($"FPS: {1.0f / Time.deltaTime:F1} | Passes: {pipeline.passes.Count} | GFX: {SystemInfo.graphicsDeviceType}");
            Debug.Log($"Upload:{(t2-t1)*1000:F1}ms Pipeline:{(t3-t2)*1000:F1}ms Display:{(t4-t3)*1000:F1}ms");
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

    private SDFContoursPass GetActiveSDFPass()
    {
        foreach(var pass in pipeline.passes)
        {
            if(pass is SDFContoursPass sdf && pass.enabled)
                return sdf;
        }
        return null;
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