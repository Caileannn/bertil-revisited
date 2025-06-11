using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Radishmouse;
using UnityEngine.EventSystems;
using JetBrains.Annotations;

public class GraphPlotter : MonoBehaviour
{
    public RectTransform graphContainer;
    public GameObject pointPrefab;
    public ModelManager _modelManager;
    private List<Checkpoint> checkpoints;
    private List<GameObject> points = new List<GameObject>();
    private float minReward;
    private float maxReward;
    private float minSteps;
    private float maxSteps;
    public UILineRenderer lineRenderer;

    private void Start()
    {   
        // Calculate min and max rewards
        checkpoints = _modelManager._trainingData.DuckRabbit.checkpoints;

        minReward = checkpoints.Min(cp => cp.reward);
        maxReward = checkpoints.Max(cp => cp.reward);

        minSteps = checkpoints.Min(cp => cp.steps);
        maxSteps = checkpoints.Max(cp => cp.steps);

        List<Vector2> pointPositions = new List<Vector2>();

        // Plot the points on the graph
        for (int i = 0; i < checkpoints.Count; i++){
            Vector2 pointPosition = CreatePoint(checkpoints[i], i);
            pointPositions.Add(pointPosition);
        }

        // Update LineRenderer with the positions
        lineRenderer.points = pointPositions.ToArray();
    }

    private Vector2 CreatePoint(Checkpoint checkpoint, int idx)
    {
        float normalizedReward = (checkpoint.reward - minReward) / (maxReward - minReward);
        float normalisedSteps = (checkpoint.steps - minSteps) / (maxSteps - minSteps);

        float xPos = normalizedReward * (graphContainer.rect.width - 300) + 150;
        float yPos = normalisedSteps * (graphContainer.rect.height - 300) + 150; // Example y-position, adjust as needed

        GameObject point = Instantiate(pointPrefab, graphContainer);
        point.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, yPos);

        points.Add(point);


        // Add EventTrigger to handle click events
        EventTrigger trigger = point.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { OnClickEvent(idx); });
        trigger.triggers.Add(entry);

        return new Vector2(xPos, yPos);
        
    }

    private void OnClickEvent(int idx){
        _modelManager.UpdateModel(idx);
        
        for(int i = 0; i < points.Count; i++){
            Image img = points[i].GetComponent<Image>();
            if(i == idx){
                 img.color = Color.red;
            } else {
                img.color = Color.white;
            }
        }
    }
}