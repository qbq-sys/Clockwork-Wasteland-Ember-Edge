namespace ClockworkWasteland.Combat
{
    public sealed class StatusTickResult
    {
        public StatusTickResult(StatusInstance status, int damage)
        {
            Status = status;
            Damage = damage;
        }

        public StatusInstance Status { get; }
        public int Damage { get; }
    }
}
