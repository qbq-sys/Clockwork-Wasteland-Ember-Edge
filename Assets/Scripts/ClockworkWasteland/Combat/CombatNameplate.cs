using UnityEngine;
using UnityEngine.UI;

namespace ClockworkWasteland.Combat
{
    public sealed class CombatNameplate : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text positionText;
        [SerializeField] private RectTransform healthFill;

        public void BindFallbackReferences(Text nameLabel, Text positionLabel, RectTransform healthFillRect)
        {
            nameText = nameLabel;
            positionText = positionLabel;
            healthFill = healthFillRect;
        }

        public void Refresh(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = $"{unit.DisplayName}\n{unit.Health}/{unit.MaxHealth}";
            }

            if (positionText != null)
            {
                positionText.text = unit.CurrentPosition.ToString();
            }

            if (healthFill != null)
            {
                var healthPercent = unit.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)unit.Health / unit.MaxHealth);
                healthFill.localScale = new Vector3(healthPercent, 1f, 1f);
            }
        }
    }
}
