using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class RestNodeUI : CombatUIScreen
    {
        [SerializeField] private Image restBg;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private RectTransform heroListRoot;

        public override void BuildLayout() { }

        public void Show(IReadOnlyList<CombatantDefinition> heroes, Action<CombatantDefinition> onSelectHero)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("RestNodeUI prefab layout is incomplete. Repair the prefab instead of rebuilding it at runtime.", this);
                return;
            }

            titleText.text = "休息节点";
            subtitleText.text = "选择一名英雄恢复 20 生命";
            CombatUIScreenUtility.ClearChildren(heroListRoot);
            var heroList = heroes ?? Array.Empty<CombatantDefinition>();
            var hasUsableHero = false;
            for (var i = 0; i < heroList.Count && i < 4; i++)
            {
                var hero = heroList[i];
                if (hero == null) continue;
                var usable = !hero.IsDead && hero.CurrentHealth < hero.MaxHealthWithGrowth;
                hasUsableHero |= usable;
                var row = CombatUIScreenUtility.CreatePanel($"RestHero_{i}", heroListRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -20f - i * 74f), new Vector2(-40f, 54f), new Color(0.055f, 0.048f, 0.047f, 0.94f));
                row.offsetMin = new Vector2(20f, row.offsetMin.y);
                row.offsetMax = new Vector2(-20f, row.offsetMax.y);
                var status = hero.IsDead ? "已死亡" : $"{hero.CurrentHealth}/{hero.MaxHealthWithGrowth}";
                var text = CombatUIScreenUtility.CreateText("Hero", row, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 17, TextAnchor.MiddleLeft);
                text.rectTransform.offsetMin = new Vector2(18f, 0f);
                text.rectTransform.offsetMax = new Vector2(-210f, 0f);
                text.text = $"{hero.displayName}    生命 {status}";
                CombatUIScreenUtility.SetTextStyle(text, new Color(0.88f, 0.8f, 0.66f), false);
                var localHero = hero;
                var restButton = CombatUIScreenUtility.CreateButton(row, "休息", new Vector2(610f, -27f), null, usable);
                BindButton(restButton, usable ? () => onSelectHero?.Invoke(localHero) : null, "休息");
            }

            if (!hasUsableHero)
            {
                var continueButton = CombatUIScreenUtility.CreateButton(restBg.rectTransform, "继续", new Vector2(420f, -496f), null, true);
                BindButton(continueButton, () => onSelectHero?.Invoke(null), "继续");
            }
        }

        public void RebuildLayoutFromCode(Sprite panelSprite)
        {
            CombatUIScreenUtility.ClearChildren(transform);
            var root = PrepareRoot();
            restBg = CombatUIScreenUtility.CreatePanel("RestPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 560f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            restBg.sprite = panelSprite;
            restBg.type = Image.Type.Sliced;
            titleText = CombatUIScreenUtility.CreateText("Title", restBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            subtitleText = CombatUIScreenUtility.CreateText("Subtitle", restBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -98f), new Vector2(-110f, 36f), 18, TextAnchor.MiddleCenter);
            CombatUIScreenUtility.SetTextStyle(titleText, new Color(0.96f, 0.82f, 0.48f), true);
            CombatUIScreenUtility.SetTextStyle(subtitleText, new Color(0.82f, 0.72f, 0.54f), false);
            heroListRoot = NewRoot(restBg.rectTransform, "HeroListRoot", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -150f), new Vector2(-160f, 320f), new Vector2(0.5f, 1f));
        }

        private bool TryBindExistingLayout()
        {
            restBg = restBg != null ? restBg : transform.Find("RestPanel")?.GetComponent<Image>();
            titleText = titleText != null ? titleText : restBg?.transform.Find("Title")?.GetComponent<Text>();
            subtitleText = subtitleText != null ? subtitleText : restBg?.transform.Find("Subtitle")?.GetComponent<Text>();
            heroListRoot = heroListRoot != null ? heroListRoot : restBg?.transform.Find("HeroListRoot") as RectTransform;
            return restBg != null && titleText != null && subtitleText != null && heroListRoot != null;
        }

        private static RectTransform NewRoot(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot; rect.anchoredPosition = pos; rect.sizeDelta = size;
            return rect;
        }

        private static void BindButton(Button button, Action callback, string label)
        {
            if (button == null) return;
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null) text.text = label;
            button.onClick.RemoveAllListeners();
            button.interactable = callback != null;
            if (callback != null) button.onClick.AddListener(() => { CombatAudio.Instance.PlayUiClick(); callback.Invoke(); });
        }
    }
}
