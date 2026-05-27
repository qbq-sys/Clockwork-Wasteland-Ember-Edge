using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class InventoryUI : CombatUIScreen
    {
        [SerializeField] private Image inventoryBg;
        [SerializeField] private Text titleText;
        [SerializeField] private RectTransform itemGridRoot;
        [SerializeField] private Button backButton;

        public override void BuildLayout() { }

        public void Show(IReadOnlyList<InventoryItemStack> inventory, IReadOnlyList<CombatantDefinition> heroes, Action<InventoryItemData, CombatantDefinition> onUse, Action onBack)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                Debug.LogError("InventoryUI prefab layout is incomplete. Repair the prefab instead of rebuilding it at runtime.", this);
                return;
            }

            titleText.text = "背包";
            BindButton(backButton, onBack, "返回");
            CombatUIScreenUtility.ClearChildren(itemGridRoot);

            var stacks = inventory ?? Array.Empty<InventoryItemStack>();
            if (stacks.Count == 0)
            {
                var empty = CombatUIScreenUtility.CreateText("Empty", itemGridRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 20, TextAnchor.MiddleCenter);
                empty.text = "背包是空的。";
                CombatUIScreenUtility.SetTextStyle(empty, new Color(0.82f, 0.72f, 0.54f), false);
                return;
            }

            for (var i = 0; i < stacks.Count; i++)
            {
                var stack = stacks[i];
                var x = i % 2 == 0 ? 205f : 675f;
                var y = -24f - (i / 2) * 238f;
                var itemPanel = CombatUIScreenUtility.CreatePanel($"InventoryItem_{i}", itemGridRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(420f, 220f), new Color(0.055f, 0.048f, 0.047f, 0.94f));
                var itemText = CombatUIScreenUtility.CreateText("ItemText", itemPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -42f), new Vector2(-32f, 56f), 17, TextAnchor.MiddleCenter);
                itemText.text = $"{stack.Item.itemName} x{stack.Count}";
                CombatUIScreenUtility.SetTextStyle(itemText, new Color(0.96f, 0.82f, 0.48f), true);
                var heroList = heroes ?? Array.Empty<CombatantDefinition>();
                for (var h = 0; h < heroList.Count && h < 4; h++)
                {
                    var hero = heroList[h];
                    var usable = IsItemUsableOnHero(stack.Item, hero);
                    var label = hero != null ? $"{hero.displayName} {hero.CurrentHealth}/{hero.MaxHealthWithGrowth}" : "无效目标";
                    var useButton = CombatUIScreenUtility.CreateButton(itemPanel, label, new Vector2(210f, -86f - h * 34f), null, usable);
                    var localHero = hero; var localItem = stack.Item;
                    BindButton(useButton, usable ? () => onUse?.Invoke(localItem, localHero) : null, label);
                }
            }
        }

        public void RebuildLayoutFromCode(Sprite panelSprite)
        {
            CombatUIScreenUtility.ClearChildren(transform);
            var root = PrepareRoot();
            inventoryBg = CombatUIScreenUtility.CreatePanel("InventoryPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 690f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            inventoryBg.sprite = panelSprite;
            inventoryBg.type = Image.Type.Sliced;
            titleText = CombatUIScreenUtility.CreateText("Title", inventoryBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            CombatUIScreenUtility.SetTextStyle(titleText, new Color(0.96f, 0.82f, 0.48f), true);
            itemGridRoot = NewRoot(inventoryBg.rectTransform, "ItemGridRoot", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -136f), new Vector2(880f, 500f), new Vector2(0f, 1f));
            backButton = CombatUIScreenUtility.CreateButton(inventoryBg.rectTransform, "返回", new Vector2(490f, -632f), null, true);
        }

        private bool TryBindExistingLayout()
        {
            inventoryBg = inventoryBg != null ? inventoryBg : transform.Find("InventoryPanel")?.GetComponent<Image>();
            titleText = titleText != null ? titleText : inventoryBg?.transform.Find("Title")?.GetComponent<Text>();
            itemGridRoot = itemGridRoot != null ? itemGridRoot : inventoryBg?.transform.Find("ItemGridRoot") as RectTransform;
            backButton = backButton != null ? backButton : FindButton(inventoryBg != null ? inventoryBg.transform : transform, "返回");
            return inventoryBg != null && titleText != null && itemGridRoot != null && backButton != null;
        }

        private static RectTransform NewRoot(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot; rect.anchoredPosition = pos; rect.sizeDelta = size;
            return rect;
        }

        private static bool IsItemUsableOnHero(InventoryItemData item, CombatantDefinition hero)
        {
            if (item == null || hero == null || !hero.isHero) return false;
            switch (item.effectType)
            {
                case InventoryItemEffectType.Revive: return hero.IsDead;
                case InventoryItemEffectType.Heal:
                default: return !hero.IsDead && !hero.IsRecovering && hero.CurrentHealth < hero.MaxHealthWithGrowth;
            }
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
            if (text != null) text.text = label;
            button.onClick.RemoveAllListeners();
            button.interactable = callback != null;
            if (callback != null) button.onClick.AddListener(() => { CombatAudio.Instance.PlayUiClick(); callback.Invoke(); });
        }
    }
}
