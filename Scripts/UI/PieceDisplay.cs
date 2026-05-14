using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JunqiGame.Core;

namespace JunqiGame.UI
{
    /// <summary>
    /// 棋子显示组件
    /// 负责在UI中显示棋子的外观（支持TextMeshPro）
    /// </summary>
        public class PieceDisplay : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("棋子文本（TextMeshPro）")]
        public TextMeshProUGUI pieceText;
        
        [Tooltip("棋子背景图片")]
        public Image backgroundImage;
        
        [Tooltip("棋子边框图片")]
        public Image borderImage;

        [Header("颜色设置")]
        [Tooltip("蓝方棋子颜色")]
        public Color blueColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        
        [Tooltip("红方棋子颜色")]
        public Color redColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        
        [Tooltip("选中状态颜色")]
        public Color selectedColor = Color.yellow;

        [Header("字体设置")]
        [Tooltip("棋子文字大小")]
        public float fontSize = 16f;

        // 当前显示的棋子
        private Piece currentPiece;
        
        /// <summary>
        /// 获取当前显示的棋子（公开属性）
        /// </summary>
        public Piece CurrentPiece => currentPiece;
        
        // 是否被选中
        private bool isSelected = false;
        
        // 原始缩放值
        private Vector3 originalScale = Vector3.one;
        
        /// <summary>
        /// 初始化时记录原始缩放
        /// </summary>
        private void Awake()
        {
            originalScale = transform.localScale;
        }
        
        /// <summary>
        /// 设置要显示的棋子
        /// </summary>
         public void SetPiece(Piece piece)
{
    currentPiece = piece;
    
    // 更新原始缩放值（因为SetPiece可能在Awake之后调用）
    originalScale = transform.localScale;
    
    // 根据棋子尺寸自动调整字体大小
    if (pieceText != null)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 根据高度设置字体大小（高度20，字体设为16）
            float fontSize = Mathf.Min(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y) * 0.8f;
            pieceText.fontSize = fontSize;
        }
    }
    
    UpdateDisplay();
}
        /// <summary>
        /// 更新显示
        /// </summary>
                 /// <summary>
        /// 更新显示
        /// </summary>
        private void UpdateDisplay()
        {
            if (currentPiece == null)
            {
                Debug.LogWarning($"⚠️ Current piece is null on {gameObject.name}, deactivating");
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // 设置棋子名称
            if (pieceText != null)
            {
                // 确保文字在最上层
                pieceText.transform.SetAsLastSibling();
                
                // 使用可配置的字体大小
                pieceText.fontSize = fontSize;
                
                pieceText.text = GetPieceName(currentPiece.Rank);
                
                // 根据阵营设置颜色（强制重置，避免累积）
pieceText.color = Color.white; // 先重置
pieceText.color = currentPiece.Color == PlayerColor.Blue ? blueColor : redColor;
                
                Debug.Log($"✅ Text set: {pieceText.text}, Font Size: {pieceText.fontSize}, Color: {pieceText.color}");
            }
            else
            {
                Debug.LogWarning($"⚠️ pieceText is null on {gameObject.name}! Please assign a TextMeshProUGUI component.");
            }

            // 设置背景颜色（可选）
            if (backgroundImage != null)
            {
                backgroundImage.color = currentPiece.Color == PlayerColor.Blue 
                    ? new Color(blueColor.r, blueColor.g, blueColor.b, 0.3f)
                    : new Color(redColor.r, redColor.g, redColor.b, 0.3f);
                    
                // 背景应该在文字下面
                backgroundImage.transform.SetAsFirstSibling();
            }

            // 设置边框（可选）
            if (borderImage != null)
            {
                borderImage.color = currentPiece.Color == PlayerColor.Blue 
                    ? blueColor 
                    : redColor;
            }
        }
        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            // 选中时扩大0.1倍（在原始缩放基础上乘以1.1）
            if (isSelected)
            {
                transform.localScale = originalScale * 1.1f;
            }
            else
            {
                // 取消选中时恢复到原始缩放
                transform.localScale = originalScale;
            }
        }
       
        /// <summary>
        /// 高亮显示（用于可移动位置提示）
        /// </summary>
        public void Highlight(bool highlight)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlight ? 
                    new Color(1f, 1f, 0f, 0.5f) : 
                    new Color(1f, 1f, 1f, 0.3f);
            }
        }

        /// <summary>
        /// 获取棋子中文名称
        /// </summary>
        private string GetPieceName(PieceRank rank)
        {
            switch (rank)
            {
                case PieceRank.Mine:
                    return "地雷";
                case PieceRank.Marshal:
                    return "司令";
                case PieceRank.General:
                    return "军长";
                case PieceRank.MajorGeneral:
                    return "师长";
                case PieceRank.Brigadier:
                    return "旅长";
                case PieceRank.Colonel:
                    return "团长";
                case PieceRank.Major:
                    return "营长";
                case PieceRank.Captain:
                    return "连长";
                case PieceRank.Lieutenant:
                    return "排长";
                case PieceRank.Sapper:
                    return "工兵";
                case PieceRank.Bomb:
                    return "炸弹";
                case PieceRank.Flag:
                    return "军旗";
                default:
                    return "未知";
            }
        }

        private void OnMouseDown()
        {
            Debug.Log($"Clicked on piece: {currentPiece}");
        }
    }
}