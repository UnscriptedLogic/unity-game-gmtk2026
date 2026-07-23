using System.Threading.Tasks;
using Framework;
using UnityEngine;

namespace FrameworkTest
{
    public class UTestFrameworkGameMode : UGameMode
    {
        protected override async void Start()
        {
            base.Start();
            
            await Task.Delay(5000);
            
            NetworkManager.StartClient();
        }
    }
}