using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

namespace Framework.Components
{
    public class KnockbackBombComponent : UObjectComponent
    {
        [SerializeField] private float knockbackForce;
        [SerializeField] private float radius;
        [SerializeField] private float proxyPositionSyncPauseDuration = 0.15f;
        [SerializeField] private bool scaleProxyPositionSyncPauseWithPing = true;
        [SerializeField] private float proxyPositionSyncPausePingScale = 0.5f;
        [SerializeField] private float minProxyPositionSyncPauseDuration = 0.08f;
        [SerializeField] private float maxProxyPositionSyncPauseDuration = 0.25f;
        
        [SerializeField] private GameObject explosionEffectPrefab;

        private ObjectPool<GameObject> _explosionEffectPool;
        private NetworkManager _networkManager;

        protected override void BeginPlay()
        {
            base.BeginPlay();
            
            _networkManager = NetworkManager.Singleton;
            
            _explosionEffectPool = new ObjectPool<GameObject>(() =>
            {
                GameObject obj = Instantiate(explosionEffectPrefab);
                obj.SetActive(false);
                return obj;
            }, obj =>
            {
                obj.SetActive(true);
            }, obj =>
            {
                obj.SetActive(false);
            }, obj =>
            {
                Destroy(obj);
            }, false, 10, 20);
            
            
        }

        public void DoKnockback()
        {
            TryDoAOEKnockbackServerRpc();

            if (!IsServer)
            {
                AOEKnockback(GetProxyPositionSyncPauseDuration());
            }
        }
        
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Server)]
        public void TryDoAOEKnockbackServerRpc()
        {
            AOEKnockback();
            
            TryDoAOEKnockbackClientRpc();
        }

        [ClientRpc]
        public void TryDoAOEKnockbackClientRpc()
        {
            if (!IsServer && !IsOwner)
            {
                AOEKnockback(GetProxyPositionSyncPauseDuration());
            }

            SpawnVFX();
        }

        private void SpawnVFX()
        {
            _explosionEffectPool.Get().transform.position = transform.position;
        }

        private void AOEKnockback(float proxySyncPauseDuration = 0f)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            Debug.Log(colliders.Length);
            
            foreach (Collider collider in colliders)
            {
                Rigidbody rb = collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = (collider.transform.position - transform.position).normalized;
                    rb.AddForce(direction * knockbackForce, ForceMode.Impulse);
                    continue;
                }
                
                CharacterMovementComponent movement = collider.GetComponent<CharacterMovementComponent>();
                if (movement != null)
                {
                    Vector3 direction = (collider.transform.position - transform.position).normalized;
                    movement.AddKnockback(direction * knockbackForce, proxySyncPauseDuration);
                }
            }
        }

        private float GetProxyPositionSyncPauseDuration()
        {
            float duration = proxyPositionSyncPauseDuration;

            if (scaleProxyPositionSyncPauseWithPing)
            {
                duration += GetServerRttSeconds() * proxyPositionSyncPausePingScale;
            }

            return Mathf.Clamp(
                duration,
                minProxyPositionSyncPauseDuration,
                maxProxyPositionSyncPauseDuration);
        }

        private float GetServerRttSeconds()
        {
            if (_networkManager == null)
            {
                _networkManager = NetworkManager.Singleton;
            }

            if (_networkManager == null
                || !_networkManager.IsListening
                || _networkManager.NetworkConfig?.NetworkTransport == null)
            {
                return 0f;
            }

            ulong rttMilliseconds = _networkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
            return rttMilliseconds / 1000f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
