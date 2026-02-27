using System.Collections.Generic;
using UnityEngine;
using DepthProcessing;

public class DepthProcessingUI : MonoBehaviour
{
    public DepthProcessingManager depthManager;

    private bool isOpen = false;
    private enum UIMode { PassView, ParameterView, AddPassView }
    private UIMode mode = UIMode.PassView;

    private int focusedPassIndex = 0;
    private int focusedParamIndex = 0;
    private int focusedAddPassIndex = 0;

    private const float PANEL_WIDTH = 350f;
    private const float PANEL_ALPHA = 0.85f;

    private GUIStyle headerStyle;
    private GUIStyle separatorStyle;
    private GUIStyle passStyle;
    private GUIStyle passSelectedStyle;
    private GUIStyle stylizeStyle;
    private GUIStyle stylizeSelectedStyle;
    private GUIStyle paramStyle;
    private GUIStyle paramSelectedStyle;
    private GUIStyle panelStyle;
    private bool stylesInitialized = false;

    private List<string> availableProcessingPassTypes = new List<string>
    {
        "DepthNormalize",
        "DepthCrop",
        "TemporalNoise",
        "Threshold",
        "Erode",
        "Dilate",
        "ZoomAndMove",
        "Downsample",
        "Upsample"
    };

    private List<string> availableStylizePassTypes = new List<string>
    {
        "SDFContours",
        "RainbowTrails"
    };

    private bool pendingRemove = false;

    private float keyRepeatDelay = 0.3f;
    private float keyRepeatRate = 0.08f;
    private Dictionary<KeyCode, float> keyHoldTimers = new Dictionary<KeyCode, float>();

    private DepthProcessingPipeline Pipeline => depthManager.Pipeline;

    void Update()
    {
        HandleKeyboard();
    }

    private bool GetKeyRepeating(KeyCode key)
    {
        if(Input.GetKeyDown(key))
        {
            keyHoldTimers[key] = Time.unscaledTime + keyRepeatDelay;
            return true;
        }
        if(Input.GetKey(key))
        {
            if(!keyHoldTimers.ContainsKey(key))
                keyHoldTimers[key] = Time.unscaledTime + keyRepeatDelay;
            if(Time.unscaledTime >= keyHoldTimers[key])
            {
                keyHoldTimers[key] = Time.unscaledTime + keyRepeatRate;
                return true;
            }
        }
        return false;
    }

    private void HandleKeyboard()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            if(!isOpen)
            {
                isOpen = true;
                return;
            }
            if(mode == UIMode.ParameterView)
            {
                mode = UIMode.PassView;
                pendingRemove = false;
                return;
            }
            if(mode == UIMode.AddPassView)
            {
                mode = UIMode.PassView;
                return;
            }
            isOpen = false;
            pendingRemove = false;
            return;
        }

        if(!isOpen) return;

        if(mode == UIMode.PassView)
            HandlePassViewKeys();
        else if(mode == UIMode.ParameterView)
            HandleParameterViewKeys();
        else if(mode == UIMode.AddPassView)
            HandleAddPassViewKeys();
    }

    private void HandlePassViewKeys()
    {
        var passes = Pipeline.passes;
        if(passes.Count == 0) return;

        if(Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            focusedPassIndex = Mathf.Min(focusedPassIndex + 1, passes.Count - 1);
            pendingRemove = false;
        }
        if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            focusedPassIndex = Mathf.Max(focusedPassIndex - 1, 0);
            pendingRemove = false;
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if(pendingRemove)
            {
                RemoveFocusedPass();
                pendingRemove = false;
            }
            else
            {
                mode = UIMode.ParameterView;
                focusedParamIndex = 0;
            }
        }

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if(shift && Input.GetKeyDown(KeyCode.UpArrow))
            MovePass(-1);
        if(shift && Input.GetKeyDown(KeyCode.DownArrow))
            MovePass(1);

        if(Input.GetKeyDown(KeyCode.Equals))
        {
            mode = UIMode.AddPassView;
            focusedAddPassIndex = 0;
        }

        if(Input.GetKeyDown(KeyCode.E))
        {
            passes[focusedPassIndex].enabled = !passes[focusedPassIndex].enabled;
            Pipeline.MarkDirty();
            pendingRemove = false;
        }

        if(Input.GetKeyDown(KeyCode.Minus))
            pendingRemove = true;
    }

    private void HandleParameterViewKeys()
    {
        var pass = Pipeline.passes[focusedPassIndex];
        var params_ = pass.GetParameters();
        if(params_.Count == 0) return;

        if(GetKeyRepeating(KeyCode.DownArrow) || GetKeyRepeating(KeyCode.S))
            focusedParamIndex = Mathf.Min(focusedParamIndex + 1, params_.Count - 1);
        if(GetKeyRepeating(KeyCode.UpArrow) || GetKeyRepeating(KeyCode.W))
            focusedParamIndex = Mathf.Max(focusedParamIndex - 1, 0);

        var param = params_[focusedParamIndex];
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        int steps = shift ? 2 : 1;

        if(GetKeyRepeating(KeyCode.RightArrow) || GetKeyRepeating(KeyCode.D))
        {
            for(int i = 0; i < steps; i++) param.Increment();
            Pipeline.MarkDirty();
        }
        if(GetKeyRepeating(KeyCode.LeftArrow) || GetKeyRepeating(KeyCode.A))
        {
            for(int i = 0; i < steps; i++) param.Decrement();
            Pipeline.MarkDirty();
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            param.Toggle();
            Pipeline.MarkDirty();
        }
    }

    private void HandleAddPassViewKeys()
    {
        int totalCount = availableProcessingPassTypes.Count + availableStylizePassTypes.Count;
        if(Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            focusedAddPassIndex = Mathf.Min(focusedAddPassIndex + 1, totalCount - 1);
        if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            focusedAddPassIndex = Mathf.Max(focusedAddPassIndex - 1, 0);
        if(Input.GetKeyDown(KeyCode.Space))
        {
            AddPassByIndex(focusedAddPassIndex);
            mode = UIMode.PassView;
        }
    }

    private void MovePass(int direction)
    {
        var passes = Pipeline.passes;
        int newIndex = focusedPassIndex + direction;
        if(newIndex < 0 || newIndex >= passes.Count) return;
        var tmp = passes[focusedPassIndex];
        passes[focusedPassIndex] = passes[newIndex];
        passes[newIndex] = tmp;
        focusedPassIndex = newIndex;
        Pipeline.MarkDirty();
    }

    private void RemoveFocusedPass()
    {
        Pipeline.passes.RemoveAt(focusedPassIndex);
        focusedPassIndex = Mathf.Clamp(focusedPassIndex, 0, Mathf.Max(0, Pipeline.passes.Count - 1));
        mode = UIMode.PassView;
        Pipeline.MarkDirty();
    }

    private void AddPass(string passType)
    {
        DepthPass newPass = passType switch
        {
            "DepthNormalize" => new DepthNormalizePass(),
            "DepthCrop" => new DepthCropPass(),
            "TemporalNoise" => new TemporalNoisePass(),
            "Threshold" => new ThresholdPass(),
            "Erode" => new ErodePass(),
            "Dilate" => new DilatePass(),
            "ZoomAndMove" => new ZoomAndMovePass(),
            "Downsample" => new DownsamplePass(),
            "Upsample" => new UpsamplePass(),
            "SDFContours" => new SDFContoursPass(),
            "RainbowTrails" => new RainbowTrailsPass(),
            _ => null
        };
        if(newPass == null) return;
        newPass.name = GetUniqueName(newPass.name);
        int insertAt = Mathf.Min(focusedPassIndex + 1, Pipeline.passes.Count);
        Pipeline.passes.Insert(insertAt, newPass);
        focusedPassIndex = insertAt;
        Pipeline.MarkDirty();
    }

    private void AddPassByIndex(int index)
    {
        if(index < availableProcessingPassTypes.Count)
            AddPass(availableProcessingPassTypes[index]);
        else
            AddPass(availableStylizePassTypes[index - availableProcessingPassTypes.Count]);
    }

    private void InitStyles()
    {
        if(stylesInitialized) return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = MakeTexture(1, 1, new Color(0.1f, 0.1f, 0.1f, PANEL_ALPHA));

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.white;
        headerStyle.fontSize = 14;

        separatorStyle = new GUIStyle(GUI.skin.label);
        separatorStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
        separatorStyle.fontSize = 11;

        passStyle = new GUIStyle(GUI.skin.label);
        passStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        passStyle.fontSize = 13;

        passSelectedStyle = new GUIStyle(GUI.skin.label);
        passSelectedStyle.normal.textColor = Color.yellow;
        passSelectedStyle.fontStyle = FontStyle.Bold;
        passSelectedStyle.fontSize = 13;

        stylizeStyle = new GUIStyle(GUI.skin.label);
        stylizeStyle.normal.textColor = new Color(0.6f, 0.9f, 0.6f);
        stylizeStyle.fontSize = 13;

        stylizeSelectedStyle = new GUIStyle(GUI.skin.label);
        stylizeSelectedStyle.normal.textColor = Color.green;
        stylizeSelectedStyle.fontStyle = FontStyle.Bold;
        stylizeSelectedStyle.fontSize = 13;

        paramStyle = new GUIStyle(GUI.skin.label);
        paramStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        paramStyle.fontSize = 13;

        paramSelectedStyle = new GUIStyle(GUI.skin.label);
        paramSelectedStyle.normal.textColor = Color.cyan;
        paramSelectedStyle.fontStyle = FontStyle.Bold;
        paramSelectedStyle.fontSize = 13;

        stylesInitialized = true;
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        var tex = new Texture2D(width, height);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if(!isOpen) return;
        InitStyles();

        float panelHeight = Screen.height * 0.8f;
        float panelY = Screen.height * 0.1f;
        Rect panelRect = new Rect(20, panelY, PANEL_WIDTH, panelHeight);

        GUI.Box(panelRect, "", panelStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 10, panelRect.y + 10, panelRect.width - 20, panelRect.height - 20));

        if(mode == UIMode.PassView)
            DrawPassView();
        else if(mode == UIMode.ParameterView)
            DrawParameterView();
        else if(mode == UIMode.AddPassView)
            DrawAddPassView();

        GUILayout.EndArea();
    }

    private void DrawPassView()
    {
        GUILayout.Label("DEPTH PIPELINE", headerStyle);
        GUILayout.Label("↑↓ navigate  Space: edit  E: toggle  ±: add/remove  Shift+↑↓: reorder  Tab: close", paramStyle);
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Depth Res:", paramStyle, GUILayout.Width(80));

        bool isHigh = depthManager.CurrentProfileWidth == 1280;

        GUIStyle lowStyle  = isHigh ? paramStyle        : paramSelectedStyle;
        GUIStyle highStyle = isHigh ? paramSelectedStyle : paramStyle;

        if(GUILayout.Button("640x400",  lowStyle,  GUILayout.Width(80)))
            depthManager.RestartWithProfile(640);

        if(GUILayout.Button("1280x800", highStyle, GUILayout.Width(90)))
            depthManager.RestartWithProfile(1280);

        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        var passes = Pipeline.passes;
        bool separatorDrawn = false;

        for(int i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];
            bool isStylePass = pass is StylePass;

            // Draw separator before first stylize pass
            if(isStylePass && !separatorDrawn)
            {
                separatorDrawn = true;
                GUILayout.Space(5);
                GUILayout.Label("── STYLIZE ──────────────────", separatorStyle);
                GUILayout.Space(5);
            }

            bool isFocused = i == focusedPassIndex;
            GUIStyle style = isFocused
                ? (isStylePass ? stylizeSelectedStyle : passSelectedStyle)
                : (isStylePass ? stylizeStyle : passStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(isFocused ? "▶" : "  ", style, GUILayout.Width(20));

            bool newEnabled = GUILayout.Toggle(pass.enabled, "", GUILayout.Width(20));
            if(newEnabled != pass.enabled)
            {
                pass.enabled = newEnabled;
                Pipeline.MarkDirty();
            }

            if(GUILayout.Button(pass.name, style))
            {
                focusedPassIndex = i;
                mode = UIMode.ParameterView;
                focusedParamIndex = 0;
            }

            GUILayout.FlexibleSpace();

            if(GUILayout.Button("^", GUILayout.Width(25)))
            {
                focusedPassIndex = i;
                MovePass(-1);
            }
            if(GUILayout.Button("v", GUILayout.Width(25)))
            {
                focusedPassIndex = i;
                MovePass(1);
            }
            if(GUILayout.Button("X", GUILayout.Width(25)))
            {
                focusedPassIndex = i;
                pendingRemove = true;
            }

            GUILayout.EndHorizontal();

            if(isFocused && pendingRemove)
                GUILayout.Label("  Press Space to confirm remove", paramSelectedStyle);
        }

        GUILayout.Space(10);
        if(GUILayout.Button("+ Add Pass"))
        {
            mode = UIMode.AddPassView;
            focusedAddPassIndex = 0;
        }
    }

    private void DrawParameterView()
    {
        var pass = Pipeline.passes[focusedPassIndex];
        bool isStylePass = pass is StylePass;
        GUIStyle titleStyle = isStylePass ? stylizeSelectedStyle : headerStyle;

        GUILayout.Label(pass.name.ToUpper(), titleStyle);
        GUILayout.Label("↑↓ navigate  ←→ change  Shift: double step  Space: toggle  Tab: back", paramStyle);
        GUILayout.Space(10);

        var parameters = pass.GetParameters();
        for(int i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            bool isFocused = i == focusedParamIndex;
            GUIStyle style = isFocused ? paramSelectedStyle : paramStyle;
            bool isBool = param is BoolParameter;

            GUILayout.BeginHorizontal();
            GUILayout.Label(isFocused ? "▶" : "  ", style, GUILayout.Width(20));
            GUILayout.Label(param.DisplayName, style, GUILayout.Width(150));
            GUILayout.Label(param.DisplayValue, style, GUILayout.Width(80));

            if(!isBool)
            {
                if(GUILayout.Button("-", GUILayout.Width(25)))
                {
                    focusedParamIndex = i;
                    param.Decrement();
                    Pipeline.MarkDirty();
                }
                if(GUILayout.Button("+", GUILayout.Width(25)))
                {
                    focusedParamIndex = i;
                    param.Increment();
                    Pipeline.MarkDirty();
                }
            }
            else
            {
                if(GUILayout.Button("toggle", GUILayout.Width(55)))
                {
                    focusedParamIndex = i;
                    param.Toggle();
                    Pipeline.MarkDirty();
                }
            }

            GUILayout.EndHorizontal();
        }
    }

    private void DrawAddPassView()
    {
        GUILayout.Label("ADD PASS", headerStyle);
        GUILayout.Label("↑↓ navigate  Space: add  Tab: cancel", paramStyle);
        GUILayout.Space(10);

        GUILayout.Label("── PROCESSING ───────────────", separatorStyle);
        GUILayout.Space(5);

        int globalIndex = 0;
        foreach(var passType in availableProcessingPassTypes)
        {
            bool isFocused = globalIndex == focusedAddPassIndex;
            GUIStyle style = isFocused ? passSelectedStyle : passStyle;
            GUILayout.BeginHorizontal();
            GUILayout.Label(isFocused ? "▶" : "  ", style, GUILayout.Width(20));
            if(GUILayout.Button(passType, style))
            {
                AddPass(passType);
                mode = UIMode.PassView;
            }
            GUILayout.EndHorizontal();
            globalIndex++;
        }

        GUILayout.Space(5);
        GUILayout.Label("── STYLIZE ──────────────────", separatorStyle);
        GUILayout.Space(5);

        foreach(var passType in availableStylizePassTypes)
        {
            bool isFocused = globalIndex == focusedAddPassIndex;
            GUIStyle style = isFocused ? stylizeSelectedStyle : stylizeStyle;
            GUILayout.BeginHorizontal();
            GUILayout.Label(isFocused ? "▶" : "  ", style, GUILayout.Width(20));
            if(GUILayout.Button(passType, style))
            {
                AddPass(passType);
                mode = UIMode.PassView;
            }
            GUILayout.EndHorizontal();
            globalIndex++;
        }
    }

    private string GetUniqueName(string baseName)
    {
        var existingNames = new HashSet<string>();
        foreach(var p in Pipeline.passes)
            existingNames.Add(p.name);

        if(!existingNames.Contains(baseName))
            return baseName;

        int i = 2;
        while(existingNames.Contains($"{baseName} {i}"))
            i++;

        return $"{baseName} {i}";
    }
}