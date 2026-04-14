// InputHandler.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class InputHandler : MonoBehaviour
{
    [Header("Camera Settings")]
    private Camera _mainCamera;
    
    [Header("Drag Settings")]
    private Vector2 _dragStartPos;
    private Vector2 _dragCurrentPos;
    private bool _isDragging = false;
    private const float DRAG_THRESHOLD = 10f;
    
    [Header("Selection Box")]
    private RectTransform _selectionBox;
    private Image _selectionBoxImage;
    private Canvas _canvas;

    // 使用 Awake 而不是构造函数
    [Header("UI Block Check")]
    private GraphicRaycaster _graphicRaycaster;
    private PointerEventData _pointerEventData;
    private EventSystem _eventSystem;


    private void Awake()
    {
        // 获取主相机
        _mainCamera = Camera.main;
        
        // 获取或创建 Canvas
        FindOrCreateCanvas();
        
        // 创建选择框
        CreateSelectionBox();
    }

private void InitializeUICheck()
    {
        // 查找场景中的 EventSystem
        _eventSystem = FindObjectOfType<EventSystem>();
        
        // 如果没找到，创建一个新的
        if (_eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            _eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        // 获取 Canvas 上的 GraphicRaycaster
        _graphicRaycaster = _canvas.GetComponent<GraphicRaycaster>();
        if (_graphicRaycaster == null)
        {
            _graphicRaycaster = _canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
        
        // 创建 PointerEventData
        _pointerEventData = new PointerEventData(_eventSystem);
    }


 private bool IsPointerOverUI()
    {
        if (EventSystem.current != null && 
            EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }
        
        return false;
    }
    // Unity Update 循环中调用
    private void Update()
    {
        HandleInput();
    }

    public void HandleInput()
    {
        if (IsPointerOverUI())
        {
            return;
        }
        // 鼠标按下
        if (Input.GetMouseButtonDown(0))
        {
            _dragStartPos = Input.mousePosition;
            _dragCurrentPos = _dragStartPos;
            _isDragging = false;
            HideSelectionBox();
        }

        // 鼠标拖动
        if (Input.GetMouseButton(0))
        {
            _dragCurrentPos = Input.mousePosition;
            float dragDistance = Vector2.Distance(_dragStartPos, _dragCurrentPos);

            // 超过阈值则开始框选
            if (dragDistance > DRAG_THRESHOLD)
            {
                _isDragging = true;
                UpdateSelectionBox();
            }
        }

        // 鼠标释放
        if (Input.GetMouseButtonUp(0))
        {
            if (_isDragging)
            {
                HandleBoxSelection();
            }
            else
            {
                HandleSingleSelection();
            }

            HideSelectionBox();
        }
        
        // 右键点击：移动选中单位
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClickMove();
        }
    }

    // 右键移动处理
    private void HandleRightClickMove()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            // 获取所有选中的单位
            var selectedUnits = SelectionManager.Instance.SelectedUnits;
            
            if (selectedUnits.Count > 0)
            {
                // 让所有选中的单位移动到右键点击的位置
                foreach (var unit in selectedUnits)
                {
                    MovementComponent movement = unit.GetComponent<MovementComponent>();
                    if (movement != null)
                    {
                        movement.MoveTo(hit.point);
                        unit.SetState(UnitState.Moving);
                    }
                }
                
                // 可选：显示移动目标指示器
                ShowMoveTargetIndicator(hit.point);
            }
        }
    }

    // 单选处理
    private void HandleSingleSelection()
    {
        Unit clickedUnit = RaycastUnit(_dragStartPos);

        if (clickedUnit != null)
        {
            bool alreadySelected = SelectionManager.Instance.SelectedUnits.Contains(clickedUnit);
            
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (alreadySelected)
                {
                    SelectionManager.Instance.RemoveFromSelection(clickedUnit);
                }
                else
                {
                    SelectionManager.Instance.AddToSelection(clickedUnit);
                }
            }
            else
            {
                SelectionManager.Instance.SelectUnit(clickedUnit);
            }
        }
        else
        {
            // 点击空白处，检查是否有 TaskBuilding 处于 workable 状态
            DisableAllTaskBuildingsWorkable();
            
            SelectionManager.Instance.ClearSelection();
        }
    }
    
    /// <summary>
    /// 禁用所有 TaskBuilding 的 workable 状态
    /// </summary>
     private void DisableAllTaskBuildingsWorkable()
    {
        var allBuildings = FindObjectsOfType<TaskBuilding>();
        foreach (var building in allBuildings)
        {
            if (building != null)
            {
                building.DisableWorkable();
            }
        }
        
        // 通过 UIManager 隐藏单位列表面板
        if (UIManager.Instance != null && UIManager.Instance.UnitListPanel != null)
        {
            if (UIManager.Instance.UnitListPanel.gameObject.activeInHierarchy)
            {
                UIManager.Instance.UnitListPanel.Hide();
                Debug.Log("[InputHandler] 点击空白处，已关闭单位列表面板");
            }
        }
    }

    // 框选处理
    private void HandleBoxSelection()
    {
        Rect selectionRect = GetSelectionRect(_dragStartPos, _dragCurrentPos);
        List<Unit> unitsInBox = GetUnitsInRect(selectionRect);
        
        if (unitsInBox.Count > 0)
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                foreach (var unit in unitsInBox)
                {
                    SelectionManager.Instance.AddToSelection(unit);
                }
            }
            else
            {
                SelectionManager.Instance.SelectUnits(unitsInBox);
            }
        }
    }

    // Raycast 检测单位
    private Unit RaycastUnit(Vector2 screenPos)
    {
        Ray ray = _mainCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            Unit unit = hit.collider.GetComponentInParent<Unit>();
            return unit;
        }

        return null;
    }

    // 获取选择矩形
    private Rect GetSelectionRect(Vector2 start, Vector2 end)
    {
        float minX = Mathf.Min(start.x, end.x);
        float minY = Mathf.Min(start.y, end.y);
        float width = Mathf.Abs(end.x - start.x);
        float height = Mathf.Abs(end.y - start.y);

        return new Rect(minX, minY, width, height);
    }

    // 筛选框内单位
    private List<Unit> GetUnitsInRect(Rect screenRect)
    {
        List<Unit> result = new List<Unit>();
        var allUnits = UnitManager.Instance?.AllUnits;

        if (allUnits == null) return result;

        foreach (var unit in allUnits)
        {
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(unit.Position);
            
            if (screenPos.z > 0 && screenRect.Contains(new Vector2(screenPos.x, screenPos.y)))
            {
                result.Add(unit);
            }
        }

        return result;
    }

    // 更新选择框显示
    private void UpdateSelectionBox()
    {
        if (_selectionBox == null) return;

        _selectionBox.gameObject.SetActive(true);
        
        Rect rect = GetSelectionRect(_dragStartPos, _dragCurrentPos);
        
        // 设置位置和大小
        _selectionBox.anchoredPosition = new Vector2(rect.xMin, rect.yMin);
        _selectionBox.sizeDelta = new Vector2(rect.width, rect.height);
    }

    // 隐藏选择框
    private void HideSelectionBox()
    {
        if (_selectionBox != null)
        {
            _selectionBox.gameObject.SetActive(false);
        }
    }
    
    // 显示移动目标指示器（可选功能）
    private void ShowMoveTargetIndicator(Vector3 position)
    {
        // 可以在这里创建一个简单的特效或标记
        // 例如：在地面创建一个临时的绿色圆点
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.position = position + Vector3.up * 0.1f;
        indicator.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
        
        Renderer renderer = indicator.GetComponent<Renderer>();
        renderer.material.color = new Color(0, 1, 0, 0.6f);
        
        // 2 秒后销毁指示器
        Destroy(indicator, 0.1f);
    }

    // 获取或创建 Canvas
    private void FindOrCreateCanvas()
    {
        // 查找现有 Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        if (canvases.Length > 0)
        {
            _canvas = canvases[0];
        }
        else
        {
            // 创建新 Canvas
            GameObject canvasObj = new GameObject("Canvas");
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
    }

    // 创建选择框 UI
    private void CreateSelectionBox()
    {
        if (_canvas == null)
        {
            FindOrCreateCanvas();
        }

        // 创建选择框 GameObject
        GameObject boxObj = new GameObject("SelectionBox");
        
        // 关键：设置父对象为 Canvas
        boxObj.transform.SetParent(_canvas.transform, false);
        
        // 添加 RectTransform
        _selectionBox = boxObj.AddComponent<RectTransform>();
        
        // 添加 Image 组件
        _selectionBoxImage = boxObj.AddComponent<Image>();
        
        // 设置绿色半透明颜色
        _selectionBoxImage.color = new Color(0, 1, 0, 0.3f);
        
        // 设置锚点为左下角（重要！）
        _selectionBox.anchorMin = Vector2.zero;
        _selectionBox.anchorMax = Vector2.zero;
        _selectionBox.pivot = Vector2.zero;
        
        // 初始隐藏
        _selectionBox.gameObject.SetActive(false);
    }
}