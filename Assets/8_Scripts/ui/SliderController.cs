using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class SliderController : MonoBehaviour
{
    private Slider m_Slider;

    public delegate void OnSliderMovedDelegate(float value);

    public static event OnSliderMovedDelegate OnSliderMoved;
    
    private void OnEnable(){

        try {
            m_Slider = this.GetComponent<Slider>();
            m_Slider.onValueChanged.AddListener(HandleSliderValueChange);
        } catch (Exception ex) {
            Debug.LogException(ex, this);
        }
    }

    private void HandleSliderValueChange(float value){
        OnSliderMoved?.Invoke(value);
    }
    
}