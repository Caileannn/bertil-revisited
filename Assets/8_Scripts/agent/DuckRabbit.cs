using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.SocialPlatforms.Impl;
using UnityEditor.Rendering;


public class DuckRabbit : Agent
{
    [Header("Current Step Count")]
    [SerializeField]
    private int _StepCount = 0;

    [Header("Walk Speed")]
    [Range(0.1f, 10f)]
    [SerializeField]

    private float _targetWalkingSpeed = 10;
    const float _maxWalkingSpeed = 10;

    public float TargetWalkingSpeed
    {
        get { return _targetWalkingSpeed; }
        set { _targetWalkingSpeed = Mathf.Clamp(value, .1f, _maxWalkingSpeed); }
    }

    [Header("Randomisation Settings")]
    public bool _randomiseWalkSpeed = true;
    public bool _randomiseYRotation = true;
    public bool _randomiseXYZRotation = false;

    [Header("Target to Walk Towards")]
    public Transform _target;
    OrientationCubeController _orienrationCubeController;
    JointDriveController _jointDriveController;


    [Header("Body Parts")]
    public Transform bpArmatureRoot;
    public Transform bpHead;
    public Transform bpLefFoot;
    public Transform bpRightFoot;

    [HideInInspector]
    private Vector3 _WorldDirectinToWalk;

    [Header("Climbing Stats")]
    [SerializeField] private int _StepCountAtLastPosition = 0;
    [SerializeField] private const int _MaxStepsWithoutProgress = 200;
    [SerializeField] float _previousDistanceFromTarget = 100;
    [SerializeField] float _InitialDistanceFromTarget = 0;
    [SerializeField] float _ClosestDistanceFromTarget = 0;

    public override void Initialize()
    {
        _orienrationCubeController = GetComponentInChildren<OrientationCubeController>();
        _jointDriveController = GetComponent<JointDriveController>();

        SetupBodyParts();
    }

    public override void OnEpisodeBegin()
    {
        foreach(var bp in _jointDriveController.bodyPartsDict.Values)
        {
            bp.Reset(bp);
        }

        UpdateOrientationObject();

        
        if(_randomiseYRotation){
            bpArmatureRoot.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        }
    
        if(_randomiseXYZRotation){
            bpArmatureRoot.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        }

        _targetWalkingSpeed = _randomiseWalkSpeed ? Random.Range(0.1f, _maxWalkingSpeed) : _targetWalkingSpeed;
    }

    public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
    {
        // add more sensors if needed 
        sensor.AddObservation(bp.groundContact.touchingGround);

        // positional information for the agent
        sensor.AddObservation(_orienrationCubeController.transform.InverseTransformDirection(bp.rb.linearVelocity));
        sensor.AddObservation(_orienrationCubeController.transform.InverseTransformDirection(bp.rb.angularVelocity));
        sensor.AddObservation(_orienrationCubeController.transform.InverseTransformDirection(bp.rb.position - bpArmatureRoot.position));

        // if the bp is not the seat, add more observations - get the rotation and normalised strength used
        if(bp.rb.transform != bpArmatureRoot){
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / _jointDriveController.maxJointForceLimit);
        }

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var cubeForward = _orienrationCubeController.transform.forward;

        var velocityGoal = cubeForward * _targetWalkingSpeed;

        var avgVelocity = GetAvgVelocity();

        sensor.AddObservation(Vector3.Distance(velocityGoal, avgVelocity));
        sensor.AddObservation(_orienrationCubeController.transform.InverseTransformDirection(avgVelocity));
        sensor.AddObservation(_orienrationCubeController.transform.InverseTransformDirection(velocityGoal));
        sensor.AddObservation(Quaternion.FromToRotation(bpArmatureRoot.forward, cubeForward));
        sensor.AddObservation(_orienrationCubeController.transform.InverseTransformDirection(_target.transform.position));
        sensor.AddObservation(_orienrationCubeController.transform.position.y);
        sensor.AddObservation(_orienrationCubeController.transform.position - _target.transform.position);

        foreach(var bp in _jointDriveController.bodyPartsList){
            CollectObservationBodyPart(bp, sensor);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var bpDict = _jointDriveController.bodyPartsDict;
        var element = -1;
        var continuousActions = actions.ContinuousActions;

        bpDict[bpHead].SetJointTargetRotation(continuousActions[++element], continuousActions[++element], continuousActions[++element]);
        bpDict[bpLefFoot].SetJointTargetRotation(continuousActions[++element], continuousActions[++element], continuousActions[++element]);
        bpDict[bpRightFoot].SetJointTargetRotation(continuousActions[++element], continuousActions[++element], continuousActions[++element]);

        bpDict[bpHead].SetJointStrength(continuousActions[++element]);
        bpDict[bpLefFoot].SetJointStrength(continuousActions[++element]);
        bpDict[bpRightFoot].SetJointStrength(continuousActions[++element]);
    }

    void FixedUpdate()
    {
        _StepCount = StepCount;

        UpdateOrientationObject();

        var cubeForward = _orienrationCubeController.transform.forward;

        var matchSpeedReward = GetMatchingVelocityReward(cubeForward * _targetWalkingSpeed, GetAvgVelocity());

        var lookAtTargetReward = (Vector3.Dot(cubeForward, bpArmatureRoot.forward) + 1) * 0.5f;

        AddReward(matchSpeedReward * lookAtTargetReward);
        //Debug.Log($"Match Speed Reward: {matchSpeedReward}, Look At Target Reward: {lookAtTargetReward}");
    }

    void Update()
    {
        
    }

    private void SetupBodyParts(){
        _jointDriveController.SetupBodyPart(bpArmatureRoot);
        _jointDriveController.SetupBodyPart(bpHead);
        _jointDriveController.SetupBodyPart(bpLefFoot);
        _jointDriveController.SetupBodyPart(bpRightFoot);
    }

    private void UpdateOrientationObject(){
        _orienrationCubeController.UpdateOrientation(bpArmatureRoot, _target);
    }

    Vector3 GetAvgVelocity()
    {
        Vector3 velSum = Vector3.zero;

        int rbCount = 0;
        foreach (var item in _jointDriveController.bodyPartsList)
        {
            rbCount++;
            velSum += item.rb.linearVelocity;
        }

        var avgVelocity = velSum / rbCount;
        return avgVelocity;
    }

    Vector3 GetAvgAngularVelocity()
    {
        Vector3 velSum = Vector3.zero;

        int rbCount = 0;
        foreach (var item in _jointDriveController.bodyPartsList)
        {
            rbCount++;
            velSum += item.rb.angularVelocity;
        }

        var avgVelocity = velSum / rbCount;
        return 
        
        avgVelocity;
    }

    float DeltaAngle(float angle)
    {
        var currentZRot = angle;
        float zRotDist = Mathf.Abs(Mathf.DeltaAngle(0f, currentZRot));
        float normalizedDistance = 1f - Mathf.InverseLerp(0f, 180f, zRotDist);
        float expZDist = Mathf.Pow(normalizedDistance, 2);
        return expZDist;
    }
    
    Vector2 GetAngleDeltaXZ()
    {
        return new Vector2(DeltaAngle(bpArmatureRoot.eulerAngles.x), DeltaAngle(bpArmatureRoot.eulerAngles.z));
    }

    public float GetMatchingVelocityReward(Vector3 velGoal, Vector3 currentVel)
    {
        var velDeltaMag = Mathf.Clamp(Vector3.Distance(currentVel, velGoal), 0, _targetWalkingSpeed);
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMag / _targetWalkingSpeed, 2), 2);
    }

    private float CheckRaycastHeight(){
        RaycastHit hit;

        if(Physics.Raycast(bpArmatureRoot.transform.position, transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity)){
            Debug.DrawRay(bpArmatureRoot.transform.position, transform.TransformDirection(Vector3.down) * hit.distance, Color.green);
            return hit.distance / 2;
        } else {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward), Color.blue);
            return 1;
        }
    }

}