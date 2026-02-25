using System.Collections;
using Orbbec;
using OrbbecUnity;
using UnityEngine;

public class DepthImageView : MonoBehaviour
{
    public OrbbecFrameSource frameSource;

    private Texture2D depthTexture;
    private bool hasLogged = false;

    void Update()
    {
        var obDepthFrame = frameSource.GetDepthFrame();

        if(obDepthFrame == null || obDepthFrame.data == null || obDepthFrame.data.Length == 0)
            return;

        if(!hasLogged)
        {
            hasLogged = true;
            Debug.Log($"format:{obDepthFrame.format} width:{obDepthFrame.width} height:{obDepthFrame.height} dataLength:{obDepthFrame.data.Length}");
        }

        if(obDepthFrame.frameType != FrameType.OB_FRAME_DEPTH)
            return;

        if(depthTexture == null)
        {
            depthTexture = new Texture2D(obDepthFrame.width, obDepthFrame.height, TextureFormat.R16, false);
            GetComponent<Renderer>().material.mainTexture = depthTexture;
        }
        if(depthTexture.width != obDepthFrame.width || depthTexture.height != obDepthFrame.height)
        {
            depthTexture.Reinitialize(obDepthFrame.width, obDepthFrame.height);
        }

        depthTexture.LoadRawTextureData(obDepthFrame.data);
        depthTexture.Apply();
    }
}