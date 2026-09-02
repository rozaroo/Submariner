using UnityEngine;

public class EngineMiniGameActivator : MonoBehaviour
{
    [SerializeField] private EngineMiniGame engineMiniGame;
    private bool _playerInside;

    public void Activate(PlayerCharacter player)
    {
        if (engineMiniGame == null)
        {
            Debug.LogError("[ENGINE MINIGAME] EngineMiniGame no está asignado.");
            return;
        }

        engineMiniGame.StartMinigame();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerCharacter>(out _))  _playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
       if (other.TryGetComponent<PlayerCharacter>(out _)) _playerInside = false;
    }
}
