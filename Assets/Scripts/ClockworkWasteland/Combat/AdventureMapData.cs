using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClockworkWasteland.Combat
{
    public enum AdventureMapUnlockType
    {
        Default,
        ClearPrerequisiteMap
    }

    [System.Serializable]
    public sealed class AdventureEnemyPlacement
    {
        public CombatantDefinition combatant;
        [Range(1, 4)] public int slot = 1;
        [Min(1)] public int level = 1;

        public AdventureEnemyPlacement Clone()
        {
            return new AdventureEnemyPlacement
            {
                combatant = combatant,
                slot = Mathf.Clamp(slot, 1, 4),
                level = Mathf.Max(1, level)
            };
        }
    }

    [System.Serializable]
    public sealed class AdventureBattleConfig
    {
        public string battleId = "battle_01";
        public string displayName = "战斗 1";
        [TextArea(2, 4)] public string description = "\u6218\u6597\u63cf\u8ff0\u9884\u7559\u3002";
        public Sprite backgroundOverride;
        public Vector2 backgroundOffset;
        public float backgroundScale = 1f;
        public bool isBossBattle;
        [Min(0)] public int goldRewardMin = 50;
        [Min(0)] public int goldRewardMax = 150;
        [Min(0)] public int experienceRewardMin = 10;
        [Min(0)] public int experienceRewardMax = 20;
        [TextArea(1, 3)] public string rewardDescription = "\u6807\u51c6\u6218\u6597\u5956\u52b1\u3002";
        public List<AdventureEnemyPlacement> enemies = new List<AdventureEnemyPlacement>();

        public IReadOnlyList<AdventureEnemyPlacement> GetOrderedPlacements()
        {
            return enemies
                .Where(placement => placement != null && placement.combatant != null)
                .OrderBy(placement => Mathf.Clamp(placement.slot, 1, 4))
                .ToArray();
        }

        public void Normalize()
        {
            battleId = string.IsNullOrWhiteSpace(battleId) ? "battle_01" : battleId.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? battleId : displayName.Trim();
            backgroundScale = Mathf.Max(0.1f, backgroundScale);
            goldRewardMin = Mathf.Max(0, goldRewardMin);
            goldRewardMax = Mathf.Max(goldRewardMin, goldRewardMax);
            experienceRewardMin = Mathf.Max(0, experienceRewardMin);
            experienceRewardMax = Mathf.Max(experienceRewardMin, experienceRewardMax);
            if (enemies == null)
            {
                enemies = new List<AdventureEnemyPlacement>();
                return;
            }

            foreach (var placement in enemies.Where(placement => placement != null))
            {
                placement.slot = Mathf.Clamp(placement.slot, 1, 4);
                placement.level = Mathf.Max(1, placement.level);
            }
        }
    }

    [CreateAssetMenu(menuName = "Clockwork Wasteland/Adventure/Map Data")]
    public sealed class AdventureMapData : ScriptableObject
    {
        public string mapId = "map_01";
        public string displayName = "地图";
        [TextArea(2, 4)] public string description = "\u5730\u56fe\u63cf\u8ff0\u9884\u7559\u3002";
        public Sprite backgroundSprite;
        public Vector2 backgroundOffset;
        public float backgroundScale = 1f;
        public AdventureMapUnlockType unlockType = AdventureMapUnlockType.Default;
        public AdventureMapData prerequisiteMap;
        [TextArea(1, 3)] public string unlockDescription = "默认解锁";
        public List<AdventureBattleConfig> battles = new List<AdventureBattleConfig>();

        public int BattleCount => battles != null ? battles.Count(config => config != null) : 0;

        public bool IsUnlocked(ISet<string> clearedMapIds)
        {
            switch (unlockType)
            {
                case AdventureMapUnlockType.ClearPrerequisiteMap:
                    var prerequisiteId = prerequisiteMap != null ? prerequisiteMap.mapId : string.Empty;
                    return !string.IsNullOrWhiteSpace(prerequisiteId)
                        && clearedMapIds != null
                        && clearedMapIds.Contains(prerequisiteId);
                case AdventureMapUnlockType.Default:
                default:
                    return true;
            }
        }

        public string GetUnlockSummary()
        {
            switch (unlockType)
            {
                case AdventureMapUnlockType.ClearPrerequisiteMap:
                    if (!string.IsNullOrWhiteSpace(unlockDescription))
                    {
                        return unlockDescription.Trim();
                    }

                    return prerequisiteMap != null
                        ? $"\u901a\u5173\u524d\u7f6e\u5730\u56fe\uff1a{prerequisiteMap.displayName}"
                        : "\u9700\u8981\u524d\u7f6e\u5730\u56fe";
                case AdventureMapUnlockType.Default:
                default:
                    return string.IsNullOrWhiteSpace(unlockDescription) ? "默认解锁" : unlockDescription.Trim();
            }
        }

        public IReadOnlyList<AdventureBattleConfig> GetBattles()
        {
            return battles == null
                ? System.Array.Empty<AdventureBattleConfig>()
                : battles.Where(config => config != null).ToArray();
        }

        private void OnValidate()
        {
            mapId = string.IsNullOrWhiteSpace(mapId) ? "map_01" : mapId.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? mapId : displayName.Trim();
            backgroundScale = Mathf.Max(0.1f, backgroundScale);
            if (battles == null)
            {
                battles = new List<AdventureBattleConfig>();
                return;
            }

            foreach (var battle in battles.Where(config => config != null))
            {
                battle.Normalize();
            }
        }
    }

    [CreateAssetMenu(menuName = "Clockwork Wasteland/Adventure/Map Catalog")]
    public sealed class AdventureMapCatalog : ScriptableObject
    {
        public List<AdventureMapData> maps = new List<AdventureMapData>();

        public IReadOnlyList<AdventureMapData> GetOrderedMaps()
        {
            return maps == null
                ? System.Array.Empty<AdventureMapData>()
                : maps.Where(map => map != null).ToArray();
        }
    }

    [CreateAssetMenu(menuName = "Clockwork Wasteland/Adventure/Enemy Formation Preset")]
    public sealed class EnemyFormationPresetData : ScriptableObject
    {
        public string presetId = "preset_01";
        public string displayName = "阵容预设";
        public List<AdventureEnemyPlacement> enemies = new List<AdventureEnemyPlacement>();

        public void CopyFrom(IEnumerable<AdventureEnemyPlacement> source)
        {
            enemies = source == null
                ? new List<AdventureEnemyPlacement>()
                : source
                    .Where(placement => placement != null)
                    .Select(placement => placement.Clone())
                    .ToList();
        }
    }
}
