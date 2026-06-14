using System;

[Serializable]
public struct OnHullPropertyChange : IGameEvent
{
    public float maxHullDamagePosible;
    public float activeHullDamage;

    public OnHullPropertyChange(float maxHullDamagePosible, float activeHullDamage)
    {
        this.maxHullDamagePosible = maxHullDamagePosible;
        this.activeHullDamage = activeHullDamage;
    }
}