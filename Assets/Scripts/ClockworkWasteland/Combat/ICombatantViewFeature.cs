namespace ClockworkWasteland.Combat
{
    public interface ICombatantViewFeature
    {
        void Bind(CombatantView view, BattleUnit unit);
        void Refresh(BattleUnit unit);
    }
}
