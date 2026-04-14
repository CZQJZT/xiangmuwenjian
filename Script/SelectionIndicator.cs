using UnityEngine;

public class SelectionIndicator : MonoBehaviour
{
    [Header("Indicator Settings")]
    public GameObject indicatorPrefab;
    public Vector3 indicatorOffset = new Vector3(0, 0.5f, 0);
    
    private GameObject _indicatorInstance;
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        
        if (indicatorPrefab == null)
        {
            CreateDefaultIndicator();
        }
        else
        {
            _indicatorInstance = Instantiate(indicatorPrefab, transform);
            _indicatorInstance.transform.localPosition = indicatorOffset;
        }
        
        // 确保初始状态隐藏
        Hide();
    }

    private void CreateDefaultIndicator()
    {
        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        circle.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);
        circle.transform.parent = transform;
        circle.transform.localPosition = indicatorOffset;
        
        Renderer renderer = circle.GetComponent<Renderer>();
        renderer.material.color = new Color(0, 1, 0, 0.5f);
        
        _indicatorInstance = circle;
    }

    public void Show(bool show)
    {
        if (_indicatorInstance != null)
        {
            _indicatorInstance.SetActive(show);
            Debug.Log($"SelectionIndicator: {(show ? "显示" : "隐藏")} - {gameObject.name}");
        }
    }

    public void Hide()
    {
        Show(false);
    }

    private void Update()
    {
        if (_indicatorInstance != null && _mainCamera != null)
        {
            _indicatorInstance.transform.LookAt(_mainCamera.transform);
        }
    }

    private void OnDestroy()
    {
        if (_indicatorInstance != null && indicatorPrefab == null)
        {
            Destroy(_indicatorInstance);
        }
    }
}