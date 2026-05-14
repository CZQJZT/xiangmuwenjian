using System;
using UnityEngine;
using JunqiGame.RTS.Data;

namespace JunqiGame.Core
{
    /// <summary>
    /// 棋子数据类
    /// </summary>
    [Serializable]
    public class Piece
    {
        public PlayerColor Color;
        public PieceRank Rank;
        
        // 🔑 RTS 核心属性
        public int Health;
        public int Attack;

        public string Annotation;

        private static readonly (int health, int attack)[] rankStats = new (int, int)[12];

        static Piece()
        {
            var config = Resources.Load<HealthAttackConfigSO>("RTS/Data/HealthAttackConfig");
            if (config != null)
            {
                foreach (PieceRank rank in System.Enum.GetValues(typeof(PieceRank)))
                {
                    var s = config.GetForRank(rank);
                    rankStats[(int)rank] = (s.health, s.attack);
                }
            }
            else
            {
                foreach (PieceRank rank in System.Enum.GetValues(typeof(PieceRank)))
                    rankStats[(int)rank] = GetDefaultStats(rank);
                Debug.LogWarning("Piece: HealthAttackConfigSO not found, using defaults");
            }
        }

        public Piece(PlayerColor color, PieceRank rank)
        {
            Color = color;
            Rank = rank;
            var stats = rankStats[(int)rank];
            Health = stats.health;
            Attack = stats.attack;
        }

        private static (int health, int attack) GetDefaultStats(PieceRank rank)
        {
            switch (rank)
            {
                case PieceRank.Flag: return (1, 0);
                case PieceRank.Bomb: return (1, 9999);
                case PieceRank.Mine: return (9999, 9999);
                case PieceRank.Sapper: return (5, 1);
                case PieceRank.Lieutenant: return (10, 2);
                case PieceRank.Captain: return (20, 4);
                case PieceRank.Major: return (40, 8);
                case PieceRank.Colonel: return (80, 16);
                case PieceRank.Brigadier: return (160, 32);
                case PieceRank.MajorGeneral: return (320, 64);
                case PieceRank.General: return (640, 128);
                case PieceRank.Marshal: return (1280, 256);
                default: return (10, 1);
            }
        }

        public Piece Clone()
        {
            Piece p = new Piece(Color, Rank);
            // 🔑 克隆时同步战斗属性
            p.Health = this.Health;
            p.Attack = this.Attack;
            p.Annotation = this.Annotation;
            return p;
        }

        /// <summary>
        /// 棋子是否可以移动
        /// </summary>
        public bool CanMove()
        {
            return Rank != PieceRank.Mine && Rank != PieceRank.Flag;
        }

        /// <summary>
        /// 是否是炸弹
        /// </summary>
        public bool IsBomb()
        {
            return Rank == PieceRank.Bomb;
        }

        /// <summary>
        /// 是否是地雷
        /// </summary>
        public bool IsMine()
        {
            return Rank == PieceRank.Mine;
        }

        /// <summary>
        /// 是否是军旗
        /// </summary>
        public bool IsFlag()
        {
            return Rank == PieceRank.Flag;
        }

        /// <summary>
        /// 是否是工兵
        /// </summary>
        public bool IsSapper()
        {
            return Rank == PieceRank.Sapper;
        }

        /// <summary>
        /// 将等级枚举转换为字符串
        /// </summary>
        public string RankStr => Rank.ToString();

        public static string RankToString(PieceRank rank)
        {
            return ((int)rank).ToString();
        }

        /// <summary>
        /// 将字符串转换为等级枚举
        /// </summary>
        public static PieceRank StringToRank(string rankStr)
        {
            if (int.TryParse(rankStr, out int rankValue))
            {
                return (PieceRank)rankValue;
            }
            throw new ArgumentException($"Invalid rank string: {rankStr}");
        }

        public override string ToString()
        {
            return $"[{Color}] {Rank}";
        }
    }
}
