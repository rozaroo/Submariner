using System;

[Serializable]
public struct OnHullPropertyChange : IGameEvent
{
    public float maxHullDamagePosible;
    public float activeHullDamage;
}