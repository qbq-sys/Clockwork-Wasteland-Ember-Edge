using System;
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
                case HeroPassive.Berserker:
                    return "生命低于 50% 时，攻击力提升 30%。适合残局爆发和强行换血。";
                case HeroPassive.Executioner:
                    return "攻击生命低于 30% 的敌人时，伤害提升 50%。适合终结残血目标。";
                case HeroPassive.ChainReaction:
                    return "击杀敌人后，对另一名随机敌人造成额外溅射伤害。适合多目标压制和滚雪球。";
                case HeroPassive.Backstab:
                    return "从后排攻击前排敌人时，伤害提升 25%。适合切入和越位打击。";
                case HeroPassive.GlassCannon:
                    return "攻击力提升 25%，但防御力降低 50%。高风险高收益。";
                case HeroPassive.IronWill:
                    return "每场战斗首次受到致命伤害时，保留 1 点生命。用于提供一次保命容错。";
                case HeroPassive.Regenerator:
                    return "回合开始时，恢复最大生命值的 5%。适合持续作战和慢速恢复。";
                case HeroPassive.ThornArmor:
                    return "受到伤害时，反弹部分伤害给攻击者。适合前排承伤角色。";
                case HeroPassive.Bodyguard:
                    return "相邻队友受到攻击时，承担部分伤害。强化护卫和队伍保护。";
                case HeroPassive.Fortress:
                    return "位于前排时，防御力提升。适合纯前排稳阵角色。";
                case HeroPassive.Tactician:
                    return "回合开始时，减少队友技能冷却。强化技能循环和节奏支持。";
                case HeroPassive.Scavenger:
                    return "击杀敌人时，恢复自身生命。强化连续作战能力。";
                case HeroPassive.Vanguard:
                    return "位于前排时，全队获得攻击增益。适合前线指挥型角色。";
                case HeroPassive.Reaper:
                    return "每有一个敌人死亡，自身攻击持续提高。适合收割滚雪球。";
                case HeroPassive.Inspirer:
                    return "回合开始时，为全队提供生命恢复。强化团队续航。";
                default:
                    return "当前没有被动效果。";
            }
        }

        public static IReadOnlyList<string> GetPassiveTags(HeroPassive passive)
        {
            switch (passive)
            {
                case HeroPassive.Berserker: return new[] { "残血", "爆发", "换血" };
                case HeroPassive.Executioner: return new[] { "处决", "单体", "收尾" };
                case HeroPassive.ChainReaction: return new[] { "击杀", "溅射", "扩散" };
                case HeroPassive.Backstab: return new[] { "切入", "后排", "爆发" };
                case HeroPassive.GlassCannon: return new[] { "高攻", "脆皮", "风险" };
                case HeroPassive.IronWill: return new[] { "保命", "容错", "前排" };
                case HeroPassive.Regenerator: return new[] { "续航", "恢复", "持久" };
                case HeroPassive.ThornArmor: return new[] { "反伤", "承伤", "牵制" };
                case HeroPassive.Bodyguard: return new[] { "护卫", "相邻", "承伤" };
                case HeroPassive.Fortress: return new[] { "前排", "防御", "稳阵" };
                case HeroPassive.Tactician: return new[] { "冷却", "支援", "节奏" };
                case HeroPassive.Scavenger: return new[] { "击杀", "回血", "续航" };
                case HeroPassive.Vanguard: return new[] { "前排", "光环", "团队" };
                case HeroPassive.Reaper: return new[] { "击杀", "叠层", "收割" };
                case HeroPassive.Inspirer: return new[] { "群体", "恢复", "支援" };
                default: return Array.Empty<string>();
            }
        }

        public static string GetPassiveDesignNote(HeroPassive passive)
        {
            switch (passive)
            {
                case HeroPassive.Executioner:
                    return "适合处决者路线，强化残血终结能力。";
                case HeroPassive.Backstab:
                    return "适合高机动切入角色，强调站位和目标选择。";
                case HeroPassive.Bodyguard:
                    return "适合守卫者的护卫路线，强化保护队友。";
                case HeroPassive.Fortress:
                    return "适合壁垒路线，强化前排纯防御。";
                case HeroPassive.Tactician:
                    return "适合控场师和激励师路线，强化团队技能循环。";
                case HeroPassive.ChainReaction:
                    return "适合爆破和多目标压制路线。";
                default:
                    return "当前仍然是第一版规则，后续如果被动系统继续扩展，可再升级为独立资产。";
            }
        }

        public static string GetSpecializationLongDescription(CombatSpecialization specialization)
        {
            switch (specialization)
            {
                case CombatSpecialization.Bastion:
                    return "偏纯防御与护卫。通过承伤、拦截和稳阵维持前排。";
                case CombatSpecialization.Sentinel:
                    return "偏压制与破阵。通过撞击、压制和阵型干扰打乱敌方节奏。";
                case CombatSpecialization.Slayer:
                    return "偏残局处决。围绕残血目标、击杀窗口和爆发收尾展开。";
                case CombatSpecialization.Breaker:
                    return "偏切入破后。主动制造缺口，威胁关键后排。";
                case CombatSpecialization.Bombardier:
                    return "偏爆破与范围压制。依靠多目标火力和异常状态扩大战果。";
                case CombatSpecialization.Controller:
                    return "偏控场与干扰。依靠控制、冷却干涉和节奏压制削弱敌方出手。";
                case CombatSpecialization.Surgeon:
                    return "偏急救与单体保命。围绕濒危救援和血线稳定展开。";
                case CombatSpecialization.Stimulator:
                    return "偏净化与团队支援。围绕增益、资源和冷却恢复展开。";
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
                case CombatSpecialization.Bastion: return new[] { "前排", "护卫", "承伤" };
                case CombatSpecialization.Sentinel: return new[] { "阵型", "压制", "反制" };
                case CombatSpecialization.Slayer: return new[] { "收割", "残血", "追击" };
                case CombatSpecialization.Breaker: return new[] { "破后", "爆发", "切入" };
                case CombatSpecialization.Bombardier: return new[] { "AOE", "DOT", "异常" };
                case CombatSpecialization.Controller: return new[] { "控场", "冷却", "干扰" };
                case CombatSpecialization.Surgeon: return new[] { "急救", "治疗", "保命" };
                case CombatSpecialization.Stimulator: return new[] { "净化", "增益", "支援" };
                default: return new[] { "成长" };
            }
        }

        public static CombatSpecialization[] GetAvailableSpecializations(CombatArchetype archetype)
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
                    return Array.Empty<CombatSpecialization>();
            }
        }

        public static HeroPassive[] GetLevelThreePassiveChoices(CombatantDefinition hero)
        {
            if (hero == null)
            {
                return Array.Empty<HeroPassive>();
            }

            switch (hero.specialization)
            {
                case CombatSpecialization.Bastion:
                    return new[] { HeroPassive.Fortress, HeroPassive.Bodyguard };
                case CombatSpecialization.Sentinel:
                    return new[] { HeroPassive.Bodyguard, HeroPassive.Vanguard };
                case CombatSpecialization.Slayer:
                    return new[] { HeroPassive.Executioner, HeroPassive.Reaper };
                case CombatSpecialization.Breaker:
                    return new[] { HeroPassive.Backstab, HeroPassive.Berserker };
                case CombatSpecialization.Bombardier:
                    return new[] { HeroPassive.ChainReaction, HeroPassive.GlassCannon };
                case CombatSpecialization.Controller:
                    return new[] { HeroPassive.Tactician, HeroPassive.GlassCannon };
                case CombatSpecialization.Surgeon:
                    return new[] { HeroPassive.Regenerator, HeroPassive.Inspirer };
                case CombatSpecialization.Stimulator:
                    return new[] { HeroPassive.Tactician, HeroPassive.Inspirer };
                default:
                    switch (hero.archetype)
                    {
                        case CombatArchetype.Bulwark:
                            return new[] { HeroPassive.Fortress, HeroPassive.Bodyguard };
                        case CombatArchetype.Executioner:
                            return new[] { HeroPassive.Executioner, HeroPassive.Backstab };
                        case CombatArchetype.Artificer:
                            return new[] { HeroPassive.ChainReaction, HeroPassive.Tactician };
                        case CombatArchetype.Physician:
                            return new[] { HeroPassive.Regenerator, HeroPassive.Inspirer };
                        default:
                            return Array.Empty<HeroPassive>();
                    }
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

            if (hero.growthPassive != HeroPassive.None)
            {
                builder.AppendLine();
                builder.AppendLine($"成长被动：{GetPassiveDisplayName(hero.growthPassive)}");
                builder.AppendLine(GetPassiveDescription(hero.growthPassive));
            }

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
    }
}
