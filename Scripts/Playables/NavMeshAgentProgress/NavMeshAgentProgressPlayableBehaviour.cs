using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.AI;
using UnityEngine.Timeline;
using System;
using System.Linq;


// weird workaround required - without OnAnimatorMove callback timeline NavAgent playback is overriden by root motion animation
// related to https://forum.unity.com/threads/how-to-properly-use-onanimatormove-without-breaking-timeline.600199/
public class AnimatorEmptyCallback : MonoBehaviour
{
	public void OnAnimatorMove() {}
}


[System.Serializable]
public class NavMeshAgentProgressPlayableBehaviour : PlayableBehaviour
{
	[HideInInspector]
	public TimelineClip clip;
	public NavMeshAgent agent;
	public Transform startTransform;
	public Transform targetTransform;
	public int trackId;
	public bool updatePathPerFrame = false;
	public AnimationCurve progressCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
	
	private GameObject agentGameObject;
	private float updateRate = 0.01f;
	private double timeOffset = 0;
	private bool forwardInTime = true;
	private float rotationSpeed = 12.0f;
	private float agentPathProgress;
	private double lastTime;
	private float oldDistance = 0.0f;
	private float distanceTotal = 0.0f;
	private float elapsed = 0.0f;	
	private NavMeshPath pathForward;
	private NavMeshPath pathPast;
	private Vector3[] cachePathForward;
	private Vector3[] cachePathPast;
	private Vector3[] lastPathForwardArray;
	private Vector3[] lastPathPastArray;
	
	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		//Debug.Log("OnBehaviorPlay called");
		LoadClipData(playable, info);
		InitialSetUp(playable, info);
	}
	
	public void OnBehaviourPause(Playable playable, FrameData info)
	{
		ClearCache();
	}
	
	public override void OnGraphStop(Playable playable)
	{
		// weird workaround required - without OnAnimatorMove callback timeline NavAgent playback is overriden by root motion animation
		// related to https://forum.unity.com/threads/how-to-properly-use-onanimatormove-without-breaking-timeline.600199/	
		//Debug.Log("OnGraphStop called");
		if (agentGameObject != null)
			if (agentGameObject.TryGetComponent<AnimatorEmptyCallback>(out AnimatorEmptyCallback animatorEmptyCallback)) GameObject.DestroyImmediate(animatorEmptyCallback);
	}
	
	private void LoadClipData(Playable playable, FrameData info)
	{
		var playableDirector = (playable.GetGraph().GetResolver() as PlayableDirector);
		var timelineAsset = playableDirector.playableAsset as TimelineAsset;
		
		foreach (var track in timelineAsset.GetOutputTracks())
		{
			var navMeshAgentProgressTrack = track as NavMeshAgentProgressTrack;
			if (navMeshAgentProgressTrack != null)
			{
				foreach (TimelineClip timelineClip in navMeshAgentProgressTrack.GetClips())
				{
					var asset = timelineClip.asset as NavMeshAgentProgressClip;
				
					if (asset)
						if (asset.trackId == trackId)
						{
							UnityEngine.Object binding = playableDirector.GetGenericBinding(track);
							clip = timelineClip;
							agent = binding as NavMeshAgent;
						}
				}
			}
		}
	}
	
	private void InitialSetUp(Playable playable, FrameData info)
	{
		if (agent == null || startTransform == null || targetTransform == null) return;
		
		Vector3[] pathForwardArray = UpdatePathForward(agent.transform.position, targetTransform.position, forwardInTime, lastPathForwardArray, false);
		Vector3[] pathPastArray = UpdatePathPast(agent.transform.position, startTransform.position, forwardInTime, lastPathPastArray, false);
		float distanceForward = PathDistance(pathForwardArray);
		float distancePast = PathDistance(pathPastArray);
		
		oldDistance = distanceTotal = distanceForward + distancePast;
		
		timeOffset = 0;
		agentGameObject = agent.gameObject;
		
		// weird workaround required - without OnAnimatorMove callback timeline NavAgent playback is overriden by root motion animation
		// related to https://forum.unity.com/threads/how-to-properly-use-onanimatormove-without-breaking-timeline.600199/
		if (agentGameObject.GetComponent<AnimatorEmptyCallback>() == null) agentGameObject.AddComponent<AnimatorEmptyCallback>();
	}
	
	private void ClearVector3Array(Vector3[] vector3Array)
	{
		if (vector3Array != null) Array.Clear(vector3Array, 0, vector3Array.Length);
	}
	
	private void ClearCache()
	{
		elapsed = 0.0f;
		pathForward = null;
		pathPast = null;
		timeOffset = 0;
		forwardInTime = true;
		rotationSpeed = 12;
		agentPathProgress = 0;
		lastTime = 0;
		oldDistance = 0;
		distanceTotal = 0;
		ClearVector3Array(cachePathForward);
		ClearVector3Array(cachePathPast);
		ClearVector3Array(lastPathForwardArray);
		ClearVector3Array(lastPathPastArray);	
	}
	
	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		agent = playerData as NavMeshAgent;
	}
	
	public override void PrepareFrame(Playable playable, FrameData info)
	{
		if (!Mathf.Approximately(distanceTotal, oldDistance)) timeOffset = playable.GetTime();
			
		double clipDuration = playable.GetDuration();
		float clipProgress = (float)(playable.GetTime() - timeOffset) / (float)(clipDuration - timeOffset);
		
		forwardInTime = lastTime < playable.GetTime() ? true : false;
		
		// evaluate the curve using the normalised time of the clip
		agentPathProgress = progressCurve.Evaluate(clipProgress);
		
		lastTime = playable.GetTime();
		if (agent == null || targetTransform == null) return;
		
		elapsed += Time.deltaTime;
		bool recalculatePath = elapsed > updateRate;
		if (recalculatePath) elapsed = 0;
		
		Vector3[] pathForwardArray = UpdatePathForward(agent.transform.position, targetTransform.position, forwardInTime, lastPathForwardArray, recalculatePath);
		lastPathForwardArray = pathForwardArray;
		Vector3[] pathPastArray = UpdatePathPast(agent.transform.position, startTransform.position, forwardInTime, lastPathPastArray, recalculatePath);
		lastPathPastArray = pathPastArray;
		
		Transform newTransform = CalculateTransformAlongPath(agentPathProgress, pathForwardArray, pathPastArray, forwardInTime); 
		agent.transform.position = newTransform.position;
		agent.transform.rotation = newTransform.rotation;

		oldDistance = distanceTotal;
	}
	
	private Vector3[] UpdatePathForward(Vector3 startPosition, Vector3 endPosition, bool _forwardInTime, Vector3[] lastPathForward, bool recalculate)
	{
		Vector3[] cornersForward = new Vector3[] { };

		bool canCalculatePathForward = true;
		
		if (pathForward == null) pathForward = new NavMeshPath();
			
		if (updatePathPerFrame || cornersForward.Length <= 0 || recalculate) canCalculatePathForward = NavMesh.CalculatePath(startPosition, endPosition, NavMesh.AllAreas, pathForward);		
		cachePathForward = pathForward.corners;
		cornersForward = cachePathForward;
		NavMeshHit hit;
		
		// Return calulated path if NavAgent is on viable NavMesh position, else return last good calculated path
		// This is to prevent the NavAgent from getting stuck
		return !canCalculatePathForward ? lastPathForward : cornersForward;
	}
	
	private Vector3[] UpdatePathPast(Vector3 startPos, Vector3 endPos, bool _forwardInTime, Vector3[] lastPathPast, bool recalculate)
	{
		Vector3[] cornersPast = new Vector3[] { };
		
		bool canCalculatePathPast = true;
		
		if (pathPast == null) pathPast = new NavMeshPath();

		if (updatePathPerFrame || cornersPast.Length <= 0 || recalculate) canCalculatePathPast = NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, pathPast);
		
		cachePathPast = pathPast.corners;
		cornersPast = cachePathPast;
		NavMeshHit hit;
		
		return !canCalculatePathPast ? lastPathPast : cornersPast;
	}
	
	// this is called every frame - in timeline preview you may be progressing forward or backward in time
	// depending on the direction of time, the point the NavAgent is moving towards is found and the clip progress is used to
	// find the exact location the NavAgent should be. Rotation towards or away from that point is given depending on the direction of time
	
	private Transform CalculateTransformAlongPath(float progress, Vector3[] pathForward, Vector3[] pathPast, bool forward)
	{
		Transform newTransform = agent.transform;
		Vector3 newDirection = agent.transform.forward;
		
		float distanceForward = PathDistance(pathForward);
		float distancePast = PathDistance(pathPast);
		
		DrawColoredLine(pathPast, Color.red);
		DrawColoredLine(pathForward, Color.blue);
		
		distanceTotal = distanceForward + distancePast;
		
		// Change progress percentage and path based on direction of time
		float progressPercent = forward ? 1 - progress : progress;
		Vector3[] newDirectionPath =  forward ? pathForward : pathPast;
		
		float pathPointsDistance = 0;
		
		// the path being analyzed is iterated over backwards (closest to furthest) so the points ahead and behind NavAgent's location can be found
		// it's highly likely that the first few points will be the ones that the NavAgent is between
		for (int i = newDirectionPath.Length - 1; i >= 1; i--)
		{
			// these assignments are theoretical - only if the point ahead actually is ahead of the NavAgent will they be used
			Vector3 pointPast = newDirectionPath[i];
			Vector3 pointForward = newDirectionPath[i - 1];
			float distanceBetweenPoints = Vector3.Distance(pointPast, pointForward);
			float pathPointBehindDistance = pathPointsDistance;
			pathPointsDistance += distanceBetweenPoints;
			float pathPointAheadDistance = pathPointsDistance;
			
			// distance the agent is along the whole path
			float agentProgress = progressPercent * distanceTotal;
			
			// the first time we find a point ahead of the NavAgent, we know the NavAgent is between [i] and [i - 1]
			// remember time may be progressing backwards using timeline preview, so past and forward just means in respect to the next frame
			if (pathPointAheadDistance > agentProgress)
			{
				// the distance the NavAgent is ahead of the point behind
				float agentProgressDistanceBetweenPoints = agentProgress - pathPointBehindDistance;
				// get the direction between the two points
				Vector3 directionToTravel = pointForward - pointPast;
				// normalize the direction so it's between -1 and 1
				directionToTravel.Normalize();
				// the new position for the NavAgent ahead of the point behind the NavAgent depending on its progress
				Vector3 newPosition = pointPast + (directionToTravel * agentProgressDistanceBetweenPoints);
				newTransform.position = Vector3.Lerp(agent.transform.position, newPosition, agentProgressDistanceBetweenPoints / distanceBetweenPoints);
				// depending on the direction of time the NavAgent will face towards or away from the end of the path we analyzed
				newDirection = forward ? -directionToTravel : directionToTravel;
				newTransform.rotation = Quaternion.Slerp(agent.transform.rotation, Quaternion.LookRotation(newDirection), Time.deltaTime * rotationSpeed);
			}
		}
		return newTransform;
	}
	
	private float PathDistance(Vector3[] points)
	{
		float totalDistance = 0;
		for (int i = 0; i < points.Length - 1; i++) totalDistance += Vector3.Distance(points[i], points[i + 1]);
		return totalDistance;
	}
	
	private void DrawColoredLine(Vector3[] points, Color color)
	{
		for (int i = 0; i < points.Length - 1; i++) Debug.DrawLine(points[i], points[i + 1], color);
	}
}
