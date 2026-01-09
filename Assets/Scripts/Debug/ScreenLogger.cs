using UnityEngine;

public class ScreenLogger : MonoBehaviour
{
    // 单例模式，方便任何地方调用
    public static ScreenLogger Instance;

    // 要显示的文本
    private string currentLog = "";
    // 显示持续时间
    private float showDuration = 0f;
    // 字体样式
    private GUIStyle textStyle;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 设置一下字体样式，不然默认太小看不清
        textStyle = new GUIStyle();
        textStyle.fontSize = 30; // 字体搞大点
        textStyle.normal.textColor = Color.red; // 搞成红色显眼
    }

    // 核心功能：供外部调用
    public static void Show(string message, float duration = 2.0f)
    {
        if (Instance != null)
        {
            Instance.currentLog = message;
            Instance.showDuration = duration;
        }
        // 同时打印到Console，方便后续翻查
        Debug.Log($"[ScreenLog] {message}");
    }

    void Update()
    {
        // 倒计时，时间到了就清空文字
        if (showDuration > 0)
        {
            showDuration -= Time.deltaTime;
        }
        else
        {
            currentLog = "";
        }
    }

    // Unity自带的简易GUI绘制，每一帧都会运行
    void OnGUI()
    {
        if (!string.IsNullOrEmpty(currentLog))
        {
            // 在屏幕左上角 (x=20, y=20) 绘制文字
            GUI.Label(new Rect(20, 20, 1000, 100), currentLog, textStyle);
        }
    }
}