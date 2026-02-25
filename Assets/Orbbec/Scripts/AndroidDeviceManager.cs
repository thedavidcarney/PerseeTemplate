using UnityEngine;

namespace OrbbecUnity
{
    public class AndroidDeviceManager
    {
        private static AndroidJavaClass UsbPermissionUtil;

        public static void Init()
        {
            Debug.Log("init android device");
            Application.RequestUserAuthorization(UserAuthorization.WebCam);

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Request runtime permissions
            try
            {
                // Request runtime permissions using native Android API
                activity.Call("requestPermissions",
                    new string[] {
                        "android.permission.WRITE_EXTERNAL_STORAGE",
                        "android.permission.READ_EXTERNAL_STORAGE",
                        "android.permission.CAMERA"
                    }, 1001);
                Debug.Log("Permission request sent");
                Debug.Log("Permission request sent");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Permission request failed: {e.Message}");
            }

            // Wait for user to respond to permission dialogs
            System.Threading.Thread.Sleep(3000);

            UsbPermissionUtil = new AndroidJavaClass("com.orbbec.obsensor.usbdevice.UsbPermissionUtil");
            UsbPermissionUtil.CallStatic("waitForUsbDevice", activity);
            Debug.Log("android device has init");
        }

        public static void Close()
        {
            Debug.Log("close android device");
            UsbPermissionUtil.CallStatic("closeUsbDevice");
        }
    }
}