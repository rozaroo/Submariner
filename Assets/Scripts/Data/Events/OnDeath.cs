public struct OnDeath : IGameEvent
{
    public DeathType TypeOfDeath;

    public OnDeath(DeathType type)
    {
        TypeOfDeath = type;
    }
}
