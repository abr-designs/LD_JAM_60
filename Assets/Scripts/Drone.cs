using System.Numerics;
using UnityEngine;
using UnityEngine.AI; // Required for NavMesh components

[RequireComponent(typeof(NavMeshAgent))]
public class Drone : MonoBehaviour
{
    [SerializeField]
    private Transform[] patrolPoints;

    private NavMeshAgent agent;

    private int index = 0;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.destination = patrolPoints[index].position;
    }

    void Update()
    {
        Patrol();
    }

    void Patrol()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            index = (index + 1) % patrolPoints.Length;
            Debug.Log("Patrolling to point: " + index);
            agent.destination = patrolPoints[index].position;
        }
    }

    void Seek()
    {

    }

    void Alert()
    {

    }


}