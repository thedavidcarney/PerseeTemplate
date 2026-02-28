using UnityEngine;
using DepthProcessing;
using Orbbec;
using OrbbecUnity;
using System.Linq;

public class DepthProcessingManager : MonoBehaviour
{
    public OrbbecFrameSource frameSource;
    public OrbbecPipeline orbbecPipeline;
    public Renderer displayQuad;

    private static readonly Vector3 QUAD_SCALE_DEPTH      = new Vector3(16f,   12f, 1f);
    private static readonly Vector3 QUAD_SCALE_FULLSCREEN = new Vector3(20.5f, 12f, 1f);

    private DepthProcessingPipeline pipeline;
    public DepthProcessingPipeline Pipeline => pipeline;
    private Texture2D depthTexture;
    private Material displayMat;
    private Material stylizeMat;
    private bool pipelineReady = false;
    public bool PipelineReady => pipelineReady;
    private bool wasStyleActive = false;

    // Current depth profile width — persisted so UI can read it
    private int currentProfileWidth = 640;
    public int CurrentProfileWidth => currentProfileWidth;

    private const string PROFILE_WIDTH_KEY = "Pipeline_ProfileWidth";

    void Start()
    {
        pipeline   = gameObject.AddComponent<DepthProcessingPipeline>();
        displayMat = new Material(Shader.Find("Custom/DepthDisplay"));
        stylizeMat = new Material(Shader.Find("Custom/PassThrough"));
        displayQuad.material = displayMat;
        displayQuad.transform.localScale = QUAD_SCALE_DEPTH;

        currentProfileWidth = PlayerPrefs.GetInt(PROFILE_WIDTH_KEY, 640);
        if(currentProfileWidth != 640)
            RestartWithProfile(currentProfileWidth);
    }

    void Update()
    {
        if(pipelineReady) WireMotionVectorSources();

        float fetchStart = Time.realtimeSinceStartup;
        var obDepthFrame = frameSource.GetDepthFrame();
        float fetchTime  = Time.realtimeSinceStartup - fetchStart;

        if(fetchTime > 0.1f)
            Debug.LogWarning($"GetDepthFrame took {fetchTime:F3}s - potential block");

        if(obDepthFrame == null || obDepthFrame.data == null || obDepthFrame.data.Length == 0)
            return;
        if(obDepthFrame.frameType != FrameType.OB_FRAME_DEPTH)
            return;

        if(!pipelineReady)
        {
            // Wait until we get a frame at the expected resolution
            if(obDepthFrame.width != currentProfileWidth)
                return;

            depthTexture = new Texture2D(obDepthFrame.width, obDepthFrame.height, TextureFormat.R16, false);

            // Restore saved pass list, fall back to defaults on first run
            if(!pipeline.TryLoadPassList())
            {
                pipeline.passes.Add(new DepthNormalizePass());
                pipeline.passes.Add(new DepthCropPass());
                pipeline.passes.Add(new ThresholdPass());
                pipeline.passes.Add(new ErodePass());
                pipeline.passes.Add(new DilatePass());
                pipeline.passes.Add(new UpscaleAndCenterPass());
                pipeline.passes.Add(new SDFContoursPass());
            }

            pipeline.Initialize(obDepthFrame.width, obDepthFrame.height);
            WireMotionVectorSources();
            pipelineReady = true;

            Debug.Log($"Pipeline initialized at {obDepthFrame.width}x{obDepthFrame.height} with {pipeline.passes.Count} passes");
        }

        // Guard against frames arriving during resolution switch before reinitialization
        if(depthTexture == null) return;

        // Guard against frame size mismatch during stream transition
if(obDepthFrame.width != depthTexture.width || obDepthFrame.height != depthTexture.height) return;

        float t1 = Time.realtimeSinceStartup;
        depthTexture.LoadRawTextureData(obDepthFrame.data);
        depthTexture.Apply();
        float t2 = Time.realtimeSinceStartup;

        var output = pipeline.Process(depthTexture);
        float t3   = Time.realtimeSinceStartup;

        bool      hasActiveStylePass = HasActiveStylePass();
        StylePass activeStylePass    = GetActiveStylePass();

        if(hasActiveStylePass != wasStyleActive)
        {
            wasStyleActive = hasActiveStylePass;
            if(hasActiveStylePass)
            {
                displayQuad.enabled = false;
                displayQuad.material = stylizeMat;
                displayQuad.transform.localScale = QUAD_SCALE_FULLSCREEN;
            }
            else
            {
                displayQuad.enabled = true;
                displayQuad.material = displayMat;
                displayQuad.transform.localScale = QUAD_SCALE_DEPTH;
            }
        }

        if(hasActiveStylePass)
        {
            if(activeStylePass?.FullResOutput != null && activeStylePass.FullResOutput.IsCreated())
            {
                stylizeMat.mainTexture = activeStylePass.FullResOutput;
                if(!displayQuad.enabled) displayQuad.enabled = true;
            }
        }
        else if(output != null)
        {
            displayMat.mainTexture = output;
        }

        float t4 = Time.realtimeSinceStartup;

        // if(Time.frameCount % 60 == 0)
        // {
        //     Debug.Log($"FPS: {1.0f / Time.deltaTime:F1} | Passes: {pipeline.passes.Count} | GFX: {SystemInfo.graphicsDeviceType}");
        //     Debug.Log($"Upload:{(t2-t1)*1000:F1}ms Pipeline:{(t3-t2)*1000:F1}ms Display:{(t4-t3)*1000:F1}ms");
        // }
    }

    public void RestartWithProfile(int width)
    {
        currentProfileWidth = width;
        PlayerPrefs.SetInt(PROFILE_WIDTH_KEY, width);
        PlayerPrefs.Save();
        Debug.Log($"Restarting pipeline with profile width {width}");

        // Stop Orbbec stream
        orbbecPipeline.StopPipeline();

        // Swap the matching profile to the front of the array
        var profiles = orbbecPipeline.orbbecProfiles;
        for(int i = 0; i < profiles.Length; i++)
        {
            if(profiles[i].width == width)
            {
                var tmp     = profiles[0];
                profiles[0] = profiles[i];
                profiles[i] = tmp;
                break;
            }
        }

        // Reset pipeline — next Update will reinitialize from new frame dimensions
        orbbecPipeline.RebuildConfig();
        pipeline.passes.Clear();
        pipelineReady = false;
        depthTexture  = null;

        // Restart Orbbec stream
        orbbecPipeline.StartPipeline();
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

    private StylePass GetActiveStylePass()
    {
        foreach(var pass in pipeline.passes)
        {
            if(pass is StylePass sp && pass.enabled && sp.FullResOutput != null)
                return sp;
        }
        return null;
    }

    private void WireMotionVectorSources()
    {
        var mvPass = pipeline.passes.OfType<MotionVectorPass>().FirstOrDefault();
        if(mvPass == null) return;

        foreach(var pass in pipeline.passes)
        {
            if(pass is MotionVectorViewerPass viewer && viewer.Source == null)
                viewer.Source = mvPass;
            if(pass is RainbowTrailsPass rainbow && rainbow.MotionVectorSource == null)
                rainbow.MotionVectorSource = mvPass;
            if(pass is FluidTrailsPass fluid && fluid.MotionVectorSource == null)
                fluid.MotionVectorSource = mvPass;
        }
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
