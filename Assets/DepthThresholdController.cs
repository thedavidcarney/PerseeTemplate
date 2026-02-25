using UnityEngine;

public class DepthThresholdController : MonoBehaviour
{
    public float minDepth = 0.0f;
    public float maxDepth = 1.0f;
    public float stepSize = 0.5f;

    private Material mat;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (shift)
        {
            if (Input.GetKey(KeyCode.W)) minDepth += stepSize * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) minDepth -= stepSize * Time.deltaTime;
        }
        else
        {
            if (Input.GetKey(KeyCode.W)) maxDepth += stepSize * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) maxDepth -= stepSize * Time.deltaTime;
        }

        minDepth = Mathf.Clamp(minDepth, 0f, 1f);
        maxDepth = Mathf.Clamp(maxDepth, 0f, 1f);
        if (minDepth > maxDepth) minDepth = maxDepth;

        mat.SetFloat("_MinDepth", minDepth);
        mat.SetFloat("_MaxDepth", maxDepth);

        if (Time.frameCount % 60 == 0)
            Debug.Log($"Depth range: {minDepth / 6.5535f * 10000f:F0}mm - {maxDepth / 6.5535f * 10000f:F0}mm");
    }
}