using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Sentis;
using Unity.Sentis.ONNX;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;


[CustomEditor(typeof(ModelManager))]
public class FolderPathHolder : Editor
{
    public override void OnInspectorGUI()
    {
        

        // Get the target object
        ModelManager folderPathHolder = (ModelManager)target;

        // Add a button to open the folder selection dialog
        if (GUILayout.Button("Select Folder"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", "", "");

            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log("Loading Assets...");
                // Set the folder path in the target object
                folderPathHolder._directoryPath = path;
                folderPathHolder._modelAssets.Clear();

                DirectoryInfo dirInfo = new DirectoryInfo(path);
                FileInfo[] nnList = dirInfo.GetFiles("*.onnx");

                FileInfo[] sortedModelList = SortModelList(nnList);

                LoadModelsFromDisk(sortedModelList, folderPathHolder);

                // Set the number of files in the 
                folderPathHolder._fileCount = Directory.GetFiles(path, "*.onnx").Length;

                // Mark the target object as dirty to save changes
                EditorUtility.SetDirty(folderPathHolder);
            }
        }

        // Draw the default inspector
        DrawDefaultInspector();
    }

    private void LoadModelsFromDisk(FileInfo[] nnList, ModelManager folderPathHolder){

        foreach (FileInfo file in nnList)
    {
        byte[] modelData = File.ReadAllBytes(file.FullName.ToString());

        // Convert ONNX model
        ONNXModelConverter converter = new ONNXModelConverter(file.FullName.ToString());
        Model sentisModel = converter.Convert();
        
        // Create model asset data instance
        ModelAssetData modelAssetData = ScriptableObject.CreateInstance<ModelAssetData>();
        
        // Create a list to store weight chunks
        List<ModelAssetWeightsData> weightChunks = new List<ModelAssetWeightsData>();

        using (var memoryStream = new MemoryStream())
        {
            // Save model description
            byte[] modelDescriptionBytes = ModelWriter.SaveModelDescription(sentisModel);
            memoryStream.Write(modelDescriptionBytes);
            modelAssetData.value = memoryStream.ToArray();
        }

        // Handle weights separately
        var modelWeightsChunksBytes = ModelWriter.SaveModelWeights(sentisModel);
        foreach (var chunkBytes in modelWeightsChunksBytes)
        {
            var weightChunk = ScriptableObject.CreateInstance<ModelAssetWeightsData>();
            weightChunk.value = chunkBytes;
            weightChunks.Add(weightChunk);
        }

        modelAssetData.name = "Data";
        modelAssetData.hideFlags = HideFlags.HideInHierarchy;

        // Create and setup the ModelAsset
        ModelAsset modelAsset = ScriptableObject.CreateInstance<ModelAsset>();
        modelAsset.modelAssetData = modelAssetData;
        modelAsset.modelWeightsChunks = weightChunks.ToArray(); // Set the weights chunks
        modelAsset.name = file.Name;

        folderPathHolder._modelAssets.Add(modelAsset);
    }
    }

    private FileInfo[] SortModelList(FileInfo[] list){
        return list.OrderBy(file => ExtractNumberFromFileName(file.Name)).ToArray();
    }

    private int ExtractNumberFromFileName(string fileName)
    {
        // Use Regex to extract the number in the filename
        var match = Regex.Match(fileName, @"-(\d+)\.onnx");
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }
        else
        {
            throw new InvalidOperationException("Filename does not contain a valid number.");
        }
    }
}