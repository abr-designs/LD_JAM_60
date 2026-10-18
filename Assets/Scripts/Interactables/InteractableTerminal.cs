using UnityEngine;

namespace ldjam60.Interactables
{
    public class InteractableTerminal : InteractableBase
    {
        public override bool IsInteractable => CanInteract();

        [SerializeField]
        private InteractablePort drivePortInteractable;

        private InteractableDrive MountedDrive => drivePortInteractable.Mounted;

        //IInteractable Functions
        //================================================================================================================//

        public override void Interact()
        {
            //TODO Fade into the 
            UIController.Instance.MountDrive(MountedDrive, DestroyDrive);
        }

        public void DestroyDrive()
        {
            Debug.Log("Drive Destroyed...");
            Destroy(MountedDrive.gameObject);
            //drivePortInteractable.Mounted = null;
        }

        private bool CanInteract()
        {
            //I don't think I want the player to be able to interact with the terminal if their hands are busy
            if(InteractableController.HoldingInteractable != null)
                return false;
            
            if(!drivePortInteractable.Mounted)
                return false;

            return true;
        }
        //================================================================================================================//

    }
}