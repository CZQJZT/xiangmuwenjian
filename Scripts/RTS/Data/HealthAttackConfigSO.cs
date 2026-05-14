using System.Collections.Generic;
using UnityEngine;
using JunqiGame.Core;

namespace JunqiGame.RTS.Data
{
    /// <summary>
    /// 数据驱动：每个 Rank 对应的 Health 与 Attack
    /// </summary>
    [CreateAssetMenu(fileName = "HealthAttackConfig", menuName = "Junqi/RTS/Health Attack Config")]
    public class HealthAttackConfigSO : ScriptableObject
    {
        [System.Serializable]
        public class RankHealthEntry
        {
            public PieceRank Rank;
            public int Health;
            public int Attack;
        }

        public List<RankHealthEntry> Entries = new List<RankHealthEntry>();

        private Dictionary<PieceRank, (int health, int attack)> lookupCache = null;

        private void BuildCache()
        {
            if (lookupCache != null) return;
            lookupCache = new Dictionary<PieceRank, (int, int)>();
            foreach (var e in Entries)
            {
                lookupCache[e.Rank] = (e.Health, e.Attack);
            }
        }

        public (int health, int attack) GetForRank(PieceRank rank)
        {
            BuildCache();
            if (lookupCache.TryGetValue(rank, out var result))
                return result;
            Debug.LogWarning($"HealthAttackConfig: No entry for {rank}, using default (1, 0)");
            return (1, 0);
        }

        public void ValidateConfig()
        {
            var allRanks = System.Enum.GetValues(typeof(PieceRank));
            BuildCache();
            foreach (PieceRank rank in allRanks)
            {
                if (!lookupCache.ContainsKey(rank))
                {
                    Debug.LogWarning($"HealthAttackConfig: Missing entry for {rank}");
                }
            }
        }
    }
}