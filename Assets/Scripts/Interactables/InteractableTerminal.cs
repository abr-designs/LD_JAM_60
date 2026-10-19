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
        public bool HasMountedDrive => MountedDrive != null && m_currentDriveIndex < EffectiveDriveCount;
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
        public bool IsWinReady
        {
            get
            {
                if (RecoveryPercent < 100f || EffectiveDriveCount == 0)
                    return false;

                var finalDriveIndex = EffectiveDriveCount - 1;
                var finalDriveProcessed = HasMountedDrive
                                          && m_currentDriveIndex == finalDriveIndex
                                          && MountedFiles.Count > 0
                                          && MountedFiles.All(file => file.Processed);
                var finalDriveEjected = !HasMountedDrive && m_currentDriveIndex >= EffectiveDriveCount;
                return finalDriveProcessed || finalDriveEjected;
            }
        }

        [SerializeField] private InteractablePort drivePortInteractable;
        [SerializeField, Tooltip("Files available in the terminal library at the start of the game.")]
        private List<FileDefinitionScriptableObject> startingLibraryFiles = new();
        [SerializeField] private List<DriveFileList> driveFileLists = new();

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField, Tooltip("In Editor play mode, treat one configured drive as the complete recovery run.")]
        private bool testWithSingleDrive;
        [SerializeField, Min(0), Tooltip("Index in Drive File Lists used by the one-drive test run.")]
        private int debugDriveIndex;
#endif

        private InteractableDrive MountedDrive => drivePortInteractable.Mounted;
        private int m_currentDriveIndex;
        private int m_recoveredZipWeight;
        private int m_totalZipWeight;
        private readonly List<TerminalFileState> m_mountedFiles = new();
        private readonly List<FileDefinitionScriptableObject> m_libraryFiles = new();

        private int EffectiveDriveCount
        {
            get
            {
#if UNITY_EDITOR
                if (testWithSingleDrive && driveFileLists.Count > 0)
                    return 1;
#endif
                return driveFileLists.Count;
            }
        }

        private void Awake()
        {
            foreach (var file in startingLibraryFiles)
            {
                if (file != null && file.FileType == FILE_TYPE.TEXT && !m_libraryFiles.Contains(file))
                    m_libraryFiles.Add(file);
            }

            m_totalZipWeight = Enumerable.Range(0, EffectiveDriveCount)
                .SelectMany(index => GetDriveFiles(index))
                .Where(file => file != null && file.FileType == FILE_TYPE.ZIP)
                .Sum(file => Mathf.Max(0, file.ProgressTowardsObjective));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            debugDriveIndex = driveFileLists.Count == 0
                ? 0
                : Mathf.Clamp(debugDriveIndex, 0, driveFileLists.Count - 1);
        }
#endif

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

            foreach (var file in GetDriveFiles(m_currentDriveIndex))
                m_mountedFiles.Add(new TerminalFileState(file));
        }

        private IReadOnlyList<FileDefinitionScriptableObject> GetDriveFiles(int effectiveIndex)
        {
#if UNITY_EDITOR
            if (testWithSingleDrive && driveFileLists.Count > 0)
                return driveFileLists[Mathf.Clamp(debugDriveIndex, 0, driveFileLists.Count - 1)].files;
#endif
            return driveFileLists[effectiveIndex].files;
        }
    }
}
