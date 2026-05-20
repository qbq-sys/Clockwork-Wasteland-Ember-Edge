using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

namespace ClockworkWasteland.Combat
{
    public sealed class CombatantView : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer overlayRenderer;
        private Text nameLabel;
        private Text positionLabel;
        private RectTransform healthFill;
        private BattleUnit unit;
        private Sprite[] idleFrames;
        private float animationTimer;
        private int animationFrame;
        private Action<BattleUnit> clicked;

        public BattleUnit Unit => unit;

        public void Initialize(BattleUnit battleUnit, Sprite fallbackSprite, float scaleMultiplier, Action<BattleUnit> onClicked)
        {
            unit = battleUnit;
            clicked = onClicked;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            idleFrames = battleUnit.Definition.idleAnimationFrames;
            spriteRenderer.sprite = GetInitialSprite(battleUnit, fallbackSprite);
            spriteRenderer.color = battleUnit.Definition.tint;
            spriteRenderer.sortingOrder = 2;

            var overlayObject = new GameObject("ActionOverlay");
            overlayObject.transform.SetParent(transform, false);
            overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
            overlayRenderer.sortingOrder = 80;
            overlayRenderer.enabled = false;

            var scale = Mathf.Max(0.1f, battleUnit.Definition.visualScale * scaleMultiplier);
            transform.localScale = new Vector3(scale, scale, 1f);
            spriteRenderer.flipX = battleUnit.IsHero;

            CreateNameplate();
            CreatePositionBadge();
            CreateClickCollider();
            Refresh();
        }

        public void Refresh()
        {
            if (unit == null)
            {
                return;
            }

            var healthPercent = unit.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)unit.Health / unit.MaxHealth);
            healthFill.localScale = new Vector3(healthPercent, 1f, 1f);
            spriteRenderer.color = unit.IsCorpse ? new Color(0.32f, 0.3f, 0.28f, 0.78f) : unit.Definition.tint;
            nameLabel.text = $"{unit.DisplayName}\n{unit.Health}/{unit.MaxHealth}";
            if (positionLabel != null)
            {
                positionLabel.text = unit.CurrentPosition.ToString();
            }

            gameObject.SetActive(unit.IsAlive);
        }

        private void Update()
        {
            if (idleFrames == null || idleFrames.Length == 0 || spriteRenderer == null || unit == null || !unit.IsAlive)
            {
                return;
            }

            animationTimer += Time.deltaTime;
            if (animationTimer < 1f / 12f)
            {
                return;
            }

            animationTimer = 0f;
            animationFrame = (animationFrame + 1) % idleFrames.Length;
            spriteRenderer.sprite = idleFrames[animationFrame];
        }

        public void SetHighlighted(bool highlighted)
        {
            if (spriteRenderer == null || unit == null || !unit.IsAlive)
            {
                return;
            }

            spriteRenderer.color = highlighted ? Color.white : unit.IsCorpse ? new Color(0.32f, 0.3f, 0.28f, 0.78f) : unit.Definition.tint;
        }

        public void AlignFeetTo(float worldY)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            var offset = worldY - spriteRenderer.bounds.min.y;
            transform.position += new Vector3(0f, offset, 0f);
        }

        public IEnumerator MoveToFormation(float worldX, float feetY, float duration)
        {
            if (spriteRenderer == null)
            {
                transform.position = new Vector3(worldX, transform.position.y, transform.position.z);
                yield break;
            }

            var start = transform.position;
            var targetY = transform.position.y + feetY - spriteRenderer.bounds.min.y;
            var target = new Vector3(worldX, targetY, start.z);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            transform.position = target;
            AlignFeetTo(feetY);
        }

        public IEnumerator PlayAttackCue()
        {
            yield return Shake(0.18f, 0.055f);
        }

        public IEnumerator PlayHitCue()
        {
            yield return Shake(0.22f, 0.075f);
        }

        public IEnumerator PlayOverlay(Sprite overlaySprite, float duration)
        {
            yield return PlayOverlay(overlaySprite, duration, 0f, 80);
        }

        public IEnumerator PlayOverlay(Sprite overlaySprite, float duration, float worldScaleBonus, int sortingOrder)
        {
            if (spriteRenderer == null || overlayRenderer == null)
            {
                yield return new WaitForSecondsRealtime(duration);
                yield break;
            }

            overlaySprite = overlaySprite != null ? overlaySprite : spriteRenderer.sprite;
            var previousScale = overlayRenderer.transform.localScale;
            var previousPosition = overlayRenderer.transform.localPosition;
            var previousSortingOrder = overlayRenderer.sortingOrder;
            var baseWorldScale = Mathf.Max(0.1f, transform.lossyScale.x);
            var overlayScale = 1f + Mathf.Max(0f, worldScaleBonus) / baseWorldScale;

            spriteRenderer.enabled = false;
            overlayRenderer.sprite = overlaySprite;
            overlayRenderer.flipX = spriteRenderer.flipX;
            overlayRenderer.sortingOrder = sortingOrder;
            overlayRenderer.transform.localScale = Vector3.one * overlayScale;
            overlayRenderer.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            overlayRenderer.enabled = true;
            yield return new WaitForSecondsRealtime(duration);
            overlayRenderer.enabled = false;
            overlayRenderer.sortingOrder = previousSortingOrder;
            overlayRenderer.transform.localScale = previousScale;
            overlayRenderer.transform.localPosition = previousPosition;
            spriteRenderer.enabled = true;
        }

        public IEnumerator PlayDeathCue()
        {
            yield return Shake(0.22f, 0.08f);
        }

        public void ShowFloatingText(string text, Color color)
        {
            ShowFloatingText(text, color, 1f);
        }

        public void ShowFloatingText(string text, Color color, float sizeMultiplier)
        {
            var textObject = new GameObject("FloatingText");
            textObject.transform.position = transform.position + new Vector3(0f, 1.2f, -0.6f);
            textObject.transform.localScale = Vector3.one * Mathf.Max(0.2f, sizeMultiplier);
            textObject.AddComponent<FloatingCombatText>().Initialize(text, color, UnityEngine.Random.Range(0.5f, 0.8f), 0.65f);
        }

        public Sprite CurrentSprite => spriteRenderer != null ? spriteRenderer.sprite : null;

        private IEnumerator Shake(float duration, float strength)
        {
            var start = transform.localPosition;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localPosition = start + new Vector3(
                    UnityEngine.Random.Range(-strength, strength),
                    UnityEngine.Random.Range(-strength * 0.45f, strength * 0.45f),
                    0f);
                yield return null;
            }

            transform.localPosition = start;
        }

        private void CreateNameplate()
        {
            var canvasObject = new GameObject("Nameplate", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = new Vector3(0f, -0.24f, 0f);
            canvasObject.transform.localScale = Vector3.one * 0.01f;

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 12;

            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 52f);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.02f, 0.018f, 0.016f, 0.72f);

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(canvasObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 4f);
            textRect.offsetMax = new Vector2(-6f, -4f);

            nameLabel = textObject.GetComponent<Text>();
            nameLabel.alignment = TextAnchor.MiddleCenter;
            nameLabel.fontSize = 16;
            nameLabel.fontStyle = FontStyle.Bold;
            nameLabel.color = new Color(0.96f, 0.88f, 0.68f);
            nameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameLabel.verticalOverflow = VerticalWrapMode.Truncate;
            nameLabel.font = ChineseFontProvider.LegacyFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameLabel.raycastTarget = false;

            var healthBack = new GameObject("HealthBack", typeof(RectTransform), typeof(Image));
            healthBack.transform.SetParent(canvasObject.transform, false);
            var backRect = healthBack.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.anchoredPosition = new Vector2(0f, 5f);
            backRect.sizeDelta = new Vector2(146f, 6f);
            var healthBackImage = healthBack.GetComponent<Image>();
            healthBackImage.color = new Color(0.08f, 0.055f, 0.045f, 0.95f);
            healthBackImage.sprite = CombatUiAssetLoader.LoadSprite("Assets/Art/UI/Combat/ui_health_bar_frame.png", new Vector4(70f, 70f, 70f, 70f));
            healthBackImage.type = healthBackImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;

            var healthFillObject = new GameObject("HealthFill", typeof(RectTransform), typeof(Image));
            healthFillObject.transform.SetParent(healthBack.transform, false);
            healthFill = healthFillObject.GetComponent<RectTransform>();
            healthFill.anchorMin = new Vector2(0f, 0.5f);
            healthFill.anchorMax = new Vector2(0f, 0.5f);
            healthFill.pivot = new Vector2(0f, 0.5f);
            healthFill.anchoredPosition = Vector2.zero;
            healthFill.sizeDelta = new Vector2(146f, 4f);
            healthFillObject.GetComponent<Image>().color = new Color(0.68f, 0.08f, 0.06f, 1f);
        }

        private void CreatePositionBadge()
        {
            var canvasObject = new GameObject("PositionBadge", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = new Vector3(0f, -0.78f, 0f);
            canvasObject.transform.localScale = Vector3.one * 0.01f;

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 14;

            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(48f, 36f);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.07f, 0.048f, 0.038f, 0.92f);
            backgroundImage.sprite = CombatUiAssetLoader.LoadSprite("Assets/Art/UI/Combat/ui_position_badge_frame.png", new Vector4(48f, 48f, 48f, 48f));
            backgroundImage.type = backgroundImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(canvasObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            positionLabel = textObject.GetComponent<Text>();
            positionLabel.alignment = TextAnchor.MiddleCenter;
            positionLabel.fontSize = 20;
            positionLabel.fontStyle = FontStyle.Bold;
            positionLabel.color = new Color(0.97f, 0.82f, 0.45f);
            positionLabel.font = ChineseFontProvider.LegacyFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            positionLabel.raycastTarget = false;
        }

        private void CreateClickCollider()
        {
            var collider = gameObject.AddComponent<BoxCollider2D>();
            var bounds = spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            collider.offset = bounds.center;
            collider.size = bounds.size;
        }

        private void OnMouseDown()
        {
            if (unit != null && unit.IsAlive)
            {
                clicked?.Invoke(unit);
            }
        }

        private static Sprite GetInitialSprite(BattleUnit battleUnit, Sprite fallbackSprite)
        {
            if (battleUnit.Definition.idleAnimationFrames != null && battleUnit.Definition.idleAnimationFrames.Length > 0)
            {
                return battleUnit.Definition.idleAnimationFrames[0];
            }

            return battleUnit.Definition.battleSprite != null ? battleUnit.Definition.battleSprite : fallbackSprite;
        }
    }
}
