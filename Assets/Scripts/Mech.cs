using UnityEngine;

public class Mech : MonoBehaviour
{

    [SerializeField]
    public float TimeUntilDeath = 10.0f;
    public float total_timer = 0.0f;

    public UnityEngine.Vector3 MechStartPosition;
    public UnityEngine.Vector3 MechEndPosition = new UnityEngine.Vector3(0, 0, 0);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MechStartPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
