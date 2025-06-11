using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Sentis;
using Unity.Sentis.ONNX;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Data.Common;

[CustomEditor(typeof(ModelManager))]
public class TrainingPathSelector : Editor
{
    public override void OnInspectorGUI()
    {
        

        // Get the target object
        ModelManager trainingFileHolder = (ModelManager)target;

        // Add a button to open the folder selection dialog
        if (GUILayout.Button("Select Training File"))
        {
            string path = EditorUtility.OpenFilePanel("Select Training File", "", "json");

            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log("Loading Assets...");

                // Set the folder path in the target object
                trainingFileHolder._trainingPath = path;
                trainingFileHolder._modelAssets.Clear();

                // Loads the json into the new structs
                string jsonContent = File.ReadAllText(path);
                trainingFileHolder._trainingData = JsonUtility.FromJson<TrainingData>(jsonContent);

                List<FileInfo> nnList = new List<FileInfo>();
                foreach (var cp in trainingFileHolder._trainingData.DuckRabbit.checkpoints){
                    try{
                        // Create FileInfo object
                        FileInfo fileInfo = new FileInfo(cp.file_path);

                        // Attempt to access the file to ensure it can be loaded
                        if (fileInfo.Exists)
                        {
                            nnList.Add(fileInfo);
                        } else {
                            Debug.LogError($"File not found: {cp.file_path}");
                        }
                    } catch(Exception e){
                        Debug.LogError($"Error loading file {cp.file_path}: {e.Message}");
                    }
                    
                }

                if(nnList.Count < 1) {
                    return;
                }

                // Need to convert list to array -- could update this to accept lists only
                FileInfo[] sortedModelList = SortModelList(nnList.ToArray());

                LoadModelsFromDisk(sortedModelList, trainingFileHolder);

                // Set the number of files in the 
                trainingFileHolder._fileCount = nnList.Count;

                // Mark the target object as dirty to save changes
                EditorUtility.SetDirty(trainingFileHolder);
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