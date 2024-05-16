using TMPro;
using UnityEngine;

public class StatusWindow : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;
    [SerializeField] private TMP_Text _messageText;

    public static StatusWindow Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null)
            Instance = this;

        DontDestroyOnLoad(this);
    }
    
    public void Show(string message)
    {
        if (_canvas == null)
        {
            Debug.LogError($"Canvas not set on {nameof(StatusWindow)}.");
            return;
        }

        if(_messageText != null)
        {
            _messageText.SetText(message);
        }
            
        _canvas.enabled = true;
    }
    
    public void Hide()
    {
        if (_canvas == null)
        {
            Debug.LogError($"Canvas not set on {nameof(StatusWindow)}.");
            return;
        }

        _canvas.enabled = false;
    }

    public void OnCloseButtonClicked() => Hide();
}
