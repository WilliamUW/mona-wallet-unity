using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UnauthorizedPopup : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;

    public static UnauthorizedPopup Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }
    
    public void Show()
    {
        if (_canvas == null)
        {
            Debug.LogError($"Canvas not set on {nameof(UnauthorizedPopup)}.");
            return;
        }
            
        _canvas.enabled = true;
    }
    
    public void Hide()
    {
        if (_canvas == null)
        {
            Debug.LogError($"Canvas not set on {nameof(UnauthorizedPopup)}.");
            return;
        }

        _canvas.enabled = false;
    }

    public void OnRegisterButtonClicked()
    {
        Application.OpenURL("https://monaverse.com");
        Hide();
    }

    public void OnCloseButtonClicked() => Hide();
}
