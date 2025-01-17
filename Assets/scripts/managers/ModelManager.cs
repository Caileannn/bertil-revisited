using System;
using System.Collections.Generic;
using System.IO;
using Unity.MLAgents.Policies;
using Unity.Sentis;
using Unity.Sentis.ONNX;
using UnityEngine;

public class ModelManager : MonoBehaviour
{
    [SerializeField]
    public string _directoryPath;

    [SerializeField]
    public List<ModelAsset> _modelAssets = new();
    
    [HideInInspector]
    public int _fileCount;

    private int m_CurrentModelIdx = 0;

    public Chair _chair;

    public delegate void OnModelChangedDelegate(ModelAsset modelAsset);

    public static event OnModelChangedDelegate OnModelChanged;

    private void OnEnable(){
        SliderController.OnSliderMoved += HandleSliderChange;
    }

    private void OnDisable(){
        SliderController.OnSliderMoved -= HandleSliderChange;
    }

    public void Start(){

        Debug.Log($"Number of Models : {_modelAssets.Count}");
        _chair.SetModel("Bertil", _modelAssets[m_CurrentModelIdx], InferenceDevice.Burst);
        Debug.Log($"Set Model {_modelAssets[m_CurrentModelIdx].name}");

    }

    public void HandleSliderChange(float sliderValue){

        int modelIndex = Mathf.RoundToInt(sliderValue * (_fileCount - 1)) + 1;
        int newModelIdx = Mathf.Clamp(modelIndex, 1, _fileCount); 

        if(newModelIdx != m_CurrentModelIdx){
            m_CurrentModelIdx = newModelIdx;
            HandleModelChange();
        }
        
    }

    public void HandleModelChange(){
        _chair.SetModel("Bertil", _modelAssets[m_CurrentModelIdx], InferenceDevice.Burst);
        Debug.Log($"Mapped file index: {m_CurrentModelIdx}");
        OnModelChanged?.Invoke(_modelAssets[m_CurrentModelIdx]);
    }

    

}
