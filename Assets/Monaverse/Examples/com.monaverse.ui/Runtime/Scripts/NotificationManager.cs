using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NotificationManager : MonoBehaviour
{
    public enum Severity
    {
        Message = 0,
        Warning = 1,
        Error = 2
    }
    
    private readonly Color32[] _titleBarColors = new Color32[]
    {
        new Color32(96, 96, 96, 255),
        new Color32(210, 170, 0, 255),
        new Color32(210, 0, 0, 255),
    };
    
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Image _titleBarBackground;
    [SerializeField] private TMP_Text _titleText; 
    [SerializeField] private TMP_Text _messageText;
     
    public static NotificationManager Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }

    public void ShowNotification(string title, string message, Severity severity = Severity.Message)
    {
        if (_canvas == null)
        {
            Debug.LogError($"Canvas not set on {nameof(NotificationManager)}.");
            return;
        }

        if (_titleBarBackground != null)
        {
            var color = _titleBarColors[(int)Severity.Message];
            
            if (severity >= Severity.Message && severity <= Severity.Error)
                color = _titleBarColors[(int)severity];

            _titleBarBackground.material.color = color;
        }

        if (_titleText != null)
            _titleText.SetText(title);

        if (_messageText != null)
            _messageText.SetText(message);
            
        _canvas.enabled = true;
    }
    
    public void HideNotification()
    {
        if (_canvas == null)
        {
            Debug.LogError($"Canvas not set on {nameof(NotificationManager)}.");
            return;
        }

        _canvas.enabled = false;
    }

    public void OnCloseButtonClicked() => HideNotification();
}
