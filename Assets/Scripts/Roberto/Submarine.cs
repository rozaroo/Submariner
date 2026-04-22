using UnityEngine;
using UnityEngine.AI;

public class Submarine : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform[] destinations;
    int currentTarget = -1;
    public enum SubmarineState { Idle, Moving, Braking }
    public SubmarineState currentState = SubmarineState.Idle;
    int currentIndex = 0;
    bool engineEnabled = true;
    float currentSpeed;
    public float deceleration = 2f;
    public float baseSpeed = 5f;
    float speedMultiplier = 1f;

    void Start()
    {
        if (destinations.Length > 0) StartTravel(0);
        agent.speed = baseSpeed;
    }
    void Update()
    {
        if (!engineEnabled && currentState == SubmarineState.Moving)
        {
            HandleInertia();
            return;
        }
        if (currentState == SubmarineState.Moving) 
        {
            if (!agent.pathPending && agent.remainingDistance < 1f) StopSub();
        }
    }
    public void StartTravel(int index) 
    { 
        if (!engineEnabled) return;
        if (index < 0 || index >= destinations.Length) return;
        currentTarget = index;
        agent.isStopped = false;
        currentSpeed = agent.speed;
        agent.SetDestination(destinations[index].position);
        currentState = SubmarineState.Moving;
    }
    public void StopSub() 
    {
        agent.isStopped = true;
        currentState = SubmarineState.Idle;
    }
    public void Brake() 
    {
        currentState = SubmarineState.Braking;
        agent.isStopped = true;
    }
    public void GoToCurrentDestination()
    {
        StartTravel(currentIndex);
    }
    public void SetEngineState(bool state)
    {
        engineEnabled = state;
        if (!engineEnabled)
        {
            agent.isStopped = true;
            currentState = SubmarineState.Idle;
        }
        else
        {
            agent.isStopped = false;
            agent.speed = baseSpeed * speedMultiplier;
        }
    }
    void HandleInertia()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
        agent.speed = currentSpeed;
        if (currentSpeed <= 0.1f) StopSub();
    }
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
        if (engineEnabled) agent.speed = baseSpeed * speedMultiplier;
    }
}
