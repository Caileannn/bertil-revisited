using UnityEngine;
using Unity.MLAgents;

namespace Unity.MLAgentsExamples
{
    /// <summary>
    /// This class contains logic for locomotion agents with joints which might make contact with the ground.
    /// By attaching this as a component to those joints, their contact with the ground can be used as either
    /// an observation for that agent, and/or a means of punishing the agent for making undesirable contact.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundContact : MonoBehaviour
    {
        [HideInInspector] public Agent agent;

        [Header("Ground Check")] public bool agentDoneOnGroundContact; // Whether to reset agent on ground contact.
        public bool penalizeGroundContact; // Whether to penalize on contact.
        public bool penalizeStairsContact; // Whether to penalize on contact.
        public float groundContactPenalty; // Penalty amount (ex: -1).
        public float stairsContactPenalty; // Penalty amount (ex: -1).
        public bool touchingGround;
        const string k_Ground = "Ground"; // Tag of ground object.
        public bool touchingStairs;
        const string k_Stairs = "Stairs"; // Tag of ground object.
        public bool touchingObstacle;
        const string k_Obstacle = "Obstacle";

        /// <summary>
        /// Check for collision with ground, and optionally penalize agent.
        /// </summary>
        void OnCollisionEnter(Collision col)
        {
            if (col.transform.CompareTag(k_Ground))
            {
                touchingGround = true;
                if (penalizeGroundContact)
                {
                    agent.AddReward(groundContactPenalty);
                }

                if (agentDoneOnGroundContact)
                {
                    agent.EndEpisode();
                }
            }

            if (col.transform.CompareTag(k_Stairs))
            {
                touchingStairs = true;
                if (penalizeStairsContact)
                {
                    agent.AddReward(stairsContactPenalty);
                }
            }

            if (col.transform.CompareTag(k_Obstacle))
            {
                touchingObstacle = true;

                agent.EndEpisode();
            }
        }

        /// <summary>
        /// Check for end of ground collision and reset flag appropriately.
        /// </summary>
        void OnCollisionExit(Collision other)
        {
            if (other.transform.CompareTag(k_Ground))
            {
                touchingGround = false;
            }

            if (other.transform.CompareTag(k_Stairs))
            {
                touchingStairs = false;
            }

            if (other.transform.CompareTag(k_Stairs))
            {
                touchingObstacle = false;
            }
        }
    }
}
