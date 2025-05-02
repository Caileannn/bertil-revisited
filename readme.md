## Installation

To install mlagents, you will need to clone the mlagents repo from Unity -- instructions for this can be found here https://github.com/Unity-Technologies/ml-agents/blob/develop/docs/Installation.md


## Commands for Training

mlagents-learn config/ppo/walker-config.yaml --run-id getup-1 --env ../getup-build/bertil-training.exe --num-envs 8

mlagents-learn unity-project/config/ppo/walker-config.yaml --run-id climber-test-1 --results-dir training-results --force --initialize-from walker