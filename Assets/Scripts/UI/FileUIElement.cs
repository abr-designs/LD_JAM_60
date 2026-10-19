using System;
using ldjam60.Files;
using ldjam60.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FileUIElement : MonoBehaviour
{
    [SerializeField] private TMP_Text fileNameText;
    [SerializeField] private TMP_Text fileTypeText;
    [SerializeField] private Button button;

    private static readonly Color SelectedBackground = new Color32(0xC0, 0xD1, 0xCC, 0xFF); //new(0.72f, 0.92f, 0.74f, 1f);
    private static readonly Color SelectedText = new Color32(0xC0, 0xD1, 0xCC, 0xFF); //new(0.06f, 0.12f, 0.07f, 1f);

    private FileDefinitionScriptableObject m_data;
    private Image m_background;
    private Color m_normalBackground;
    private Color m_normalNameColor;
    private Color m_normalTypeColor;
    private string m_status;

    public void Init(FileDefinitionScriptableObject file, Action<FileUIElement> onPressed, string status = null)
    {
        button.onClick.RemoveAllListeners();
        m_data = file;
        m_status = status;

        m_background = button.targetGraphic as Image;
        if (m_background != null)
            m_normalBackground = m_background.color;
        m_normalNameColor = fileNameText.color;
        m_normalTypeColor = fileTypeText.color;

        button.transition = Selectable.Transition.None;
        RefreshLabels(false);
        button.onClick.AddListener(() =>
        {
            onPressed?.Invoke(this);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        });
    }

    public void SetSelected(bool selected)
    {
        if (m_background != null)
            m_background.color = selected ? SelectedBackground : m_normalBackground;

        fileNameText.color = selected ? SelectedText : m_normalNameColor;
        fileTypeText.color = selected ? SelectedText : m_normalTypeColor;
        RefreshLabels(selected);
    }

    private void RefreshLabels(bool selected)
    {
        fileNameText.text = $"{(selected ? "> " : "  ")}{m_data.FileName}";
        var type = FileTypeToString(m_data.FileType);
        fileTypeText.text = string.IsNullOrEmpty(m_status) ? type : $"{type} [{m_status}]";
    }

    private static string FileTypeToString(FILE_TYPE fileType)
    {
        return fileType switch
        {
            FILE_TYPE.TEXT => "TXT",
            FILE_TYPE.IMAGE => "IMG",
            FILE_TYPE.ZIP => "ZIP",
            FILE_TYPE.EXE => "EXE",
            FILE_TYPE.NONE => "---",
            _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null)
        };
    }
}
