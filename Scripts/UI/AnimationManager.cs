using UnityEngine;
using JunqiGame.Core;

namespace JunqiGame.UI
{
    /// <summary>
    /// 动画管理�?
    /// 负责管理游戏中的各种动画效果
    /// </summary>
    public class AnimationManager : MonoBehaviour
    {
        [Header("动画设置")]
        [Tooltip("棋子移动动画时长")]
        public float moveAnimationDuration = 0.3f;
        
        [Tooltip("棋子出现动画时长")]
        public float appearAnimationDuration = 0.5f;
        
        [Tooltip("棋子消失动画时长")]
        public float disappearAnimationDuration = 0.3f;
        
        [Tooltip("战斗动画时长")]
        public float combatAnimationDuration = 0.6f;

        [Header("动画曲线")]
        [Tooltip("移动动画曲线")]
        public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Tooltip("出现动画曲线")]
        public AnimationCurve appearCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 2),
            new Keyframe(1, 1, 2, 0)
        );
        
        [Tooltip("消失动画曲线")]
        public AnimationCurve disappearCurve = new AnimationCurve(
            new Keyframe(0, 1, -2, 0),
            new Keyframe(1, 0, 0, -2)
        );

        public static AnimationManager Instance { get; private set; }

        [Header("Animation Settings")]
        public float moveDuration = 0.5f;
        
        [Header("Runtime State")]
        private Coroutine currentAnimationCoroutine;
        public bool IsAnimating => currentAnimationCoroutine != null;
        
        // 🔑 新增：暴露战斗触发状态供 UI 层查�?
        public bool IsCombatTriggered => isCombatTriggered;
        
        private bool isCombatTriggered = false;

        private void Awake()
        {
            // 🔑 修复：确保使用大�?I �?Instance
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 播放棋子移动动画
        /// </summary>
        public void PlayMoveAnimation(GameObject piece, Vector3 targetPosition, System.Action onComplete = null)
        {
            if (piece == null) return;
            
            currentAnimationCoroutine = StartCoroutine(MoveAnimationCoroutine(piece, targetPosition, moveAnimationDuration, moveCurve, onComplete));
        }

        /// <summary>
        /// 播放棋子出现动画
        /// </summary>
        public void PlayAppearAnimation(GameObject piece, System.Action onComplete = null)
        {
            if (piece == null) return;
            
            piece.transform.localScale = Vector3.zero;
            
            currentAnimationCoroutine = StartCoroutine(ScaleAnimationCoroutine(piece, Vector3.one, appearAnimationDuration, appearCurve, onComplete));
        }

        /// <summary>
        /// 播放棋子消失动画
        /// </summary>
        public void PlayDisappearAnimation(GameObject piece, System.Action onComplete = null)
        {
            if (piece == null) return;
            
            StartCoroutine(ScaleAnimationCoroutine(piece, Vector3.zero, disappearAnimationDuration, disappearCurve, () =>
            {
                GameObject.Destroy(piece);
                onComplete?.Invoke();
            }));
        }

        /// <summary>
        /// 播放战斗动画（震动效果）
        /// </summary>
        public void PlayCombatAnimation(GameObject piece, System.Action onComplete = null)
        {
            if (piece == null) return;
            
            StartCoroutine(CombatAnimationCoroutine(piece, combatAnimationDuration, onComplete));
        }

        /// <summary>
        /// 播放选中动画（脉冲效果）
        /// </summary>
        public void PlaySelectAnimation(GameObject piece, float duration = 1f)
        {
            if (piece == null) return;
            
            StartCoroutine(PulseAnimationCoroutine(piece, duration));
        }

        /// <summary>
        /// 停止选中的动�?
        /// </summary>
        public void StopSelectAnimation(GameObject piece)
        {
            StopAllCoroutines();
            currentAnimationCoroutine = null;
            if (piece != null) piece.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 停止当前正在播放的动�?
        /// </summary>
        public void StopCurrentAnimation()
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }
            currentAnimationCoroutine = null;
            isCombatTriggered = true;
            Debug.Log("🛑 [AnimationManager] Animation stopped");
        }

        /// <summary>
        /// 重置战斗触发标志（每 Tick 开始时调用�?
        /// </summary>
        public void ResetCombatTriggered()
        {
            isCombatTriggered = false;
        }

        // 协程：移动动�?
        private System.Collections.IEnumerator MoveAnimationCoroutine(
            GameObject obj, 
            Vector3 targetPosition, 
            float duration, 
            AnimationCurve curve,
            System.Action onComplete)
        {
            Vector3 startPosition = obj.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = curve.Evaluate(t);
                
                obj.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
                yield return null;
            }

            obj.transform.position = targetPosition;
            onComplete?.Invoke();
        }

        // 协程：缩放动�?
        private System.Collections.IEnumerator ScaleAnimationCoroutine(
            GameObject obj, 
            Vector3 targetScale, 
            float duration, 
            AnimationCurve curve,
            System.Action onComplete)
        {
            Vector3 startScale = obj.transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = curve.Evaluate(t);
                
                obj.transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
                yield return null;
            }

            obj.transform.localScale = targetScale;
            onComplete?.Invoke();
        }

        // 协程：战斗动画（震动�?
        private System.Collections.IEnumerator CombatAnimationCoroutine(
            GameObject obj, 
            float duration,
            System.Action onComplete)
        {
            Vector3 originalPosition = obj.transform.position;
            float elapsed = 0f;
            float shakeAmount = 10f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                
                // 震动效果
                float xOffset = Random.Range(-shakeAmount, shakeAmount) * (1 - elapsed / duration);
                float yOffset = Random.Range(-shakeAmount, shakeAmount) * (1 - elapsed / duration);
                
                obj.transform.position = originalPosition + new Vector3(xOffset, yOffset, 0);
                
                yield return null;
            }

            obj.transform.position = originalPosition;
            onComplete?.Invoke();
        }

        // 协程：脉冲动�?
        private System.Collections.IEnumerator PulseAnimationCoroutine(GameObject obj, float duration)
        {
            float elapsed = 0f;
            Vector3 originalScale = obj.transform.localScale;
            Vector3 targetScale = originalScale * 1.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 使用正弦波创建脉冲效�?
                float pulse = Mathf.Sin(t * Mathf.PI * 4) * 0.1f + 1f;
                obj.transform.localScale = originalScale * pulse;
                
                yield return null;
            }

            obj.transform.localScale = originalScale;
        }

        /// <summary>
        /// 播放沿路径逐格移动的动画
        /// onStepComplete 返回 false 可提前终止后续步骤（用于碰撞检测）
        /// </summary>
        public void PlayMoveAlongPath(
            GameObject piece,
            System.Collections.Generic.List<BoardPosition> path,
            GameObject[,] boardCells,
            float moveSpeed = 50f,
            System.Func<int, BoardPosition, bool> onStepComplete = null,
            System.Action onComplete = null)
        {
            if (piece == null || path == null || path.Count < 2 || boardCells == null)
            {
                Debug.LogWarning("⚠️ [AnimationManager] Invalid parameters");
                onComplete?.Invoke();
                return;
            }

            Vector3[] worldPositions = Board.PathToWorldPositions(path, boardCells);
            currentAnimationCoroutine = StartCoroutine(MoveAlongPathCoroutine(piece, worldPositions, path, boardCells, moveSpeed, onStepComplete, onComplete));
        }

        private System.Collections.IEnumerator MoveAlongPathCoroutine(
            GameObject piece,
            Vector3[] worldPositions,
            System.Collections.Generic.List<BoardPosition> boardPositions,
            GameObject[,] boardCells,
            float moveSpeed,
            System.Func<int, BoardPosition, bool> onStepComplete,
            System.Action onComplete)
        {
            if (piece == null)
            {
                currentAnimationCoroutine = null;
                onComplete?.Invoke();
                yield break;
            }

            for (int i = 0; i < worldPositions.Length - 1; i++)
            {
                if (piece == null)
                {
                    currentAnimationCoroutine = null;
                    onComplete?.Invoke();
                    yield break;
                }

                Vector3 startPos = piece.transform.position;
                Vector3 targetPos = worldPositions[i + 1];

                float distance = Vector3.Distance(startPos, targetPos);
                float moveTime = distance / moveSpeed;

                if (moveTime < 0.01f)
                {
                    if (piece != null)
                        piece.transform.position = targetPos;
                    bool shouldContinue = onStepComplete?.Invoke(i + 1, boardPositions[i + 1]) ?? true;
                    if (!shouldContinue)
                    {
                        currentAnimationCoroutine = null;
                        break;
                    }
                    yield return null;
                    continue;
                }

                float elapsed = 0f;
                while (elapsed < moveTime)
                {
                    if (piece == null)
                    {
                        onComplete?.Invoke();
                        yield break;
                    }
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / moveTime);
                    piece.transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }

                if (piece == null)
                {
                    onComplete?.Invoke();
                    yield break;
                }

                piece.transform.position = targetPos;
                bool shouldContinueAfter = onStepComplete?.Invoke(i + 1, boardPositions[i + 1]) ?? true;
                if (!shouldContinueAfter)
                {
                    currentAnimationCoroutine = null;
                    break;
                }
                yield return null;
            }

            onComplete?.Invoke();
        }
    }
}
