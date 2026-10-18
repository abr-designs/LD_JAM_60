using System;
using System.Collections.Generic;
using ldjam60.ScriptableObjects;
using UnityEngine;

namespace ldjam60.Interactables
{

    [Serializable]
    public class DriveFileList
    {
        public List<FileDefinitionScriptableObject> files = new();
    } 

    public class InteractableTerminal : InteractableBase
    {
        public override bool IsInteractable => CanInteract();

        [SerializeField]
        private InteractablePort drivePortInteractable;

        private InteractableDrive MountedDrive => drivePortInteractable.Mounted;

        private int _currentDriveIndex = 0;
        [SerializeField]
        private List<DriveFileList> driveFileLists = new();

        //IInteractable Functions
        //================================================================================================================//

        public override void Interact()
        {
            if(_currentDriveIndex >= driveFileLists.Count)
                return;

            UIController.Instance.MountDrive(driveFileLists[_currentDriveIndex++].files, DestroyDrive);
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