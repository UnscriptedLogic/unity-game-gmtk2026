using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuWidget : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    public event Action OnHostButtonClickedEvent;
    public event Action OnClientButtonClickedEvent;
    
    private void Start()
    {
        hostButton.onClick.AddListener(OnHostButtonClicked);
        clientButton.onClick.AddListener(OnClientButtonClicked);
    }

    private void OnClientButtonClicked()
    {
        OnClientButtonClickedEvent?.Invoke();
    }

    private void OnHostButtonClicked()
    {
        OnHostButtonClickedEvent?.Invoke();
    }
}
