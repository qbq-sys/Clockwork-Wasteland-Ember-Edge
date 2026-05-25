using UnityEngine;

namespace ClockworkWasteland.Combat
{
    [CreateAssetMenu(menuName = "Clockwork Wasteland/Combat/Buff Data")]
    public sealed class BuffData : ScriptableObject
    {
        public string buffId = "buff";
        public string buffName = "Buff";
        [TextArea]
        public string description;
        public int duration = 1;
        public bool stun;
        public int tickDamage;
        public Color nameTextColor = Color.white;
        public Color damageTextColor = new Color(1f, 0.3f, 0.3f, 1f);
        public Sprite icon;
    }
}
