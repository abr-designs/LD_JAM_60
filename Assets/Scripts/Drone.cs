using System.Numerics;
using UnityEngine;
using UnityEngine.AI; // Required for NavMesh components

[RequireComponent(typeof(NavMeshAgent))]
public class Drone : MonoBehaviour
{
    [SerializeField]
    private Transform[] patrolPoints;

    private NavMeshAgent agent;

    [SerializeField]
    private GameObject player;
    [SerializeField]
    private float seek_distance = 10f;
    [SerializeField]
    private float alert_distance = 4f;

    [SerializeField]
    private Material material;
    private int index = 0;

    private DroneState state;
    enum DroneState
    {
        Patrol,
        Seek,
        Alert
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.destination = patrolPoints[index].position;
        state = DroneState.Patrol;
        //material = GetComponent<Renderer>().material;
    }

    void Update()
    {
        float distance = UnityEngine.Vector3.Distance(agent.transform.position, player.transform.position);

        switch (state)
        {
            case DroneState.Patrol:
                Patrol(distance);
                break;
            case DroneState.Seek:
                Seek(distance);
                break;
            case DroneState.Alert:
                Alert(distance);
                break;
        }
        // if (distance < seek_distance)
        // {
        //     if (distance < alert_distance)
        //     {
        //         Alert();
        //     }
        //     else
        //     {
        //         Debug.Log("Seeking player");
        //         Seek();
        //     }

        // }
        // else
        // {
        //     Patrol();
        // }
    }

    void Patrol(float distance)
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            index = (index + 1) % patrolPoints.Length;
            agent.destination = patrolPoints[index].position;
        }

        if (distance < seek_distance)
        {
            state = DroneState.Seek;
        }
    }
    float wait_time = 1.0f;
    float timer = 0.0f;
    void Seek(float distance)
    {
        Debug.Log("Seeking player");

        timer += Time.deltaTime;

        if (timer > wait_time)
        {
            agent.destination = player.transform.position;
            timer = 0.0f;
        }
        if (distance < alert_distance)
        {
            state = DroneState.Alert;
        }
        else if (distance > seek_distance)
        {
            state = DroneState.Patrol;
        }
    }

    float flash_time = 0.5f;
    float flash_timer = 0.0f;

    bool flashing_on = false;

    float alert_wait_time = 5.0f;
    float alert_wait_timer = 0.0f;
    void Alert(float distance)
    {
        agent.isStopped = true;

        flash_timer += Time.deltaTime;
        alert_wait_timer += Time.deltaTime;

        if (flash_timer > flash_time)
        {

            flashing_on = !flashing_on;
            flash_timer = 0.0f;
            if (flashing_on)
            {
                material.SetColor("_EmissionColor", Color.red);
            }
            else
            {
                material.SetColor("_EmissionColor", Color.black);
            }
        }
        if (alert_wait_timer > alert_wait_time)
        {
            if (distance > alert_distance)
            {
                alert_wait_timer = 0.0f;
                material.SetColor("_EmissionColor", Color.black);
                agent.isStopped = false;
                state = DroneState.Seek;
            }
        }
        //material.DisableKeyword("_EMISSION");
        //material.EnableKeyword("_EMISSION");
    }
    private float arrest_time = 2.0f;
    private float arrest_timer = 0.0f;
    void Arrest()
    {
        arrest_timer += Time.deltaTime;
        if (arrest_timer > arrest_time)
        {
            // Handle arrest logic
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying == false)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(agent.destination, 0.25f);
    }

}

