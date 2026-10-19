using System;
using System.Collections.Generic;
using System.Linq;
using ldjam60.Files;
using ldjam60.ScriptableObjects;
using UnityEngine;

namespace ldjam60.Interactables
{
    [Serializable]
    public class DriveFileList
    {
        public List<FileDefinitionScriptableObject> files = new();
    }

    public sealed class TerminalFileState
    {
        public FileDefinitionScriptableObject Definition { get; }
        public bool Processed { get; private set; }

        public TerminalFileState(FileDefinitionScriptableObject definition)
        {
            Definition = definition;
        }

        public void MarkProcessed()
        {
            Processed = true;
        }
    }

    public class InteractableTerminal : InteractableBase
    {
        public override bool IsInteractable => InteractableController.HoldingInteractable == null;
        public bool HasMountedDrive => MountedDrive != null && m_currentDriveIndex < driveFileLists.Count;
        public bool CanEject => HasMountedDrive && MountedFiles.Count > 0 && MountedFiles.All(file => file.Processed);
        public IReadOnlyList<TerminalFileState> MountedFiles
        {
            get
            {
                EnsureMountedFileState();
                return m_mountedFiles;
            }
        }
        public IReadOnlyList<FileDefinitionScriptableObject> LibraryFiles => m_libraryFiles;
        public float RecoveryPercent => m_totalZipWeight <= 0
            ? 0f
            : Mathf.Clamp01((float)m_recoveredZipWeight / m_totalZipWeight) * 100f;

        [SerializeField] private InteractablePort drivePortInteractable;
        [SerializeField] private List<DriveFileList> driveFileLists = new();

        private InteractableDrive MountedDrive => drivePortInteractable.Mounted;
        private int m_currentDriveIndex;
        private int m_recoveredZipWeight;
        private int m_totalZipWeight;
        private readonly List<TerminalFileState> m_mountedFiles = new();
        private readonly List<FileDefinitionScriptableObject> m_libraryFiles = new();

        private void Awake()
        {
            m_totalZipWeight = driveFileLists
                .SelectMany(drive => drive.files)
                .Where(file => file != null && file.FileType == FILE_TYPE.ZIP)
                .Sum(file => Mathf.Max(0, file.ProgressTowardsObjective));
        }

        public override void Interact()
        {
            EnsureMountedFileState();
            UIController.Instance.OpenTerminal(this);
        }

        public bool ProcessFile(int fileIndex)
        {
            EnsureMountedFileState();

            if (!HasMountedDrive || fileIndex < 0 || fileIndex >= m_mountedFiles.Count)
                return false;

            var fileState = m_mountedFiles[fileIndex];
            if (fileState.Processed || fileState.Definition == null)
                return false;

            switch (fileState.Definition.FileType)
            {
                case FILE_TYPE.TEXT:
                    m_libraryFiles.Add(fileState.Definition);
                    break;
                case FILE_TYPE.ZIP:
                    m_recoveredZipWeight += Mathf.Max(0, fileState.Definition.ProgressTowardsObjective);
                    break;
                default:
                    return false;
            }

            fileState.MarkProcessed();
            return true;
        }

        public bool EjectDrive()
        {
            if (!CanEject)
                return false;

            var drive = MountedDrive;
            m_currentDriveIndex++;
            m_mountedFiles.Clear();

            Destroy(drive.gameObject);
            drivePortInteractable.enabled = true;
            return true;
        }

        private void EnsureMountedFileState()
        {
            if (!HasMountedDrive || m_mountedFiles.Count > 0)
                return;

            foreach (var file in driveFileLists[m_currentDriveIndex].files)
                m_mountedFiles.Add(new TerminalFileState(file));
        }
    }
}
