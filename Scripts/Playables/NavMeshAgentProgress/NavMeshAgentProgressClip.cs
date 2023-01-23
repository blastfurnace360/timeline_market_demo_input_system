using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class NavMeshAgentProgressClip : PlayableAsset, ITimelineClipAsset
{
    [HideInInspector]
    public bool dynamicPathUpdate;
	public ExposedReference<Transform> startTransform;
	public ExposedReference<Transform> targetTransform;
    [HideInInspector]
	public int trackId;

    [HideInInspector]
	public NavMeshAgentProgressPlayableBehaviour playableBehaviour;
    //[HideInInspector]
    // public TimelineClip m_clip;
	public AnimationCurve progressCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    // public NavMeshAgent boundAgent;
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
	    trackId = GetInstanceID();
	    playableBehaviour = new NavMeshAgentProgressPlayableBehaviour();
	    playableBehaviour.trackId = trackId;
	    playableBehaviour.updatePathPerFrame = dynamicPathUpdate;
	    playableBehaviour.startTransform = startTransform.Resolve(graph.GetResolver());
	    playableBehaviour.targetTransform = targetTransform.Resolve(graph.GetResolver());
	    playableBehaviour.progressCurve = progressCurve;
	    return ScriptPlayable<NavMeshAgentProgressPlayableBehaviour>.Create(graph, playableBehaviour);
    }
}