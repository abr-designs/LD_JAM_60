using System;
using ldjam60.Files;
using ldjam60.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FileUIElement : MonoBehaviour
{
    [SerializeField]
    private TMP_Text fileNameText;
    [SerializeField]
    private TMP_Text fileTypeText;

    [SerializeField]
    private Button button;

    private FileDefinitionScriptableObject m_data;

    public void Init(FileDefinitionScriptableObject file, Action<FileUIElement, FileDefinitionScriptableObject> onPressed)
    {
        button.onClick.RemoveAllListeners();
        m_data = file;
        
        fileNameText.text = file.name; //FIXME This needs to point to the field
        fileTypeText.text = FiletypeToString(FILE_TYPE.TEXT); //FIXME This needs to point to the field
        
        button.onClick.AddListener(() =>
        {
            onPressed?.Invoke(this, file);
        });
    }

    private static string FiletypeToString(FILE_TYPE  fileType)
    {
        switch (fileType)
        {
            case FILE_TYPE.TEXT:
                return "TXT";
            case FILE_TYPE.ZIP:
                return "ZIP";
            case FILE_TYPE.EXE:
                return "EXE";
            default:
                throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
        }
    }

}
