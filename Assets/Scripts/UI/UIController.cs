using System.Collections.Generic;
using GameInput;
using ldjam60.Files;
using ldjam60.Interactables;
using ldjam60.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utilities;

public class UIController : Singleton<UIController>
{
    [SerializeField] private GameObject window;

    [Header("File Explorer Contents")]
    [SerializeField] private FileUIElement fileUIElementPrefab;
    [SerializeField] private RectTransform fileContainerRectTransform;
    [SerializeField] private TMP_Text fileExplorerHeaderText;

    [Header("File Contents")]
    [SerializeField] private TMP_Text fileContentsHeaderText;
    [SerializeField] private TMP_Text fileContentsText;
    [SerializeField] private Image fileContentsImage;
    [SerializeField] private ScrollRect fileContentsScrollRect;
    [SerializeField] private Button[] buttons;

    [Header("Misc")]
    [SerializeField] private TMP_Text progressBarText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameEndUIController gameEndUI;

    private readonly List<FileUIElement> m_activeFileUIElements = new();
    private InteractableTerminal m_terminal;
    private FileUIElement m_selectedElement;
    private int m_selectedDriveFileIndex = -1;
    private bool m_showingDrive;
    private bool m_windowOpen;
    private bool m_isTransitioning;
    private char m_driveLetter;

    private Button m_processButton;
    private Button m_ejectButton;
    private Button m_viewToggleButton;
    private TMP_Text m_processButtonText;
    private TMP_Text m_ejectButtonText;
    private TMP_Text m_viewToggleText;

    private void Start()
    {
        SetupButtons();
        exitButton.onClick.AddListener(CloseTerminal);
        SetWindowActive(false);
        UpdateProgress();
    }

    private void OnDestroy()
    {
        if (!m_windowOpen)
            return;

        GameInputDelegator.SetInputLock(false);
        InteractableController.PauseScan = false;
    }

    private void Update()
    {
        if (m_windowOpen && !m_isTransitioning && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            CloseTerminal();
    }

    public void OpenTerminal(InteractableTerminal terminal)
    {
        m_terminal = terminal;
        m_driveLetter = (char)Random.Range('A', 'Z' + 1);
        UpdateProgress();
        SetWindowActive(true);

        if (m_terminal.HasMountedDrive)
            ShowDrive();
        else
            ShowLibrary();
    }

    private void SetupButtons()
    {
        if (buttons == null || buttons.Length < 3)
        {
            Debug.LogError("Terminal UI requires three configured action buttons.", this);
            return;
        }

        m_processButton = buttons[0];
        m_ejectButton = buttons[1];
        // Clone the header's Exit control so the view toggle uses the same
        // hover/pressed sprites and interaction feedback.
        buttons[2].gameObject.SetActive(false);
        m_viewToggleButton = Instantiate(exitButton, exitButton.transform.parent);
        m_viewToggleButton.name = "DriveLibraryToggleButton";

        m_processButtonText = m_processButton.GetComponentInChildren<TMP_Text>();
        m_ejectButtonText = m_ejectButton.GetComponentInChildren<TMP_Text>();
        m_viewToggleText = m_viewToggleButton.GetComponentInChildren<TMP_Text>();

        AddInvertedHoverText(m_processButton);
        AddInvertedHoverText(m_ejectButton);

        LayoutHeaderControls();
        LayoutEjectButton();

        m_viewToggleButton.onClick.RemoveAllListeners();
        m_ejectButton.onClick.RemoveAllListeners();

        m_viewToggleButton.onClick.AddListener(ToggleView);
        m_ejectButton.onClick.AddListener(EjectDrive);
    }

    private static void AddInvertedHoverText(Button button)
    {
        var hoverText = button.gameObject.AddComponent<ButtonTextHoverColor>();
        hoverText.Initialize(button);
    }

    private void LayoutHeaderControls()
    {
        var header = exitButton.transform.parent as RectTransform;
        if (header == null)
            return;

        PositionHeaderButton(m_viewToggleButton, header, 55f, 220f);

        var exitRect = (RectTransform)exitButton.transform;
        exitRect.anchorMin = new Vector2(1f, 0.5f);
        exitRect.anchorMax = new Vector2(1f, 0.5f);
        exitRect.pivot = new Vector2(1f, 0.5f);
        exitRect.anchoredPosition = new Vector2(-55f, 0f);
        exitRect.sizeDelta = new Vector2(160f, 60f);
    }

    private static void PositionHeaderButton(Button button, RectTransform header, float x, float width)
    {
        var rect = (RectTransform)button.transform;
        rect.SetParent(header, false);
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(width, 60f);
        rect.localScale = Vector3.one;
    }

    private void LayoutEjectButton()
    {
        var current = fileContainerRectTransform.parent;
        while (current != null && current.name != "FileExplorerRectArea")
            current = current.parent;

        if (current is not RectTransform fileExplorerRect)
            return;

        var ejectRect = (RectTransform)m_ejectButton.transform;
        ejectRect.SetParent(fileExplorerRect, false);
        ejectRect.anchorMin = new Vector2(0.5f, 0.5f);
        ejectRect.anchorMax = new Vector2(0.5f, 0.5f);
        ejectRect.pivot = new Vector2(0.5f, 0.5f);
        ejectRect.anchoredPosition = Vector2.zero;
        ejectRect.sizeDelta = new Vector2(220f, 74f);
        ejectRect.localScale = Vector3.one;
    }

    private void ShowDrive()
    {
        m_showingDrive = true;
        ResetFileView();
        UpdateTabs();

        if (m_terminal == null || !m_terminal.HasMountedDrive)
        {
            fileExplorerHeaderText.text = "Drive\n>NO MEDIA";
            fileContentsHeaderText.text = "NO DRIVE MOUNTED";
            fileContentsText.text = "Insert a drive into the terminal port to recover its files.";
            ConfigureDriveActions();
            RefreshFileContentsScroll();
            return;
        }

        fileExplorerHeaderText.text = $"Drive\n>{m_driveLetter}:/";
        var mountedFiles = m_terminal.MountedFiles;
        var firstUnprocessedIndex = -1;

        for (var i = 0; i < mountedFiles.Count; i++)
        {
            var index = i;
            var state = mountedFiles[index];
            if (state.Processed)
                continue;

            if (firstUnprocessedIndex < 0)
                firstUnprocessedIndex = index;

            var instance = Instantiate(fileUIElementPrefab, fileContainerRectTransform);
            m_activeFileUIElements.Add(instance);
            instance.Init(state.Definition, element => SelectDriveFile(element, index));
        }

        ConfigureDriveActions();

        if (m_activeFileUIElements.Count > 0)
        {
            SelectDriveFile(m_activeFileUIElements[0], firstUnprocessedIndex);
        }
        else
        {
            fileContentsHeaderText.text = "DRIVE PROCESSED";
            fileContentsText.text = "All files have been recovered. Eject the empty drive to continue.";
            RefreshFileContentsScroll();
        }
    }

    private void ShowLibrary()
    {
        m_showingDrive = false;
        ResetFileView();
        UpdateTabs();
        fileExplorerHeaderText.text = "Archive\n>LIBRARY";

        if (m_terminal == null || m_terminal.LibraryFiles.Count == 0)
        {
            fileContentsHeaderText.text = "NO RECOVERED FILES";
            fileContentsText.text = "Recover text files from a mounted drive to add them to this library.";
            ConfigureLibraryActions();
            RefreshFileContentsScroll();
            return;
        }

        foreach (var file in m_terminal.LibraryFiles)
        {
            var definition = file;
            var instance = Instantiate(fileUIElementPrefab, fileContainerRectTransform);
            m_activeFileUIElements.Add(instance);
            instance.Init(definition, element => SelectLibraryFile(element, definition));
        }

        ConfigureLibraryActions();
        SelectLibraryFile(m_activeFileUIElements[0], m_terminal.LibraryFiles[0]);
    }

    private void SelectDriveFile(FileUIElement element, int index)
    {
        SetSelectedElement(element);
        m_selectedDriveFileIndex = index;

        var state = m_terminal.MountedFiles[index];
        var file = state.Definition;
        fileContentsHeaderText.text = file.FileName;
        fileContentsImage.sprite = null;
        fileContentsImage.gameObject.SetActive(false);

        m_processButton.onClick.RemoveAllListeners();
        m_processButton.gameObject.SetActive(!state.Processed);

        if (state.Processed)
        {
            fileContentsText.text = file.FileType == FILE_TYPE.ZIP
                ? "Archive successfully unzipped. Recovery progress updated."
                : "File recovered. Open the Library tab to read it.";
            RefreshFileContentsScroll();
            return;
        }

        switch (file.FileType)
        {
            case FILE_TYPE.TEXT:
                fileContentsText.text = "Text file detected. Recover it to make its contents readable in the Library.";
                m_processButtonText.text = "RECOVER";
                break;
            case FILE_TYPE.ZIP:
                fileContentsText.text = "Compressed recovery data detected.";
                m_processButtonText.text = "UNZIP";
                break;
            default:
                fileContentsText.text = "Unsupported file type.";
                m_processButton.gameObject.SetActive(false);
                RefreshFileContentsScroll();
                return;
        }

        m_processButton.onClick.AddListener(ProcessSelectedFile);
        RefreshFileContentsScroll();
    }

    private void SelectLibraryFile(FileUIElement element, FileDefinitionScriptableObject file)
    {
        SetSelectedElement(element);
        m_selectedDriveFileIndex = -1;
        fileContentsHeaderText.text = file.FileName;
        fileContentsText.text = file.FileContents;
        fileContentsImage.sprite = file.Image;
        fileContentsImage.gameObject.SetActive(file.Image != null);
        RefreshFileContentsScroll();
    }

    private void RefreshFileContentsScroll()
    {
        if (fileContentsScrollRect == null || fileContentsScrollRect.content == null)
            return;

        Canvas.ForceUpdateCanvases();

        var viewport = fileContentsScrollRect.viewport != null
            ? fileContentsScrollRect.viewport
            : (RectTransform)fileContentsScrollRect.transform;
        var content = fileContentsScrollRect.content;
        var textRect = fileContentsText.rectTransform;
        var availableTextWidth = Mathf.Max(1f, textRect.rect.width);
        var textHeight = fileContentsText.GetPreferredValues(
            fileContentsText.text,
            availableTextWidth,
            Mathf.Infinity).y;
        var imageHeight = fileContentsImage.gameObject.activeSelf
            ? fileContentsImage.rectTransform.rect.height
            : 0f;
        var requiredHeight = Mathf.Max(textHeight, imageHeight) + 40f;

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(viewport.rect.height, requiredHeight));

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        fileContentsScrollRect.horizontal = false;
        fileContentsScrollRect.vertical = true;
        fileContentsScrollRect.scrollSensitivity = 35f;
        fileContentsScrollRect.StopMovement();
        fileContentsScrollRect.verticalNormalizedPosition = 1f;
    }

    private void SetSelectedElement(FileUIElement element)
    {
        if (m_selectedElement != null)
            m_selectedElement.SetSelected(false);

        m_selectedElement = element;
        m_selectedElement.SetSelected(true);
    }

    private void ProcessSelectedFile()
    {
        if (m_terminal == null || m_selectedDriveFileIndex < 0)
            return;

        if (!m_terminal.ProcessFile(m_selectedDriveFileIndex))
            return;

        ClearUiFocus();
        UpdateProgress();
        ShowDrive();
    }

    private void EjectDrive()
    {
        if (m_terminal == null || !m_terminal.EjectDrive())
            return;

        ClearUiFocus();
        UpdateProgress();
        ShowLibrary();
    }

    private void ConfigureDriveActions()
    {
        m_processButton.onClick.RemoveAllListeners();
        m_processButton.gameObject.SetActive(false);

        var canEject = m_terminal != null && m_terminal.CanEject;
        m_ejectButton.gameObject.SetActive(canEject);
        m_ejectButton.interactable = canEject;
        m_ejectButtonText.text = "EJECT";
    }

    private void ConfigureLibraryActions()
    {
        m_processButton.onClick.RemoveAllListeners();
        m_processButton.gameObject.SetActive(false);
        m_ejectButton.gameObject.SetActive(false);
    }

    private void UpdateTabs()
    {
        m_viewToggleText.text = m_showingDrive ? "LIBRARY" : "DRIVE";
    }

    private void ToggleView()
    {
        if (m_showingDrive)
            ShowLibrary();
        else
            ShowDrive();

        ClearUiFocus();
    }

    private void ResetFileView()
    {
        for (var i = m_activeFileUIElements.Count - 1; i >= 0; i--)
            Destroy(m_activeFileUIElements[i].gameObject);

        m_activeFileUIElements.Clear();
        m_selectedElement = null;
        m_selectedDriveFileIndex = -1;
        fileContentsHeaderText.text = string.Empty;
        fileContentsText.text = string.Empty;
        fileContentsImage.sprite = null;
        fileContentsImage.gameObject.SetActive(false);
    }

    private void UpdateProgress()
    {
        var percent = m_terminal == null ? 0f : m_terminal.RecoveryPercent;
        progressBar.minValue = 0f;
        progressBar.maxValue = 100f;
        progressBar.value = percent;
        progressBarText.text = $"data recovery {Mathf.RoundToInt(percent)}%";
    }

    private void CloseTerminal()
    {
        if (m_isTransitioning)
            return;

        ClearUiFocus();

        if (m_terminal != null && m_terminal.IsWinReady)
        {
            if (gameEndUI == null)
            {
                Debug.LogError("Terminal UI requires a Game End UI Controller.", this);
                SetWindowActive(false);
                return;
            }

            m_isTransitioning = true;
            exitButton.interactable = false;
            window.SetActive(false);
            m_windowOpen = false;
            gameEndUI.ShowEndScreen(true);
            return;
        }

        SetWindowActive(false);
    }

    private static void ClearUiFocus()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void SetWindowActive(bool active)
    {
        window.SetActive(active);
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = active;

        if (active == m_windowOpen)
            return;

        m_windowOpen = active;
        GameInputDelegator.SetInputLock(active);
        InteractableController.PauseScan = active;
    }
}

internal sealed class ButtonTextHoverColor : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    private Button m_button;
    private TMP_Text m_text;
    private Color m_normalTextColor;
    private Color m_hoverTextColor;

    public void Initialize(Button button)
    {
        m_button = button;
        m_text = button.GetComponentInChildren<TMP_Text>();
        if (m_text == null)
            return;

        m_normalTextColor = m_text.color;
        // Match the dark green used by the Exit button label.
        m_hoverTextColor = new Color(0.13333334f, 0.23137257f, 0.14901961f, 1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_button != null && m_button.interactable && m_text != null)
            m_text.color = m_hoverTextColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreNormalColor();
        ClearFocus();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ClearFocus();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ClearFocus();
    }

    private void OnDisable()
    {
        RestoreNormalColor();
    }

    private void RestoreNormalColor()
    {
        if (m_text != null)
            m_text.color = m_normalTextColor;
    }

    private void ClearFocus()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
