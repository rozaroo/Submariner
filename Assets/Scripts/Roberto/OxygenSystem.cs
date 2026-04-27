using UnityEngine;

public class OxygenSystem : MonoBehaviour
{
    [Header("Oxígeno")]
    public float maxOxygen = 60f; // 1 minuto
    public float currentOxygen;

    [Header("Estado")]
    public bool isDraining = false;

    void Start()
    {
        currentOxygen = maxOxygen;
    }
    void Update()
    {
        if (isDraining)
        {
            currentOxygen -= Time.deltaTime;

            if (currentOxygen <= 0)
            {
                currentOxygen = 0;
                GameOver();
            }
        }
    }

    public void StartDrain()
    {
        isDraining = true;
        Debug.Log("Oxígeno bajando...");
    }

    public void StopDrain()
    {
        isDraining = false;
        Debug.Log("Oxígeno estabilizado");
    }

    public void RestoreOxygen(float amount)
    {
        currentOxygen += amount;
        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
    }

    void GameOver()
    {
        Debug.Log("GAME OVER - Sin oxígeno");
        // acá después conectanos UI / escena
    }
}