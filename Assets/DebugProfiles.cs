using Orbbec;
using OrbbecUnity;
using UnityEngine;

public class DebugProfiles : MonoBehaviour
{
    public OrbbecPipeline pipeline;

    void Start()
    {
        pipeline.onPipelineInit.AddListener(ListProfiles);
    }

    private void ListProfiles()
    {
        Debug.Log("=== Pipeline Init - Listing all profiles ===");
        ListSensor(pipeline.Pipeline, SensorType.OB_SENSOR_DEPTH, "DEPTH");
        ListSensor(pipeline.Pipeline, SensorType.OB_SENSOR_COLOR, "COLOR");
        ListSensor(pipeline.Pipeline, SensorType.OB_SENSOR_IR, "IR");
        ListSensor(pipeline.Pipeline, SensorType.OB_SENSOR_IR_LEFT, "IR_LEFT");
        ListSensor(pipeline.Pipeline, SensorType.OB_SENSOR_IR_RIGHT, "IR_RIGHT");
        Debug.Log("=== Starting pipeline ===");
    }

    private void ListSensor(Pipeline p, SensorType sensorType, string label)
    {
        try
        {
            var profileList = p.GetStreamProfileList(sensorType);
            uint count = profileList.ProfileCount();
            Debug.Log($"{label} profiles available: {count}");
            for (uint i = 0; i < count; i++)
            {
                var prof = profileList.GetProfile((int)i).As<VideoStreamProfile>();
                Debug.Log($"  {label} [{i}]: {prof.GetWidth()}x{prof.GetHeight()} @ {prof.GetFPS()}fps format:{prof.GetFormat()} streamType:{prof.GetStreamType()}");
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"{label} sensor not available: {e.Message}");
        }
    }
}