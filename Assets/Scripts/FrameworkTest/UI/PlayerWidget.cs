using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerWidget : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bombArmTimeTMP;

    private void Start()
    {
        bombArmTimeTMP.enabled = false;
    }

    public void Initialize(ref NetworkVariable<float> bombTimer)
    {
        bombTimer.OnValueChanged += OnBombTimerChanged;
    }

    private void OnBombTimerChanged(float previousValue, float newValue)
    {
        bombArmTimeTMP.enabled = newValue > 0f;
        
        bombArmTimeTMP.text = newValue.ToString("0.0");
    }
}
