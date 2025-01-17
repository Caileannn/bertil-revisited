using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;
using Unity.Sentis;

public class TextController : MonoBehaviour
{
    private TextMeshProUGUI m_Text;
    
    private void OnEnable(){

        ModelManager.OnModelChanged += UpdateText;

        try {
            m_Text = this.GetComponent<TextMeshProUGUI>();
        } catch (Exception ex) {
            Debug.LogException(ex, this);
        }
    }

    private void OnDisable(){
        ModelManager.OnModelChanged -= UpdateText;
    }

    private void UpdateText(ModelAsset modelAsset){
        m_Text.text = modelAsset.name;
    }

    
}