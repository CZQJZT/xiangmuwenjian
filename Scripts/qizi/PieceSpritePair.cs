using System;
using System.Collections.Generic;
using UnityEngine;
using JunqiGame.Core;

namespace UnityJunqi.PieceDisplay
{
    [System.Serializable]
    public class PieceSpritePair
    {
        public PieceRank rank;
        public Sprite sprite;
    }

    /// <summary>
    /// 棋子精灵管理器，用于管理蓝方和红方棋子的精灵映射
    /// </summary>
    public class PieceSpriteManager : MonoBehaviour
    {
        [Header("蓝方棋子精灵")]
        public PieceSpritePair[] bluePieces;

        [Header("红方棋子精灵")]
        public PieceSpritePair[] redPieces;

        // 在 Start 或 Awake 中转换为字典
        private Dictionary<PieceRank, Sprite> bluePieceSprites;
        private Dictionary<PieceRank, Sprite> redPieceSprites;

        private void Awake()
        {
            // 转换数组为字典
            bluePieceSprites = new Dictionary<PieceRank, Sprite>();
            foreach (var pair in bluePieces)
            {
                if (pair.sprite != null)
                {
                    bluePieceSprites[pair.rank] = pair.sprite;
                }
            }
            
            redPieceSprites = new Dictionary<PieceRank, Sprite>();
            foreach (var pair in redPieces)
            {
                if (pair.sprite != null)
                {
                    redPieceSprites[pair.rank] = pair.sprite;
                }
            }
        }

        /// <summary>
        /// 获取蓝方指定等级的棋子精灵
        /// </summary>
        public Sprite GetBluePieceSprite(PieceRank rank)
        {
            if (bluePieceSprites != null && bluePieceSprites.ContainsKey(rank))
            {
                return bluePieceSprites[rank];
            }
            return null;
        }

        /// <summary>
        /// 获取红方指定等级的棋子精灵
        /// </summary>
        public Sprite GetRedPieceSprite(PieceRank rank)
        {
            if (redPieceSprites != null && redPieceSprites.ContainsKey(rank))
            {
                return redPieceSprites[rank];
            }
            return null;
        }
    }
}