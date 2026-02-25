using System.Collections;
using Orbbec;
using UnityEngine;

namespace OrbbecUnity
{
    public class OrbbecContext : MonoBehaviour
    {
        private static OrbbecContext instance;
        private bool hasInit;
        private Context context;

        public static OrbbecContext Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = FindAnyObjectByType<OrbbecContext>();
 
                    if (instance == null)
                    {
                        var singletonObject = new GameObject();
                        instance = singletonObject.AddComponent<OrbbecContext>();
                        singletonObject.name = typeof(OrbbecContext).ToString();
                        DontDestroyOnLoad(singletonObject);
                    }
                }

                return instance;
            }
        }

        public bool HasInit
        {
            get
            {
                return hasInit;
            }
        }

        public Context Context
        {
            get
            {
                return context;
            }
        }

        void Awake()
        {
            if (!hasInit)
            {
                InitSDK();
            }
        }

        void OnDestroy()
        {
            if(hasInit)
            {
                context.Dispose();
            }
            hasInit = false;
        }

        private void InitSDK()
        {
            Debug.LogFormat("Orbbec SDK version: {0}.{1}.{2}",
                Version.GetMajorVersion(),
                Version.GetMinorVersion(),
                Version.GetPatchVersion());

        #if !UNITY_EDITOR && UNITY_ANDROID
            string configPath = CopyConfigToCache("OrbbecSDKConfig_v1.0.xml");
            Debug.Log($"Loading Orbbec config from: {configPath}");
            context = new Context(configPath);
            AndroidDeviceManager.Init();
        #else
            context = new Context();
        #endif
            hasInit = true;
        }

private string CopyConfigToCache(string filename)
{
    string destPath = System.IO.Path.Combine(Application.persistentDataPath, filename);
    
    try
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject assetManager = activity.Call<AndroidJavaObject>("getAssets");
        
        // List all files in assets root
        string[] files = assetManager.Call<string[]>("list", "");
        Debug.Log("Assets root contents: " + string.Join(", ", files));
        
        AndroidJavaObject inputStream = assetManager.Call<AndroidJavaObject>("open", filename);
        System.Collections.Generic.List<byte> bytes = new System.Collections.Generic.List<byte>();
        int b;
        while ((b = inputStream.Call<int>("read")) != -1)
        {
            bytes.Add((byte)b);
        }
        inputStream.Call("close");
        System.IO.File.WriteAllBytes(destPath, bytes.ToArray());
        Debug.Log($"Config copied successfully, {bytes.Count} bytes to: {destPath}");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Failed to copy config: {e.Message}");
    }
    
    return destPath;
}
    }
}