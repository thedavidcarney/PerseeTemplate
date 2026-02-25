using UnityEngine;

public class DepthThresholdController : MonoBehaviour
{
    public float minDepth = 0.0f;
    public float maxDepth = 1.0f;
    public float stepSize = 0.5f;

    private Material mat;
    private float saveTimer = 0f;
    private const float SAVE_INTERVAL = 1.0f;
    private bool dirty = false;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        minDepth = PlayerPrefs.GetFloat("minDepth", 0.0f);
        maxDepth = PlayerPrefs.GetFloat("maxDepth", 1.0f);
        mat.SetFloat("_MinDepth", minDepth);
        mat.SetFloat("_MaxDepth", maxDepth);
    }

    void Update()
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool changed = false;

        if (shift)
        {
            if (Input.GetKey(KeyCode.W)) { minDepth += stepSize * Time.deltaTime; changed = true; }
            if (Input.GetKey(KeyCode.S)) { minDepth -= stepSize * Time.deltaTime; changed = true; }
        }
        else
        {
            if (Input.GetKey(KeyCode.W)) { maxDepth += stepSize * Time.deltaTime; changed = true; }
            if (Input.GetKey(KeyCode.S)) { maxDepth -= stepSize * Time.deltaTime; changed = true; }
        }

        if (changed)
        {
            minDepth = Mathf.Clamp(minDepth, 0f, 1f);
            maxDepth = Mathf.Clamp(maxDepth, 0f, 1f);
            if (minDepth > maxDepth) minDepth = maxDepth;

            mat.SetFloat("_MinDepth", minDepth);
            mat.SetFloat("_MaxDepth", maxDepth);
            dirty = true;
        }

        if (dirty)
        {
            saveTimer += Time.deltaTime;
            if (saveTimer >= SAVE_INTERVAL)
            {
                PlayerPrefs.SetFloat("minDepth", minDepth);
                PlayerPrefs.SetFloat("maxDepth", maxDepth);
                PlayerPrefs.Save();
                dirty = false;
                saveTimer = 0f;
                Debug.Log($"Saved depth range: {minDepth / 6.5535f * 10000f:F0}mm - {maxDepth / 6.5535f * 10000f:F0}mm");
            }
        }
    }
}