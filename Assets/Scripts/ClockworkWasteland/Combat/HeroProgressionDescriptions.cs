using System.Collections.Generic;
using System.Text;

namespace ClockworkWasteland.Combat
{
    public static class HeroProgressionDescriptions
    {
        public static string GetPassiveDisplayName(HeroPassive passive)
        {
            switch (passive)
            {
                case HeroPassive.Berserker: return "狂战士";
                case HeroPassive.Executioner: return "处决者";
                case HeroPassive.ChainReaction: return "连锁反应";
                case HeroPassive.Backstab: return "背刺";
                case HeroPassive.GlassCannon: return "玻璃大炮";
                case HeroPassive.IronWill: return "铁意志";
                case HeroPassive.Regenerator: return "再生";
                case HeroPassive.ThornArmor: return "荆棘护甲";
                case HeroPassive.Bodyguard: return "保镖";
                case HeroPassive.Fortress: return "堡垒";
                case HeroPassive.Tactician: return "战术家";
                case HeroPassive.Scavenger: return "回收者";
                case HeroPassive.Vanguard: return "先锋";
                case HeroPassive.Reaper: return "收割者";
                case HeroPassive.Inspirer: return "鼓舞者";
                default: return "无";
            }
        }

        public static string GetPassiveDescription(HeroPassive passive)
        {
            switch (passive)
            {
                case HeroPassive.Berserker: return "生命低于 50% 时，攻击力提升 30%。残血时越战越勇。";
                case HeroPassive.Executioner: return "攻击生命低于 30% 的敌人时，伤害提升 50%。擅长收割残血目标。";
                case HeroPassive.ChainReaction: return "击杀敌人时，对随机另一名敌人造成目标最大生命值 25% 的溅射伤害。";
                case HeroPassive.Backstab: return "从后排攻击前排敌人时，伤害提升 25%。适合后排切入。";
                case HeroPassive.GlassCannon: return "攻击力提升 25%，但防御力降低 50%。高攻低防的极端输出者。";
                case HeroPassive.IronWill: return "每场战斗首次受到致命伤害时，保留 1 点生命值。绝境中屹立不倒。";
                case HeroPassive.Regenerator: return "回合开始时，恢复最大生命值的 5%。擅长持久作战。";
                case HeroPassive.ThornArmor: return "受到伤害时，反弹 20% 伤害给攻击者。";
                case HeroPassive.Bodyguard: return "相邻队友受到攻击时，承担 30% 伤害。守护身边队友。";
                case HeroPassive.Fortress: return "位于前排时，防御力 +4。前排最稳定的壁垒。";
                case HeroPassive.Tactician: return "回合开始时，随机减少 1 名队友技能冷却 2 回合。";
                case HeroPassive.Scavenger: return "击杀敌人时，恢复自身最大生命值的 20%。";
                case HeroPassive.Vanguard: return "位于前排时，全体队友攻击力 +2。";
                case HeroPassive.Reaper: return "场上每有 1 名敌人死亡，攻击力提升 10%（可叠加）。";
                case HeroPassive.Inspirer: return "回合开始时，恢复全体队友最大生命值的 10%。";
                default: return "该英雄当前没有固定被动。";
            }
        }

        public static string GetSpecializationLongDescription(CombatSpecialization specialization)
        {
            switch (specialization)
            {
                case CombatSpecialization.Bastion:
                    return "更偏纯防御与护卫。通过承伤、拦截和稳阵站位维持前排。";
                case CombatSpecialization.Sentinel:
                    return "更偏压制与破阵。通过撞击、压制和阵型干扰打乱敌方节奏。";
                case CombatSpecialization.Slayer:
                    return "更偏残血处决。围绕击杀窗口和爆发收尾展开。";
                case CombatSpecialization.Breaker:
                    return "更偏破后切入。主动打开缺口，威胁关键后排。";
                case CombatSpecialization.Bombardier:
                    return "更偏爆破与范围压制。用多目标火力和异常持续施压。";
                case CombatSpecialization.Controller:
                    return "更偏控场与干扰。通过控制、冷却干涉和节奏削弱压制敌方。";
                case CombatSpecialization.Surgeon:
                    return "更偏急救与单体保命。围绕抢救濒危单位和稳定血线展开。";
                case CombatSpecialization.Stimulator:
                    return "更偏净化与团队支援。围绕增益、资源和冷却恢复展开。";
                default:
                    return "尚未选择专精分支。";
            }
        }

        public static string GetSpecializationTrackLabel(CombatSpecialization specialization)
        {
            switch (specialization)
            {
                case CombatSpecialization.Bastion: return "稳阵 / 承伤 / 护卫";
                case CombatSpecialization.Sentinel: return "压制 / 破阵 / 反制";
                case CombatSpecialization.Slayer: return "收割 / 残血 / 连锁";
                case CombatSpecialization.Breaker: return "切入 / 破后 / 爆发";
                case CombatSpecialization.Bombardier: return "轰炸 / 扩散 / 异常";
                case CombatSpecialization.Controller: return "控场 / 干扰 / 稳定";
                case CombatSpecialization.Surgeon: return "急救 / 保命 / 治疗";
                case CombatSpecialization.Stimulator: return "净化 / 增益 / 支援";
                default: return "成长路线";
            }
        }

        public static IReadOnlyList<string> GetSpecializationTags(CombatSpecialization specialization)
        {
            switch (specialization)
            {
                case CombatSpecialization.Bastion:
                    return new[] { "前排", "护卫", "承伤" };
                case CombatSpecialization.Sentinel:
                    return new[] { "阵型", "压制", "反制" };
                case CombatSpecialization.Slayer:
                    return new[] { "收割", "残血", "追击" };
                case CombatSpecialization.Breaker:
                    return new[] { "破后", "爆发", "切入" };
                case CombatSpecialization.Bombardier:
                    return new[] { "AOE", "DOT", "异常" };
                case CombatSpecialization.Controller:
                    return new[] { "控场", "冷却", "干扰" };
                case CombatSpecialization.Surgeon:
                    return new[] { "急救", "治疗", "保命" };
                case CombatSpecialization.Stimulator:
                    return new[] { "净化", "增益", "支援" };
                default:
                    return new[] { "成长" };
            }
        }

        public static string GetPassiveDesignNote(HeroPassive passive)
        {
            switch (passive)
            {
                case HeroPassive.Executioner:
                    return "适合和 Slayer / Breaker 类专精配合，强化收尾节奏。";
                case HeroPassive.Backstab:
                    return "适合与后排点杀技能组合，形成高机动爆发。";
                case HeroPassive.Bodyguard:
                    return "更适合 Bulwark 的护卫路线，强化队友保护。";
                case HeroPassive.Tactician:
                    return "更适合 Stimulator 路线，围绕冷却和资源支援。";
                case HeroPassive.ChainReaction:
                    return "更适合 Bombardier 路线，强化 AOE 和击杀扩散。";
                default:
                    return "当前仍是 enum 规则；后续如需更深成长，可以升级成 PassiveData 资产。";
            }
        }

        public static string BuildGrowthOverview(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return "暂无成长信息。";
            }

            var builder = new StringBuilder();
            builder.AppendLine($"固定被动：{GetPassiveDisplayName(hero.passive)}");
            builder.AppendLine(GetPassiveDescription(hero.passive));
            builder.AppendLine();

            var specializations = GetAvailableSpecializations(hero.archetype);
            if (specializations.Length > 0)
            {
                builder.AppendLine("2级专精方向：");
                foreach (var specialization in specializations)
                {
                    builder.AppendLine($"- {CombatantDefinition.GetSpecializationDisplayName(specialization)}：{GetSpecializationLongDescription(specialization)}");
                }
            }
            else
            {
                builder.AppendLine("2级专精方向：当前职业尚未配置。");
            }

            builder.AppendLine();
            builder.AppendLine("后续成长：");
            builder.AppendLine("- 3级：被动选择");
            builder.AppendLine("- 5级：构筑成长");
            builder.AppendLine("- 7级：强化成长");
            builder.AppendLine("- 9级：终盘成长");

            if (hero.skills != null && hero.skills.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine("当前技能：");
                foreach (var skill in hero.skills)
                {
                    if (skill == null)
                    {
                        continue;
                    }

                    builder.AppendLine($"- {skill.skillName}：{skill.description}");
                }
            }

            return builder.ToString().TrimEnd();
        }

        private static CombatSpecialization[] GetAvailableSpecializations(CombatArchetype archetype)
        {
            switch (archetype)
            {
                case CombatArchetype.Bulwark:
                    return new[] { CombatSpecialization.Bastion, CombatSpecialization.Sentinel };
                case CombatArchetype.Executioner:
                    return new[] { CombatSpecialization.Slayer, CombatSpecialization.Breaker };
                case CombatArchetype.Artificer:
                    return new[] { CombatSpecialization.Bombardier, CombatSpecialization.Controller };
                case CombatArchetype.Physician:
                    return new[] { CombatSpecialization.Surgeon, CombatSpecialization.Stimulator };
                default:
                    return new CombatSpecialization[0];
            }
        }
    }
}
