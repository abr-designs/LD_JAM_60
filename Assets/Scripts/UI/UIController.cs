using System;
using System.Collections.Generic;
using System.IO;
using ldjam60.Files;
using ldjam60.Interactables;
using ldjam60.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

public class UIController : Singleton<UIController>
{
    [SerializeField]
    private GameObject window;
    
    [Header("File Explorer Contents")]
    [SerializeField]
    private FileUIElement fileUIElementPrefab;
    [SerializeField]
    private RectTransform fileContainerRectTransform;
    [SerializeField]
    private TMP_Text fileExplorerHeaderText;
    private List<FileUIElement> m_activeFileUIElements;
    
    [Header("File Contents")]
    [SerializeField]
    private TMP_Text fileContentsHeaderText;
    [SerializeField]
    private TMP_Text fileContentsText;
    [SerializeField]
    private Image fileContentsImage;

    [SerializeField]
    private Button[] buttons;
    private TMP_Text[] m_buttonsTexts; 

    [Header("Misc")] 
    [SerializeField]
    private TMP_Text progressBarText;
    [SerializeField]
    private Slider progressBar;
    [SerializeField]
    private Button exitButton;

    private int m_progressAmount;
    
    
    //Unity Functions
    //================================================================================================================//
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        m_activeFileUIElements =  new List<FileUIElement>();
        
        SetWindowActive(false);
        
        CleanFiles();
        
        fileContentsText.text = string.Empty;
        fileContentsImage.sprite = null;
        fileContentsImage.gameObject.SetActive(false);
        exitButton.onClick.AddListener(() =>
        {
            m_onFinished?.Invoke();
            SetWindowActive(false);
        });
        
        IncrementProgress(0);
    }

    //Setup
    //================================================================================================================//
    private Action m_onFinished;
    public void MountDrive(InteractableDrive file, Action onFinished)
    {
        m_onFinished = onFinished;
        
        CleanFiles();
        GenerateFiles(file.fileDefinitions);

        SetWindowActive(true);

        fileExplorerHeaderText.text = $"Drive\n>{GetRandomLetter()}:/";
        
        return;
        
        char GetRandomLetter()
        {
            return (char)UnityEngine.Random.Range('A', 'Z' + 1);
        }
        
    }
    

    private void GenerateFiles(IEnumerable<FileDefinitionScriptableObject> files)
    {
        foreach (var file in files)
        {
            var instance = Instantiate(fileUIElementPrefab, fileContainerRectTransform);
            m_activeFileUIElements.Add(instance);
            
            instance.Init(file, OnFileSelected);
        }
    }

    private void CleanFiles()
    {   
        for (int i = m_activeFileUIElements.Count - 1; i >= 0; i--)
        {
            Destroy(m_activeFileUIElements[i].gameObject);
        }
        
        m_activeFileUIElements.Clear();
    }

    //Progress
    //================================================================================================================//

    //TODO Animate this
    private void IncrementProgress(int amount)
    {
        m_progressAmount += amount;
        
        progressBar.value = m_progressAmount;
        progressBarText.text = $"data recovery {amount:0}/100";
    }
    
    //================================================================================================================//

    public void SetWindowActive(bool active)
    {
        //TODO Add the screen fade
        window.SetActive(active);
    }
    
    //Callbacks
    //================================================================================================================//

    private void OnFileSelected(FileDefinitionScriptableObject fileUIElement)
    {
        fileContentsHeaderText.text = fileUIElement.FileName;
        fileContentsText.text = fileUIElement.FileContents;

        fileContentsImage.gameObject.SetActive(fileUIElement.Image == null);
        fileContentsImage.sprite = fileUIElement.Image;

        //TODO Need to finish
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].gameObject.SetActive(false);
        }
        
        if(fileUIElement.FileType == FILE_TYPE.ZIP && fileUIElement.ProgressTowardsObjective > 0)
        {
            buttons[0].onClick.AddListener(() =>
            {
                IncrementProgress(fileUIElement.ProgressTowardsObjective);
                buttons[0].interactable = false;
            });
            
            buttons[0].interactable = true;
            buttons[0].gameObject.SetActive(true);
        }

            
    }

    //================================================================================================================//

}
