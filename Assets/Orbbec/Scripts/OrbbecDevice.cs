using System.Collections;
using System.Runtime.InteropServices;
using Orbbec;
using UnityEngine;
using UnityEngine.Events;

namespace OrbbecUnity
{
    [System.Serializable]
    public class DeviceFoundEvent : UnityEvent<Device> {}

    public class OrbbecDevice : MonoBehaviour
    {
        public int deviceIndex;
        public DeviceFoundEvent onDeviceFound;

        private Context context;
        private Device device;

        public Device Device { get { return device; } }

        // Static instance reference so the static callback can reach this object
        private static OrbbecDevice _instance;

        void Start()
        {
            _instance = this;
            context = OrbbecContext.Instance.Context;
            if(OrbbecContext.Instance.HasInit)
            {
                context.SetDeviceChangedCallback(OnDeviceChangedStatic);
                StartCoroutine(TryImmediateQuery());
            }
        }

        private IEnumerator TryImmediateQuery()
        {
            yield return new WaitForSeconds(5f);
            Debug.Log("Trying immediate device query...");
            DeviceList deviceList = context.QueryDeviceList();
            uint count = deviceList.DeviceCount();
            Debug.Log($"Immediate query found: {count} devices");
            if (count > (uint)deviceIndex)
            {
                device = deviceList.GetDevice((uint)deviceIndex);
                LogDeviceInfo();
                deviceList.Dispose();
                onDeviceFound?.Invoke(device);
            }
            else
            {
                deviceList.Dispose();
                Debug.Log("No devices found in immediate query, waiting for callback...");
            }
        }

        [AOT.MonoPInvokeCallback(typeof(DeviceChangedCallback))]
        private static void OnDeviceChangedStatic(DeviceList added, DeviceList removed)
        {
            Debug.Log($"Device changed callback! Added: {added.DeviceCount()} Removed: {removed.DeviceCount()}");
            if (added.DeviceCount() > 0 && _instance != null && _instance.device == null)
            {
                _instance.device = added.GetDevice(0);
                _instance.LogDeviceInfo();
                added.Dispose();
                removed.Dispose();
                _instance.onDeviceFound?.Invoke(_instance.device);
            }
            else
            {
                added.Dispose();
                removed.Dispose();
            }
        }

        private void LogDeviceInfo()
        {
            DeviceInfo deviceInfo = device.GetDeviceInfo();
            Debug.LogFormat("Device found: {0} {1} {2:X} {3:X}",
                deviceInfo.Name(),
                deviceInfo.SerialNumber(),
                deviceInfo.Vid(),
                deviceInfo.Pid());
        }

        void OnDestroy()
        {
            if(device != null)
            {
                device.Dispose();
            }
        }
    }
}