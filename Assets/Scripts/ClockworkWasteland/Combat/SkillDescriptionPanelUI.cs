using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed class SkillDescriptionPanelUI : MonoBehaviour
    {
        public Image panelBg;
        public Text descriptionText;

        public RectTransform RectTransform => transform as RectTransform;

        public void Build(Sprite descriptionSprite)
        {
            if (TryBindExistingLayout())
            {
                ApplyDefaultSprite(descriptionSprite);
                DisableRaycasts();
                return;
            }

            var root = RectTransform;
            if (root == null)
            {
                return;
            }

            panelBg = CombatUIScreenUtility.CreatePanel("SkillDescriptionPanel", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.04f, 0.029f, 0.025f, 0.95f)).GetComponent<Image>();
            ApplyDefaultSprite(descriptionSprite);

            descriptionText = CombatUIScreenUtility.CreateText("SkillDescriptionText", panelBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 15, TextAnchor.UpperLeft);
            descriptionText.rectTransform.offsetMin = new Vector2(24f, 18f);
            descriptionText.rectTransform.offsetMax = new Vector2(-24f, -18f);
            descriptionText.text = "\u6280\u80fd\u8bf4\u660e";
            CombatUIScreenUtility.SetTextStyle(descriptionText, new Color(0.94f, 0.84f, 0.65f), false);
            DisableRaycasts();
        }

        public bool TryBindExistingLayout()
        {
            if (panelBg == null)
            {
                panelBg = GetComponent<Image>() ?? transform.Find("SkillDescriptionPanel")?.GetComponent<Image>();
            }

            if (descriptionText == null && panelBg != null)
            {
                descriptionText = panelBg.transform.Find("SkillDescriptionText")?.GetComponent<Text>()
                    ?? panelBg.GetComponentInChildren<Text>(true);
            }

            return panelBg != null && descriptionText != null;
        }

        private void ApplyDefaultSprite(Sprite descriptionSprite)
        {
            if (panelBg == null)
            {
                return;
            }

            panelBg.sprite = panelBg.sprite != null ? panelBg.sprite : descriptionSprite;
            panelBg.type = Image.Type.Sliced;
        }

        private void DisableRaycasts()
        {
            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }
    }
}
