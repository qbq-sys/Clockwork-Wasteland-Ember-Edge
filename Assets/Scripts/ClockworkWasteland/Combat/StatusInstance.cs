namespace ClockworkWasteland.Combat
{
    public sealed class StatusInstance
    {
        public StatusInstance(string displayName, int turnsRemaining, int tickDamage, bool stun = false, BuffData sourceBuff = null)
        {
            DisplayName = displayName;
            TurnsRemaining = turnsRemaining;
            TickDamage = tickDamage;
            Stun = stun;
            SourceBuff = sourceBuff;
        }

        public string DisplayName { get; }
        public int TurnsRemaining { get; private set; }
        public int TickDamage { get; }
        public bool Stun { get; }
        public BuffData SourceBuff { get; }
        public UnityEngine.Color NameTextColor => SourceBuff != null ? SourceBuff.nameTextColor : UnityEngine.Color.white;
        public UnityEngine.Color DamageTextColor => SourceBuff != null ? SourceBuff.damageTextColor : new UnityEngine.Color(1f, 0.3f, 0.3f, 1f);
        public UnityEngine.Sprite Icon => SourceBuff != null ? SourceBuff.icon : null;

        public void AdvanceTurn()
        {
            TurnsRemaining--;
        }
    }
}
