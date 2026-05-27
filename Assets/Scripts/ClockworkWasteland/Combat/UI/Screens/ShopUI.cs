using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class ShopUI : CombatUIScreen
    {
        [SerializeField] private Image shopBg;
        [SerializeField] private Text titleText;
        [SerializeField] private Text goldText;
        [SerializeField] private RectTransform itemListRoot;
        [SerializeField] private Button backButton;

        public override void BuildLayout() { }

        public void Show(IReadOnlyList<InventoryItemData> shopItems, int currentGold, IReadOnlyList<InventoryItemStack> inventory, Action<InventoryItemData> onBuy, Action onBack)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                RebuildLayoutFromCode(null);
                if (!TryBindExistingLayout())
                {
                    Debug.LogError("ShopUI layout could not be created. Repair or create the prefab.", this);
                    return;
                }
            }

            titleText.text = "商店";
            goldText.text = $"当前金币：{currentGold}";
            BindButton(backButton, onBack, "返回");
            CombatUIScreenUtility.ClearChildren(itemListRoot);

            var items = shopItems ?? Array.Empty<InventoryItemData>();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;
                var row = CombatUIScreenUtility.CreatePanel($"ShopItem_{i}", itemListRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -16f - i * 108f), new Vector2(-20f, 92f), new Color(0.055f, 0.048f, 0.047f, 0.94f));
                row.offsetMin = new Vector2(10f, row.offsetMin.y);
                row.offsetMax = new Vector2(-10f, row.offsetMax.y);
                var rowText = CombatUIScreenUtility.CreateText("ItemText", row, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.UpperLeft);
                rowText.rectTransform.offsetMin = new Vector2(18f, 8f);
                rowText.rectTransform.offsetMax = new Vector2(-190f, -10f);
                var count = inventory != null ? inventory.FirstOrDefault(stack => stack.Item == item).Count : 0;
                rowText.text = $"{item.itemName}  {item.price}金币  已有 {count}\n{item.description}";
                rowText.verticalOverflow = VerticalWrapMode.Overflow;
                CombatUIScreenUtility.SetTextStyle(rowText, new Color(0.88f, 0.8f, 0.66f), false);
                var buyButton = CombatUIScreenUtility.CreateButton(row, "购买", new Vector2(650f, -46f), null, currentGold >= item.price);
                var localItem = item;
                BindButton(buyButton, currentGold >= item.price ? () => onBuy?.Invoke(localItem) : null, "购买");
            }
        }

        public void RebuildLayoutFromCode(Sprite panelSprite)
        {
            CombatUIScreenUtility.ClearChildren(transform);
            var root = PrepareRoot();
            shopBg = CombatUIScreenUtility.CreatePanel("ShopPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 620f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            shopBg.sprite = panelSprite;
            shopBg.type = Image.Type.Sliced;
            titleText = CombatUIScreenUtility.CreateText("Title", shopBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            goldText = CombatUIScreenUtility.CreateText("Gold", shopBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -96f), new Vector2(-110f, 34f), 18, TextAnchor.MiddleCenter);
            CombatUIScreenUtility.SetTextStyle(titleText, new Color(0.96f, 0.82f, 0.48f), true);
            CombatUIScreenUtility.SetTextStyle(goldText, new Color(1f, 0.78f, 0.34f), true);
            itemListRoot = NewRoot(shopBg.rectTransform, "ItemListRoot", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -148f), new Vector2(-120f, 400f), new Vector2(0.5f, 1f));
            backButton = CombatUIScreenUtility.CreateButton(shopBg.rectTransform, "返回", new Vector2(430f, -562f), null, true);
        }

        private bool TryBindExistingLayout()
        {
            shopBg = shopBg != null ? shopBg : transform.Find("ShopPanel")?.GetComponent<Image>();
            titleText = titleText != null ? titleText : shopBg?.transform.Find("Title")?.GetComponent<Text>();
            goldText = goldText != null ? goldText : shopBg?.transform.Find("Gold")?.GetComponent<Text>();
            itemListRoot = itemListRoot != null ? itemListRoot : shopBg?.transform.Find("ItemListRoot") as RectTransform;
            backButton = backButton != null ? backButton : FindButton(shopBg != null ? shopBg.transform : transform, "返回");
            return shopBg != null && titleText != null && goldText != null && itemListRoot != null && backButton != null;
        }

        private static RectTransform NewRoot(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot; rect.anchoredPosition = pos; rect.sizeDelta = size;
            return rect;
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

