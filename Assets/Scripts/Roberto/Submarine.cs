using UnityEngine;
using UnityEngine.AI;

public class Submarine : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform[] destinations;
    int currentTarget = -1;
    public enum SubmarineState { Idle, Moving, Braking }
    public SubmarineState currentState = SubmarineState.Idle;

    void Start()
    {
        if (destinations.Length > 0)
        {
            StartTravel(0);
        }
    }
    void Update()
    {
        if (currentState == SubmarineState.Moving) 
        {
            if (!agent.pathPending && agent.remainingDistance < 1f) StopSub();
        }
    }
    public void StartTravel(int index) 
    { 
        if (index < 0 || index >= destinations.Length) return;
        currentTarget = index;
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
}
