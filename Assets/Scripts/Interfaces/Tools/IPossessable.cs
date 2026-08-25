using UnityEngine;

public interface IPossessable
{
    string MapName { get; }
    
    void Possess(PlayerCharacter player);
    void UnPossess();
}
