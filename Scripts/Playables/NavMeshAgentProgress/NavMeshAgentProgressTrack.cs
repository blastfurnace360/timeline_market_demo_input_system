using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[TrackClipType(typeof(NavMeshAgentProgressClip))]
[TrackBindingType(typeof(NavMeshAgent))]
[TrackColor(1.0f, 0.0f, 0.2f)]
public class NavMeshAgentProgressTrack : TrackAsset 
{ 
}