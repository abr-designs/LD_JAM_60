using System.Collections;
using GameInput;
using UnityEngine;
using Utilities;

public class CinematicController : MonoBehaviour
{
    [SerializeField] 
    private GameObject playerGameObject;
    [SerializeField]
    private CinematicPlayerController cinematicPlayerController;
    [SerializeField] 
    private GameObject playerCamera;

    [SerializeField, Min(0f)]
    private float standTime;
    
    [SerializeField] 
    private GameObject cinematicPlayerGameObject;
    [SerializeField] 
    private GameObject cinematicPlayerCamera;
    [SerializeField]
    private GameObject mainMenuUI;
    [SerializeField]
    private CanvasGroup canvasGroup;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameInputDelegator.SetInputLock(true);
        playerGameObject.SetActive(false);
        playerCamera.gameObject.SetActive(false);
        
        cinematicPlayerGameObject.SetActive(true);
        cinematicPlayerCamera.SetActive(true);
        mainMenuUI.SetActive(true);
    }

    public void OnPlayPressed()
    {
        mainMenuUI.gameObject.SetActive(false);
        StartCoroutine(PlayCoroutine());
        StartCoroutine(MainMenuFadeCoroutine());

        IEnumerator PlayCoroutine()
        {
            cinematicPlayerController.Stand();
            
            
            yield return new WaitForSeconds(standTime);
            
            ScreenFader.FadeInOut(1f, () =>
            {
                playerGameObject.SetActive(true);
                playerCamera.gameObject.SetActive(true);
        
                cinematicPlayerGameObject.SetActive(false);
                cinematicPlayerCamera.SetActive(false);
                mainMenuUI.SetActive(false);
            }, () =>
            {
                GameInputDelegator.SetInputLock(false);
            });
        }

        IEnumerator MainMenuFadeCoroutine()
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                canvasGroup.alpha = 1f - t;
                yield return null;
            }
        }
    }
}
