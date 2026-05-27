using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class RewardScreenUI : CombatUIScreen
    {
        [SerializeField] private Image rewardBg;
        [SerializeField] private Text titleText;
        [SerializeField] private Text rewardSummaryText;
        [SerializeField] private Text rewardsText;
        [SerializeField] private Button continueButton;

        public override void BuildLayout() { }

        public void Show(int goldGained, int totalGold, IReadOnlyList<BattleRewardResult> results, Action onContinue)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("RewardScreenUI prefab layout is incomplete. Repair the prefab instead of rebuilding it at runtime.", this);
                return;
            }

            if (titleText != null) titleText.text = "战斗奖励";
            if (rewardSummaryText != null) rewardSummaryText.text = $"获得金币：{goldGained}\n当前金币：{totalGold}";

            if (rewardsText != null)
            {
                var lines = (results ?? Array.Empty<BattleRewardResult>())
                    .Where(result => result.Hero != null)
                    .Select(result => result.LevelsGained > 0
                        ? $"{result.Hero.displayName} 获得 {result.ExperienceGained} 经验，提升 {result.LevelsGained} 级"
                        : $"{result.Hero.displayName} 获得 {result.ExperienceGained} 经验");
                rewardsText.text = string.Join("\n", lines);
                if (string.IsNullOrWhiteSpace(rewardsText.text)) rewardsText.text = "本次未获得角色经验。";
            }

            BindButton(continueButton, onContinue, "继续");
        }

        public void RebuildLayoutFromCode(Sprite panelSprite)
        {
            CombatUIScreenUtility.ClearChildren(transform);
            var root = PrepareRoot();
            rewardBg = CombatUIScreenUtility.CreatePanel("RewardPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(720f, 470f), new Color(0.035f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            rewardBg.sprite = panelSprite;
            rewardBg.type = Image.Type.Sliced;
            titleText = CombatUIScreenUtility.CreateText("Title", rewardBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -46f), new Vector2(-60f, 52f), 30, TextAnchor.MiddleCenter);
            rewardSummaryText = CombatUIScreenUtility.CreateText("RewardSummary", rewardBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -112f), new Vector2(-80f, 64f), 20, TextAnchor.MiddleCenter);
            rewardsText = CombatUIScreenUtility.CreateText("Rewards", rewardBg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, 18, TextAnchor.UpperLeft);
            rewardsText.rectTransform.offsetMin = new Vector2(52f, 96f);
            rewardsText.rectTransform.offsetMax = new Vector2(-52f, -188f);
            rewardsText.verticalOverflow = VerticalWrapMode.Overflow;
            CombatUIScreenUtility.SetTextStyle(titleText, new Color(0.96f, 0.82f, 0.48f), true);
            CombatUIScreenUtility.SetTextStyle(rewardSummaryText, new Color(1f, 0.78f, 0.34f), true);
            CombatUIScreenUtility.SetTextStyle(rewardsText, new Color(0.88f, 0.8f, 0.66f), false);
            continueButton = CombatUIScreenUtility.CreateButton(rewardBg.rectTransform, "继续", new Vector2(360f, -414f), null, true);
        }

        private bool TryBindExistingLayout()
        {
            rewardBg = rewardBg != null ? rewardBg : transform.Find("RewardPanel")?.GetComponent<Image>();
            titleText = titleText != null ? titleText : rewardBg?.transform.Find("Title")?.GetComponent<Text>();
            rewardSummaryText = rewardSummaryText != null ? rewardSummaryText : rewardBg?.transform.Find("RewardSummary")?.GetComponent<Text>();
            rewardsText = rewardsText != null ? rewardsText : rewardBg?.transform.Find("Rewards")?.GetComponent<Text>();
            continueButton = continueButton != null ? continueButton : FindButton(rewardBg != null ? rewardBg.transform : transform, "继续");
            return rewardBg != null && titleText != null && rewardSummaryText != null && rewardsText != null && continueButton != null;
        }

        private static Button FindButton(Transform root, string label)
        {
            if (root == null) return null;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var text = button.GetComponentInChildren<Text>(true);
                if (text != null && string.Equals(text.text, label, StringComparison.Ordinal)) return button;
            }
            return null;
        }

        private static void BindButton(Button button, Action callback, string label)
        {
            if (button == null) return;
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null && !string.IsNullOrWhiteSpace(label)) text.text = label;
            button.onClick.RemoveAllListeners();
            button.interactable = callback != null;
            if (callback != null) button.onClick.AddListener(() => { CombatAudio.Instance.PlayUiClick(); callback.Invoke(); });
        }
    }
}
