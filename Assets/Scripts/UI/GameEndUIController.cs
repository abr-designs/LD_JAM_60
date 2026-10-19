using System.Collections;
using GameInput;
using ldjam60.Interactables;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utilities;

public sealed class GameEndUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject endCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject resultContainer;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Result Text")]
    [SerializeField, TextArea] private string winText = "DATA RECOVERY COMPLETE";
    [SerializeField, TextArea] private string loseText = "DATA RECOVERY FAILED";

    [Header("Transition")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.75f;

    [Header("Debug")]
    [SerializeField, Tooltip("Result used by Show Configured End Screen.")]
    private bool debugResultIsWin = true;

    private bool m_isShowing;
    private bool m_isTransitioning;
    private bool m_hasFrozenWorld;
    private Coroutine m_fadeCoroutine;

    private void OnEnable()
    {
        Mech.OnPlayerCaptured += ShowLoseScreen;
    }

    private void Awake()
    {
        restartButton.onClick.AddListener(RestartGame);

#if UNITY_WEBGL
        quitButton.gameObject.SetActive(false);
#else
        quitButton.onClick.AddListener(QuitGame);
#endif

        HideEndScreen();
    }

    private void Start()
    {
        // Keep the hierarchy enabled so Unity's Selectable registry stays stable.
        // Visibility and input are controlled exclusively through the CanvasGroup.
        if (!endCanvas.activeSelf)
            endCanvas.SetActive(true);
    }

    private void OnDestroy()
    {
        if (m_hasFrozenWorld)
            Time.timeScale = 1f;
    }

    private void OnDisable()
    {
        Mech.OnPlayerCaptured -= ShowLoseScreen;
    }

    public void ShowEndScreen(bool didWin)
    {
        if (m_isShowing || m_isTransitioning)
            return;

        m_isShowing = true;
        resultText.text = didWin ? winText : loseText;
        resultContainer.SetActive(true);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        GameInputDelegator.SetInputLock(true);
        InteractableController.PauseScan = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
        m_hasFrozenWorld = true;

        if (m_fadeCoroutine != null)
            StopCoroutine(m_fadeCoroutine);

        m_fadeCoroutine = StartCoroutine(FadeInCoroutine());
    }

    [ContextMenu("Show Configured End Screen")]
    public void ShowConfiguredEndScreen()
    {
        ShowEndScreen(debugResultIsWin);
    }

    private void ShowLoseScreen()
    {
        ShowEndScreen(false);
    }

    private IEnumerator FadeInCoroutine()
    {
        if (fadeDuration <= 0f)
        {
            CompleteFadeIn();
            yield break;
        }

        for (var elapsed = 0f; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        CompleteFadeIn();
    }

    private void CompleteFadeIn()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        m_fadeCoroutine = null;
    }

    private void HideEndScreen()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void RestartGame()
    {
        if (m_isTransitioning)
            return;

        m_isTransitioning = true;
        canvasGroup.interactable = false;
        ClearUiFocus();
        ClearEditorSelection();
        RestoreTimeScale();

        var activeSceneName = SceneManager.GetActiveScene().name;
        ScreenFader.FadeOut(1f, () => SceneManager.LoadScene(activeSceneName));
    }

    private void QuitGame()
    {
        if (m_isTransitioning)
            return;

        m_isTransitioning = true;
        ClearUiFocus();
        RestoreTimeScale();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = 1f;
        m_hasFrozenWorld = false;
    }

    private static void ClearUiFocus()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private static void ClearEditorSelection()
    {
#if UNITY_EDITOR
        UnityEditor.Selection.activeObject = null;
#endif
    }
}
