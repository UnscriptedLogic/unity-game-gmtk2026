using System;
using System.Collections;
using System.Threading.Tasks;
using Framework;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace FrameworkTest
{
    public class UTestFrameworkGameMode : UGameMode
    {
        int counter = 0;
        
        protected override void Awake()
        {
            Debug.Log("UTestFrameworkGameMode Initializing....");
            
            base.Awake();
        }

        protected override async void Start()
        {
            base.Start();
            
#if DEDICATED_SERVER
            Debug.Log("Starting server...");
            
            UnityTransport transport = NetworkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData("0.0.0.0", 7777, "0.0.0.0");
            
            NetworkManager.OnServerStarted += () =>
            {
                Debug.Log("Dedicated Linux Server started!");
                Debug.Log($"IP: {transport.ConnectionData.Address}");
                Debug.Log($"Port: {transport.ConnectionData.Port}");
            };
            
            NetworkManager.OnClientConnectedCallback += (clientId) =>
            {
                Debug.Log($"Player{clientId} has joined the game");
            };
            
            NetworkManager.StartServer();
#endif

#if CLIENT
            await Task.Delay(5000);
            
            UnityTransport transport = NetworkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData("13.229.74.29", 7777, "0.0.0.0");
            
            NetworkManager.StartClient();      
#endif
        }
    }
}