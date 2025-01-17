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

    public Chair _chair;

    public void Start(){
        Debug.Log($"Number of Models : {_modelAssets.Count}");
        _chair.SetModel("Bertil", _modelAssets[0], InferenceDevice.Burst);
        Debug.Log($"Set Model {_modelAssets[0].name}");
    }

    

}
