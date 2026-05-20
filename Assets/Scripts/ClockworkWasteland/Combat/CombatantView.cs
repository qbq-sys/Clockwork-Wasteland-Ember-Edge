using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

namespace ClockworkWasteland.Combat
{
    public sealed class CombatantView : MonoBehaviour
    {
        [Header("Prefab Hooks")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer overlayRenderer;
        [SerializeField] private Transform nameplatePosition;

        private CombatNameplate nameplate;
        private BattleUnit unit;
        private Sprite[] idleFrames;
        private float animationTimer;
        private int animationFrame;
        private Action<BattleUnit> clicked;
        private Vector3 baseLocalScale;
        private int baseSortingOrder;

        public BattleUnit Unit => unit;

        public void Initialize(BattleUnit battleUnit, Sprite fallbackSprite, float scaleMultiplier, Action<BattleUnit> onClicked, CombatNameplate nameplatePrefab)
        {
            unit = battleUnit;
            clicked = onClicked;

            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            idleFrames = battleUnit.Definition.idleAnimationFrames;
            spriteRenderer.sprite = GetInitialSprite(battleUnit, fallbackSprite);
            spriteRenderer.color = battleUnit.Definition.tint;
            if (spriteRenderer.sortingOrder == 0)
            {
                spriteRenderer.sortingOrder = 2;
            }

            baseSortingOrder = spriteRenderer.sortingOrder;

            EnsureOverlayRenderer();
            EnsureNameplatePosition();

            var scale = Mathf.Max(0.1f, battleUnit.Definition.visualScale * scaleMultiplier);
            transform.localScale = new Vector3(scale, scale, 1f);
            baseLocalScale = transform.localScale;
            spriteRenderer.flipX = battleUnit.IsHero;

            AttachNameplate(nameplatePrefab);
            CreateClickCollider();
            Refresh();
        }

        public void Refresh()
        {
            if (unit == null)
            {
                return;
            }

            spriteRenderer.color = unit.IsCorpse ? new Color(0.32f, 0.3f, 0.28f, 0.78f) : unit.Definition.tint;
            nameplate?.Refresh(unit);
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

        public void SetCombatEmphasis(bool emphasized, int sortingOrder)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sortingOrder = emphasized ? sortingOrder : baseSortingOrder;
            transform.localScale = emphasized ? baseLocalScale * 1.06f : baseLocalScale;
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
            var previousSortingOrder = overlayRenderer.sortingOrder;
            var baseWorldScale = Mathf.Max(0.1f, transform.lossyScale.x);
            var overlayScale = 1f + Mathf.Max(0f, worldScaleBonus) / baseWorldScale;

            spriteRenderer.enabled = false;
            overlayRenderer.sprite = overlaySprite;
            overlayRenderer.flipX = spriteRenderer.flipX;
            overlayRenderer.sortingOrder = sortingOrder;
            overlayRenderer.transform.localScale = previousScale * overlayScale;
            overlayRenderer.enabled = true;
            yield return new WaitForSecondsRealtime(duration);
            overlayRenderer.enabled = false;
            overlayRenderer.sortingOrder = previousSortingOrder;
            overlayRenderer.transform.localScale = previousScale;
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

        private void EnsureOverlayRenderer()
        {
            if (overlayRenderer == null)
            {
                var overlayTransform = transform.Find("ActionOverlay");
                overlayRenderer = overlayTransform != null ? overlayTransform.GetComponent<SpriteRenderer>() : null;
            }

            if (overlayRenderer == null)
            {
                var overlayObject = new GameObject("ActionOverlay");
                overlayObject.transform.SetParent(transform, false);
                overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
                overlayRenderer.sortingOrder = 80;
            }

            overlayRenderer.enabled = false;
        }

        private void EnsureNameplatePosition()
        {
            if (nameplatePosition == null)
            {
                nameplatePosition = transform.Find("NameplatePosition");
            }

            if (nameplatePosition == null)
            {
                var positionObject = new GameObject("NameplatePosition");
                positionObject.transform.SetParent(transform, false);
                positionObject.transform.localPosition = new Vector3(0f, -0.24f, 0f);
                nameplatePosition = positionObject.transform;
            }
        }

        private void AttachNameplate(CombatNameplate nameplatePrefab)
        {
            nameplate = nameplatePrefab != null
                ? Instantiate(nameplatePrefab, nameplatePosition)
                : CreateFallbackNameplate(nameplatePosition);

            nameplate.transform.localPosition = Vector3.zero;
            nameplate.transform.localRotation = Quaternion.identity;
        }

        private static CombatNameplate CreateFallbackNameplate(Transform parent)
        {
            var canvasObject = new GameObject("Nameplate", typeof(RectTransform), typeof(Canvas), typeof(CombatNameplate));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localScale = Vector3.one * 0.01f;

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 12;

            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 78f);

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

            var nameLabel = textObject.GetComponent<Text>();
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
            var healthFill = healthFillObject.GetComponent<RectTransform>();
            healthFill.anchorMin = new Vector2(0f, 0.5f);
            healthFill.anchorMax = new Vector2(0f, 0.5f);
            healthFill.pivot = new Vector2(0f, 0.5f);
            healthFill.anchoredPosition = Vector2.zero;
            healthFill.sizeDelta = new Vector2(146f, 4f);
            healthFillObject.GetComponent<Image>().color = new Color(0.68f, 0.08f, 0.06f, 1f);

            var badge = new GameObject("PositionBadge", typeof(RectTransform));
            badge.transform.SetParent(canvasObject.transform, false);
            var badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 0f);
            badgeRect.anchorMax = new Vector2(0.5f, 0f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(0f, -18f);
            badgeRect.sizeDelta = new Vector2(48f, 36f);

            var badgeBackground = new GameObject("Background", typeof(RectTransform), typeof(Image));
            badgeBackground.transform.SetParent(badge.transform, false);
            var badgeBackgroundRect = badgeBackground.GetComponent<RectTransform>();
            badgeBackgroundRect.anchorMin = Vector2.zero;
            badgeBackgroundRect.anchorMax = Vector2.one;
            badgeBackgroundRect.offsetMin = Vector2.zero;
            badgeBackgroundRect.offsetMax = Vector2.zero;
            var badgeBackgroundImage = badgeBackground.GetComponent<Image>();
            badgeBackgroundImage.color = new Color(0.07f, 0.048f, 0.038f, 0.92f);
            badgeBackgroundImage.sprite = CombatUiAssetLoader.LoadSprite("Assets/Art/UI/Combat/ui_position_badge_frame.png", new Vector4(48f, 48f, 48f, 48f));
            badgeBackgroundImage.type = badgeBackgroundImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;

            var positionObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            positionObject.transform.SetParent(badge.transform, false);
            var positionRect = positionObject.GetComponent<RectTransform>();
            positionRect.anchorMin = Vector2.zero;
            positionRect.anchorMax = Vector2.one;
            positionRect.offsetMin = Vector2.zero;
            positionRect.offsetMax = Vector2.zero;

            var positionLabel = positionObject.GetComponent<Text>();
            positionLabel.alignment = TextAnchor.MiddleCenter;
            positionLabel.fontSize = 20;
            positionLabel.fontStyle = FontStyle.Bold;
            positionLabel.color = new Color(0.97f, 0.82f, 0.45f);
            positionLabel.font = ChineseFontProvider.LegacyFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            positionLabel.raycastTarget = false;

            var fallback = canvasObject.GetComponent<CombatNameplate>();
            fallback.BindFallbackReferences(nameLabel, positionLabel, healthFill);
            return fallback;
        }

        private void CreateClickCollider()
        {
            var collider = GetComponent<BoxCollider2D>() ?? gameObject.AddComponent<BoxCollider2D>();
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
