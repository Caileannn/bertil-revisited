using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using Unity.MLAgentsExamples;
using System.Numerics;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;


public class Chair : Agent
{
    [Header("Walk Speed")]
    [Range(0.1f, 30f)]
    [SerializeField]

    private float _targetWalkingSpeed = 30;
    const float _maxWalkingSpeed = 30;

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
    public Transform bpSeat;
    public Transform bpBack;
    public Transform bpFrontLeftLeg;
    public Transform bpFrontRightLeg;
    public Transform bpBackLeftLeg;
    public Transform bpBackRightLeg;
    public Transform bpFrontLeftUpper; 
    public Transform bpFrontRightUpper;
    public Transform bpBackLeftUpper;
    public Transform bpBackRightUpper;
    public Transform bpFrontLeftFoot;
    public Transform bpFrontRightFoot;
    public Transform bpBackLeftFoot;
    public Transform bpBackRightFoot;

    [HideInInspector]
    private Vector3 _WorldDirectinToWalk;

    public override void Initialize()
    {
        _orienrationCubeController = GetComponentInChildren<OrientationCubeController>();
        _jointDriveController = GetComponent<JointDriveController>();

        SetuoBodyParts();
    }

    public override void OnEpisodeBegin()
    {
        foreach(var bp in _jointDriveController.bodyPartsDict.Values)
        {
            bp.Reset(bp);
        }

        UpdateOrientationObject();

        if(_randomiseYRotation){
            bpSeat.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
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
        sensor.AddObservation(_orienrationCubeController.transform.InverseTransformDirection(bp.rb.position - bpSeat.position));

        // if the bp is not the seat, add more observations - get the rotation and normalised strength used
        if(bp.rb.transform != bpSeat){
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / _jointDriveController.maxJointForceLimit);
        }

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var cubeForward = _orienrationCubeController.transform.forward;

        var velocityGoal = cubeForward * _targetWalkingSpeed;

        var avgVelocity = GetAvgVelocity();

        sensor.AddObservation(UnityEngine.Vector3.Distance(velocityGoal, avgVelocity));

        sensor.AddObservation(_orienrationCubeController.transform.InverseTransformDirection(avgVelocity));

        sensor.AddObservation(_orienrationCubeController.transform.InverseTransformDirection(velocityGoal));

        sensor.AddObservation(UnityEngine.Quaternion.FromToRotation(bpSeat.forward, cubeForward));

        sensor.AddObservation(_orienrationCubeController.transform.InverseTransformDirection(_target.transform.position));

        foreach(var bp in _jointDriveController.bodyPartsList){
            CollectObservationBodyPart(bp, sensor);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var bpDict = _jointDriveController.bodyPartsDict;
        var element = -1;
        var continuousActions = actions.ContinuousActions;

        bpDict[bpBack].SetJointTargetRotation(continuousActions[++element], 0, 0);
        
        bpDict[bpFrontLeftLeg].SetJointTargetRotation(continuousActions[++element], continuousActions[++element], 0);
        bpDict[bpFrontRightLeg].SetJointTargetRotation(continuousActions[++element], continuousActions[++element], 0);
        bpDict[bpBackLeftLeg].SetJointTargetRotation(continuousActions[++element], continuousActions[++element], 0);
        bpDict[bpBackRightLeg].SetJointTargetRotation(continuousActions[++element], continuousActions[++element], 0);

        bpDict[bpFrontLeftUpper].SetJointTargetRotation(continuousActions[++element], continuousActions[++element], 0);
        bpDict[bpFrontRightUpper].SetJointTargetRotation(continuousActions[++element], continuousActions[++element], 0);
        bpDict[bpBackLeftUpper].SetJointTargetRotation(continuousActions[++element], continuousActions[++element], 0);
        bpDict[bpBackRightUpper].SetJointTargetRotation(continuousActions[++element], continuousActions[++element], 0);

        bpDict[bpFrontLeftFoot].SetJointTargetRotation(continuousActions[++element], 0, 0);
        bpDict[bpFrontRightFoot].SetJointTargetRotation(continuousActions[++element], 0, 0);
        bpDict[bpBackLeftFoot].SetJointTargetRotation(continuousActions[++element], 0, 0);
        bpDict[bpBackRightFoot].SetJointTargetRotation(continuousActions[++element], 0, 0);

        bpDict[bpBack].SetJointStrength(continuousActions[++element]);
        bpDict[bpFrontLeftLeg].SetJointStrength(continuousActions[++element]);
        bpDict[bpFrontRightLeg].SetJointStrength(continuousActions[++element]);
        bpDict[bpBackLeftLeg].SetJointStrength(continuousActions[++element]);
        bpDict[bpBackRightLeg].SetJointStrength(continuousActions[++element]);
        bpDict[bpFrontLeftUpper].SetJointStrength(continuousActions[++element]);
        bpDict[bpFrontRightUpper].SetJointStrength(continuousActions[++element]);
        bpDict[bpBackLeftUpper].SetJointStrength(continuousActions[++element]);
        bpDict[bpBackRightUpper].SetJointStrength(continuousActions[++element]);
        bpDict[bpFrontLeftFoot].SetJointStrength(continuousActions[++element]);
        bpDict[bpFrontRightFoot].SetJointStrength(continuousActions[++element]);
        bpDict[bpBackLeftFoot].SetJointStrength(continuousActions[++element]);
        bpDict[bpBackRightFoot].SetJointStrength(continuousActions[++element]);
    }

    void FixedUpdate()
    {
        UpdateOrientationObject();

        var cubeForward = _orienrationCubeController.transform.forward;

        var matchSpeedReward = GetMatchingVelocityReward(cubeForward * _targetWalkingSpeed, GetAvgVelocity());

        var lookAtTargetReward = (UnityEngine.Vector3.Dot(cubeForward, bpSeat.forward) + 1) * 0.5f;

        AddReward(matchSpeedReward * lookAtTargetReward);
    }

    void Update()
    {
        
    }

    private void SetuoBodyParts(){
        _jointDriveController.SetupBodyPart(bpSeat);
        _jointDriveController.SetupBodyPart(bpBack);
        _jointDriveController.SetupBodyPart(bpFrontLeftLeg);
        _jointDriveController.SetupBodyPart(bpFrontRightLeg);
        _jointDriveController.SetupBodyPart(bpBackLeftLeg);
        _jointDriveController.SetupBodyPart(bpBackRightLeg);
        _jointDriveController.SetupBodyPart(bpFrontLeftUpper);
        _jointDriveController.SetupBodyPart(bpFrontRightUpper);
        _jointDriveController.SetupBodyPart(bpBackLeftUpper);
        _jointDriveController.SetupBodyPart(bpBackRightUpper);
        _jointDriveController.SetupBodyPart(bpFrontLeftFoot);
        _jointDriveController.SetupBodyPart(bpFrontRightFoot);
        _jointDriveController.SetupBodyPart(bpBackLeftFoot);
        _jointDriveController.SetupBodyPart(bpBackRightFoot);
    }

    private void UpdateOrientationObject(){
        _orienrationCubeController.UpdateOrientation(bpSeat, _target);
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

    public float GetMatchingVelocityReward(Vector3 velGoal, Vector3 currentVel)
    {
        var velDeltaMag = Mathf.Clamp(Vector3.Distance(currentVel, velGoal), 0, _targetWalkingSpeed);
        return Mathf.Pow(1 - Mathf.Pow(velDeltaMag / _targetWalkingSpeed, 2), 2);
    }

}