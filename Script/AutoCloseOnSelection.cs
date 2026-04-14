using UnityEngine;

/// <summary>
/// 当选择框隐藏时自动关闭自身所在的 UI 面板
/// 挂载到需要自动关闭的 UI Panel 上
/// </summary>
public class AutoCloseOnSelection : MonoBehaviour
{
    private InputHandler _inputHandler;
    
    private void Start()
    {
        // 查找场景中的 InputHandler
        _inputHandler = FindObjectOfType<InputHandler>();
        
        if (_inputHandler == null)
        {
            Debug.LogWarning("[AutoCloseOnSelection] 没有找到 InputHandler!");
        }
    }
    
    /// <summary>
    /// 监听方法 - 在 HideSelectionBox 后调用
    /// </summary>
    public void OnSelectionBoxHidden()
    {
        // 关闭自身所在的 GameObject
        gameObject.SetActive(false);
        Debug.Log($"[AutoCloseOnSelection] 已关闭面板：{gameObject.name}");
    }
}