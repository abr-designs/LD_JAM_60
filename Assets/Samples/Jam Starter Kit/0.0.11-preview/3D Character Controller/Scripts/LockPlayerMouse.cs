using UnityEngine;

namespace Samples.CharacterController3D.Scripts
{
    public class LockPlayerMouse : MonoBehaviour
    {
        [SerializeField]
        private CursorLockMode stateOnStart = CursorLockMode.Confined;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            Cursor.lockState = stateOnStart;
        }

    }
}
