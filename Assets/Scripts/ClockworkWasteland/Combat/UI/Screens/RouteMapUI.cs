using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed partial class RouteMapUI : CombatUIScreen
    {
        [SerializeField] private Image mapBg;
        [SerializeField] private Text titleText;
        [SerializeField] private Text routeText;
        [SerializeField] private RectTransform cardRoot;

        public override void BuildLayout() { }

        public void Show(int step, int totalSteps, IReadOnlyList<MapNodeOption> options, Action<MapNodeOption> onSelect)
        {
            PrepareRoot();
            if (!TryBindExistingLayout())
            {
                RebuildLayoutFromCode(null);
                if (!TryBindExistingLayout())
                {
                    Debug.LogError("RouteMapUI layout could not be created. Repair or create the prefab.", this);
                    return;
                }
            }

            titleText.text = "路线选择";
            routeText.text = $"起点  >  节点 {step}/{totalSteps}  >  最终 Boss";
            CombatUIScreenUtility.ClearChildren(cardRoot);

            var nodeOptions = options ?? Array.Empty<MapNodeOption>();
            for (var i = 0; i < nodeOptions.Count; i++)
            {
                var option = nodeOptions[i];
                var cardX = nodeOptions.Count == 2 ? 310f + i * 360f : 190f + i * 300f;
                var card = CombatUIScreenUtility.CreatePanel($"MapNode_{i}", cardRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cardX, -286f), new Vector2(260f, 240f), GetMapNodeColor(option.NodeType));
                var name = CombatUIScreenUtility.CreateText("Name", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -42f), new Vector2(-32f, 52f), 21, TextAnchor.MiddleCenter);
                name.text = option.DisplayName;
                CombatUIScreenUtility.SetTextStyle(name, new Color(1f, 0.84f, 0.44f), true);
                var description = CombatUIScreenUtility.CreateText("Description", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.UpperLeft);
                description.rectTransform.offsetMin = new Vector2(24f, 72f);
                description.rectTransform.offsetMax = new Vector2(-24f, -86f);
                description.text = option.Description;
                description.verticalOverflow = VerticalWrapMode.Overflow;
                CombatUIScreenUtility.SetTextStyle(description, new Color(0.88f, 0.8f, 0.66f), false);
                var localOption = option;
                var goButton = CombatUIScreenUtility.CreateButton(card, "前往", new Vector2(130f, -198f), null, true);
                BindButton(goButton, () => onSelect?.Invoke(localOption), "前往");
            }
        }

        public void RebuildLayoutFromCode(Sprite panelSprite)
        {
            CombatUIScreenUtility.ClearChildren(transform);
            var root = PrepareRoot();
            mapBg = CombatUIScreenUtility.CreatePanel("MapPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 560f), new Color(0.032f, 0.026f, 0.024f, 0.97f)).GetComponent<Image>();
            mapBg.sprite = panelSprite;
            mapBg.type = Image.Type.Sliced;
            titleText = CombatUIScreenUtility.CreateText("Title", mapBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-80f, 56f), 30, TextAnchor.MiddleCenter);
            routeText = CombatUIScreenUtility.CreateText("Route", mapBg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -106f), new Vector2(-120f, 40f), 18, TextAnchor.MiddleCenter);
            CombatUIScreenUtility.SetTextStyle(titleText, new Color(0.96f, 0.82f, 0.48f), true);
            CombatUIScreenUtility.SetTextStyle(routeText, new Color(0.82f, 0.72f, 0.54f), false);
            cardRoot = NewRoot(mapBg.rectTransform, "CardRoot", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, mapBg.rectTransform.sizeDelta, new Vector2(0f, 1f));
        }

        private bool TryBindExistingLayout()
        {
            mapBg = mapBg != null ? mapBg : transform.Find("MapPanel")?.GetComponent<Image>();
            titleText = titleText != null ? titleText : mapBg?.transform.Find("Title")?.GetComponent<Text>();
            routeText = routeText != null ? routeText : mapBg?.transform.Find("Route")?.GetComponent<Text>();
            cardRoot = cardRoot != null ? cardRoot : mapBg?.transform.Find("CardRoot") as RectTransform;
            return mapBg != null && titleText != null && routeText != null && cardRoot != null;
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

        private static Color GetMapNodeColor(MapNodeType nodeType)
        {
            switch (nodeType)
            {
                case MapNodeType.Rest: return new Color(0.045f, 0.075f, 0.06f, 0.96f);
                case MapNodeType.Chest: return new Color(0.105f, 0.072f, 0.034f, 0.96f);
                case MapNodeType.Battle:
                default: return new Color(0.075f, 0.044f, 0.04f, 0.96f);
            }
        }
    }
}
