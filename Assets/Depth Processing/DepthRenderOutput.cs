using UnityEngine;
using DepthProcessing;

public class DepthRenderOutput : MonoBehaviour
{
    public DepthProcessingManager depthManager;

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if(depthManager == null || !depthManager.PipelineReady)
        {
            Graphics.Blit(src, dst);
            return;
        }

        foreach(var pass in depthManager.Pipeline.passes)
        {
            if(pass is SDFContoursPass sdf && pass.enabled)
            {
                if(sdf.FullResOutput != null)
                {
                    Graphics.Blit(sdf.FullResOutput, dst);
                    return;
                }
            }
        }

        Graphics.Blit(src, dst);
    }
}