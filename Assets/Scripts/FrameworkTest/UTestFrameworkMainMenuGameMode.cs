using Framework;
using Unity.Netcode;

namespace FrameworkTest
{
    public class UTestFrameworkMainMenuGameMode : UGameMode
    {
        protected override void Start()
        {
            base.Start();
        
            NetworkManager.Singleton.StartHost();
        }
    }
}
