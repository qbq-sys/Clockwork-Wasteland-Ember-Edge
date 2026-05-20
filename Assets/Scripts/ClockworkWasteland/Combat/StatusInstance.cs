namespace ClockworkWasteland.Combat
{
    public sealed class StatusInstance
    {
        public StatusInstance(string displayName, int turnsRemaining, int tickDamage, bool stun = false)
        {
            DisplayName = displayName;
            TurnsRemaining = turnsRemaining;
            TickDamage = tickDamage;
            Stun = stun;
        }

        public string DisplayName { get; }
        public int TurnsRemaining { get; private set; }
        public int TickDamage { get; }
        public bool Stun { get; }

        public void AdvanceTurn()
        {
            TurnsRemaining--;
        }
    }
}
