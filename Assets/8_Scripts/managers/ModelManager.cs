using System;
using System.Collections.Generic;
using System.IO;
using Unity.MLAgents.Policies;
using Unity.Sentis;
using Unity.Sentis.ONNX;
using UnityEngine;
using UnityEngine.UI;

public class ModelManager : MonoBehaviour
{
    [SerializeField]
    public string _trainingPath;

    [SerializeField]
    public List<ModelAsset> _modelAssets = new();
    
    [HideInInspector]
    public int _fileCount;

    private int m_CurrentModelIdx = 0;

    public TrainingData _trainingData;

    public Chair _chair;

    public delegate void OnModelChangedDelegate(ModelAsset modelAsset);

    public static event OnModelChangedDelegate OnModelChanged;

    public Button _button;

    private void OnEnable(){
        SliderController.OnSliderMoved += HandleSliderChange;
        _button.onClick.AddListener(ResetAgent);
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

        int modelIndex = Mathf.RoundToInt(sliderValue * (_fileCount - 1));
        int newModelIdx = Mathf.Clamp(modelIndex, 0, _fileCount - 1); 

        if(newModelIdx != m_CurrentModelIdx){
            m_CurrentModelIdx = newModelIdx;
            HandleModelChange();
        }
        
    }

    public void HandleModelChange(){
        _chair.SetModel("Bertil", _modelAssets[m_CurrentModelIdx], InferenceDevice.Burst);
        OnModelChanged?.Invoke(_modelAssets[m_CurrentModelIdx]);
    }

    public void UpdateModel(int idx){
        if(idx != m_CurrentModelIdx){
            m_CurrentModelIdx = idx;
            HandleModelChange();
        }
    }

    public void ResetAgent(){
        _chair.EndEpisode();
    }

    

}

// Define classes to match the JSON structure
[System.Serializable]
public class TrainingData
{
    public TrainingRun Bertil;
}

[System.Serializable]
public class TrainingRun
{
    public List<Checkpoint> checkpoints;
    public Checkpoint final_checkpoint;
}

[System.Serializable]
public class Checkpoint
{
    public int steps;
    public string file_path;
    public float reward;
    public double creation_time;
    public List<string> auxillary_file_paths;
}
