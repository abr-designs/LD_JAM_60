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

    [SerializeField]
    private Mech mech;
    private DroneState state;


    private Color Yellowish = new Color(191, 109, 22);
    enum DroneState
    {
        Patrol,
        Seek,
        Alert,
        Arrest
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
            case DroneState.Arrest:
                Arrest();
                break;
        }
        //Debug.Log(state);
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
        material.SetColor("_EmissionColor", Yellowish);
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            index = (index + 1) % patrolPoints.Length;
            agent.destination = patrolPoints[index].position;
        }
        Ray ray = new Ray(transform.position + new UnityEngine.Vector3(0, 1, 0), transform.forward);
        RaycastHit hit;
        if (Physics.SphereCast(transform.position + new UnityEngine.Vector3(0, 1, 1.5f), 1f, transform.forward, out hit, alert_distance))
        {
            // Check if the hit object has the "Player" tag
            if (hit.collider.CompareTag("Player"))
            {
                state = DroneState.Seek;
            }

        }
        if (distance < seek_distance)
        {

        }
    }
    float wait_time = 1.0f;
    float timer = 0.0f;
    void Seek(float distance)
    {
        material.SetColor("_EmissionColor", Yellowish);
        //Debug.Log("Seeking player");

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
        // Ray ray = new Ray(transform.position + new UnityEngine.Vector3(0, 1, 0), transform.forward);
        // RaycastHit hit;
        // if (!Physics.Raycast(ray, out hit, alert_distance))
        // {
        //     state = DroneState.Patrol;
        // }
        // else
        // {
        //     Debug.DrawRay(transform.position, transform.forward * alert_distance, Color.red);
        // }
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


        if (distance < alert_distance)
        {
            mech.total_timer += Time.deltaTime;
            float t = mech.total_timer / mech.TimeUntilDeath;
            mech.transform.position = UnityEngine.Vector3.Lerp(mech.MechStartPosition, mech.MechEndPosition, t);
            if (mech.total_timer >= mech.TimeUntilDeath)
            {
                state = DroneState.Arrest;
            }
        }
        if (flash_timer > flash_time)
        {

            flashing_on = !flashing_on;
            flash_timer = 0.0f;
            if (flashing_on)
            {
                Color finalColor = Color.red * Mathf.Pow(2, 6);
                material.SetColor("_EmissionColor", finalColor);
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
            // Arrest logic here
            Debug.Log("Player arrested!");

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

