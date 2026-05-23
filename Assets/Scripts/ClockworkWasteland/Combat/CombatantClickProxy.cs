using UnityEngine;

namespace ClockworkWasteland.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CombatantClickProxy : MonoBehaviour
    {
        private CombatantView owner;

        public void Bind(CombatantView view)
        {
            owner = view;
        }

        private void Awake()
        {
            if (owner == null)
            {
                owner = GetComponentInParent<CombatantView>();
            }
        }

        private void OnMouseDown()
        {
            if (owner == null)
            {
                owner = GetComponentInParent<CombatantView>();
            }

            owner?.NotifyClicked();
        }
    }
}