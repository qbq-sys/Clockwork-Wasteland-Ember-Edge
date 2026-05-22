using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClockworkWasteland.Combat
{
    public sealed class BattleUnit
    {
        private readonly List<StatusInstance> statuses = new List<StatusInstance>();
        private readonly Dictionary<SkillData, int> skillCooldowns = new Dictionary<SkillData, int>();

        public BattleUnit(CombatantDefinition definition, int currentPosition)
        {
            Definition = definition;
            CurrentPosition = currentPosition;
            definition.EnsureRuntimeHealth();
            Health = definition.isHero ? definition.CurrentHealth : definition.MaxHealthWithGrowth;
            Resource = 0;
        }

        public CombatantDefinition Definition { get; }
        public int CurrentPosition { get; set; }
        public int OccupiedSlotCount => System.Math.Max(1, Definition.occupiedSlotCount);
        public int Health { get; private set; }
        public int Resource { get; private set; }
        public int ActionPoints { get; private set; } = 1;
        public bool IsCorpse { get; private set; }
        public bool IsHero => Definition.isHero;
        public bool IsAlive => Health > 0;
        public bool CanAct => IsAlive && !IsCorpse && !IsStunned;
        public IReadOnlyList<StatusInstance> Statuses => statuses;
        public CombatArchetype Archetype => Definition.archetype;
        public CombatRowPreference PreferredRow => Definition.preferredRow;
        public bool HasCooldowns => skillCooldowns.Count > 0;

        public string DisplayName => IsCorpse ? $"{Definition.displayName}\u7684\u5c38\u4f53" : Definition.displayName;
        public int Level => Definition.Level;
        public int MaxHealth => IsCorpse ? System.Math.Max(1, Definition.corpseHealth) : Definition.MaxHealthWithGrowth;
        public int Speed => IsCorpse ? 0 : Definition.speed;
        public int Attack => Definition.AttackWithGrowth;
        public int Defense => Definition.DefenseWithGrowth;
        public bool IsStunned => statuses.Any(status => status.Stun && status.TurnsRemaining > 0);
        public float HealthRatio => MaxHealth > 0 ? (float)Health / MaxHealth : 0f;

        public void TakeDamage(int amount)
        {
            Health = System.Math.Max(0, Health - amount);
            SyncHealthToDefinition();
        }

        public void Heal(int amount)
        {
            if (IsCorpse)
            {
                return;
            }

            Health = System.Math.Min(MaxHealth, Health + amount);
            SyncHealthToDefinition();
        }

        public void RestoreForMaxHealthGain(int amount)
        {
            if (!IsCorpse && amount > 0)
            {
                Heal(amount);
            }
        }

        public void ResetActionPoints()
        {
            ActionPoints = 1;
        }

        public bool HasResourcesFor(SkillData skill)
        {
            return ActionPoints >= 1 && Resource >= skill.manaCost;
        }

        public void SpendResourcesFor(SkillData skill)
        {
            ActionPoints = System.Math.Max(0, ActionPoints - 1);
            Resource = System.Math.Max(0, Resource - skill.manaCost);
            StartCooldown(skill);
        }

        public int GetCooldownRemaining(SkillData skill)
        {
            return skill != null && skillCooldowns.TryGetValue(skill, out var remaining) ? remaining : 0;
        }

        public void TickSkillCooldowns()
        {
            foreach (var skill in skillCooldowns.Keys.ToArray())
            {
                skillCooldowns[skill]--;
                if (skillCooldowns[skill] <= 0)
                {
                    skillCooldowns.Remove(skill);
                }
            }
        }

        public SkillData ReduceRandomCooldown(int amount)
        {
            if (amount <= 0 || skillCooldowns.Count == 0)
            {
                return null;
            }

            var skillsOnCooldown = skillCooldowns.Keys.ToArray();
            var selectedSkill = skillsOnCooldown[Random.Range(0, skillsOnCooldown.Length)];
            skillCooldowns[selectedSkill] = System.Math.Max(0, skillCooldowns[selectedSkill] - amount);
            if (skillCooldowns[selectedSkill] <= 0)
            {
                skillCooldowns.Remove(selectedSkill);
            }

            return selectedSkill;
        }

        private void StartCooldown(SkillData skill)
        {
            if (skill != null && skill.cooldown > 0)
            {
                skillCooldowns[skill] = skill.cooldown;
            }
        }

        public void ConvertToCorpse()
        {
            IsCorpse = true;
            statuses.Clear();
            Health = MaxHealth;
        }

        private void SyncHealthToDefinition()
        {
            if (IsHero && !IsCorpse)
            {
                Definition.SetRuntimeHealth(Health);
            }
        }

        public void AddOrRefreshStatus(string displayName, int duration, int tickDamage)
        {
            statuses.RemoveAll(status => status.DisplayName == displayName);
            statuses.Add(new StatusInstance(displayName, duration, tickDamage));
        }

        public void AddOrRefreshBuff(BuffData buff)
        {
            if (buff == null || buff.duration <= 0)
            {
                return;
            }

            statuses.RemoveAll(status => status.DisplayName == buff.buffName);
            statuses.Add(new StatusInstance(buff.buffName, buff.duration, buff.tickDamage, buff.stun));
        }

        public int TickStatuses()
        {
            var totalDamage = statuses.Where(status => status.TickDamage > 0).Sum(status => status.TickDamage);
            if (totalDamage > 0)
            {
                TakeDamage(totalDamage);
            }

            foreach (var status in statuses)
            {
                status.AdvanceTurn();
            }

            statuses.RemoveAll(status => status.TurnsRemaining <= 0);
            return totalDamage;
        }
    }
}
