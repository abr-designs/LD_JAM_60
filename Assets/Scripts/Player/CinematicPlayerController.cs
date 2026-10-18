using UnityEngine;

public class CinematicPlayerController : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    private const string GROUND_SIT_ANIMATION = "groundSit";
    private const string GROUND_SIT_EXIT_ANIMATION = "groundSitExit";
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator.Play(stateName: GROUND_SIT_ANIMATION);
    }

    [ContextMenu("Stand")]
    public void Stand()
    {
        animator.Play(stateName: GROUND_SIT_EXIT_ANIMATION);
    }

}
