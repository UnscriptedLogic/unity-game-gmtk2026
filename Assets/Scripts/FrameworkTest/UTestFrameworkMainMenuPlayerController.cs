using Framework;
using UnityEngine;

public class UTestFrameworkMainMenuPlayerController : UController
{
    [SerializeField] private MainMenuWidget mainMenuWidgetPrefab;
    
    private MainMenuWidget mainMenuWidget;

    protected override void BeginPlay()
    {
        base.BeginPlay();
        
        mainMenuWidget = Instantiate(mainMenuWidgetPrefab);
        mainMenuWidget.OnClientButtonClickedEvent += OnClientButtonClicked;
    }

    private void OnClientButtonClicked()
    {
        GameMode.NetworkManager.StartClient();
    }
}
