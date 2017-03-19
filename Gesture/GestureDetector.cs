using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void OnGestureSuccessEvent();
public delegate void OnGestureFailureEvent();

public class GestureDetector : MonoBehaviour {

    static public GestureDetector instance;

    [SerializeField]
    PointTracker m_PointPrefab;

    [SerializeField]
    float m_TrackerRadius = 0.5f;

    [SerializeField]
    float m_TrackSpeedScale = 0.1f;
    float m_TrackTimerLeft = 0.0f;
    float m_TrackTimerRight = 0.0f;
    float m_MinVelocityMag = 0.25f;

    [SerializeField]
    bool m_IsTrackingLeft = false;
    [SerializeField]
    bool m_IsTrackingRight = false;

    int m_IDLeftCurr = 0;
    int m_IDRightCurr = 0;
    float m_TrackerLeftMax = 0.0f;
    float m_TrackerRightMax = 0.0f;

    bool m_LeftHandSuccess = false;
    bool m_RightHandSuccess = false;

    [SerializeField]
    bool m_VisibleTrackers = false;
    bool m_TrackersAreVisible = false;

    [SerializeField]
    bool m_TrackersCollide = true;
    bool m_TrackersCanCollide = true;

    int m_LastActiveIndex = 0;
    List<PointTracker> m_PointsPool = new List<PointTracker>();
    List<PointTracker> m_PointTrackersLeft = new List<PointTracker>();
    List<PointTracker> m_PointTrackersRight = new List<PointTracker>();

    Dictionary<string, GestureTracker> m_TrackedGestures = new Dictionary<string, GestureTracker>();

    [SerializeField]
    OnGestureSuccessEvent m_SuccessEvent = null;
    [SerializeField]
    OnGestureFailureEvent m_FailureEvent = null;

    bool m_LTPrev = false;
    bool m_RTPrev = false;

    public bool VisibleTrackers() { return m_VisibleTrackers; }

    bool HasLeftTrackers()
    {
        return m_PointTrackersLeft.Count > 1;
    }

    bool HasRightTrackers()
    {
        return m_PointTrackersRight.Count > 1;
    }

    public float CalcTimeLimit(PointTracker a, PointTracker b)
    {
        return 1.0f + (a.transform.position - b.transform.position).sqrMagnitude;
    }

    public void MakeCurrentPersistant(string name, bool loops = true, float loopDelay = 0.1f, float velMinMag = 0.25f)
    {
        m_TrackedGestures[name] = new GestureTracker();
        m_TrackedGestures[name].SetUpGestureTracking(name, m_PointTrackersLeft, m_PointTrackersRight, m_SuccessEvent, m_FailureEvent, loops, loopDelay, velMinMag);
    }

    public int PersistantTrackersCount()
    {
        int c = 0;
        foreach(var tracker in m_TrackedGestures)
        {
            c += tracker.Value.TrackersCount();
        }
        return c;
    }

    public void LoadPersistantGesuture(string name, OnGestureSuccessEvent onSuccess, OnGestureFailureEvent onFail = null, bool loops = true, float loopDelay = 0.1f, float minVelMag = 0.25f)
    {
        List<Vector3> leftMP, rightMP;
        //Try loading the gesture, return on failure
        if(!GestureInput.instance.TryLoadGesture(name, out leftMP, out rightMP)) { return; }

        //Add new points to the pool if needed
        int used = PersistantTrackersCount();
        int add = leftMP.Count + rightMP.Count;
        if (used + add > m_PointsPool.Count)
        {
            CreateTrackers(used + add - m_PointsPool.Count);
        }

        //Align the points
        m_LastActiveIndex = used;
        AlignPoints(ref leftMP, out m_PointTrackersLeft, true);
        AlignPoints(ref rightMP, out m_PointTrackersRight, false);

        //Associate to the gesture tracker
        m_TrackedGestures[name] = new GestureTracker();
        m_TrackedGestures[name].SetUpGestureTracking(name, m_PointTrackersLeft, m_PointTrackersRight, onSuccess, onFail, loops, loopDelay, minVelMag);
    }

    public void ToggleAllPersistant(bool enable)
    {
        if(enable)
        {
            foreach (var track in m_TrackedGestures)
            {
                track.Value.StartTracking();
            }
        }
        else
        {
            foreach (var track in m_TrackedGestures)
            {
                track.Value.StopTracking();
            }
        }
    }

    public void TogglePersistant(string name, bool enable)
    {
        if(m_TrackedGestures.ContainsKey(name))
        {
            if (enable)
                m_TrackedGestures[name].StartTracking();
            else
                m_TrackedGestures[name].StopTracking();
        }
    }

    public void RemovePersistant(string name)
    {
        if(m_TrackedGestures.ContainsKey(name))
        {
            m_TrackedGestures.Remove(name);
        }
    }

    public void StartTracking()
    {
        m_IsTrackingLeft = HasLeftTrackers();
        m_IsTrackingRight = HasRightTrackers();

        m_IDLeftCurr = m_IDRightCurr = 0;
        m_TrackTimerLeft = m_TrackTimerRight = 0.0f;

        m_LeftHandSuccess = !m_IsTrackingLeft;
        m_RightHandSuccess = !m_IsTrackingRight;

        SetAllTrackers(m_VisibleTrackers, m_TrackersCollide, Color.red);
        if (m_IsTrackingLeft)
        {
            m_PointTrackersLeft[0].SetColor(Color.yellow);
            m_TrackerLeftMax = CalcTimeLimit(m_PointTrackersLeft[0], m_PointTrackersLeft[1]);
        }
        if(m_IsTrackingRight)
        {
            m_PointTrackersRight[0].SetColor(Color.yellow);
            m_TrackerRightMax = CalcTimeLimit(m_PointTrackersRight[0], m_PointTrackersRight[1]);
        }
    }

    public void StopTracking()
    {
        ToggleAllTrackers(false, false);
        m_IsTrackingLeft = m_IsTrackingRight = false;
    }

    public void SetupEvents(OnGestureSuccessEvent success, OnGestureFailureEvent failure = null)
    {
        m_SuccessEvent = success;
        m_FailureEvent = failure;
    }

    public void SetupTrackers (ref List<Vector3> leftHandPoints, ref List<Vector3> rightHandPoints)
    {
        //Create points to render if we are out of pooled points
        int count = leftHandPoints.Count + rightHandPoints.Count;
        if(count > m_PointsPool.Count)
        {
            Debug.Log("Instantiating more trackers " + (count - m_PointsPool.Count) + " more needed");
            CreateTrackers(count - m_PointsPool.Count);
            Debug.Log("Total trackers: " + m_PointsPool.Count);
        }

        m_LastActiveIndex = 0;
        Debug.Log("Aligning left trackers");
        AlignPoints(ref leftHandPoints, out m_PointTrackersLeft, true);
        Debug.Log("Aligning right trackers");
        AlignPoints(ref rightHandPoints, out m_PointTrackersRight, false);
    }

    void ToggleTrackers(ref List<PointTracker> trackers, bool render, bool collides)
    {
        foreach(PointTracker tracker in trackers)
        {
            tracker.ToggleRender(render);
            tracker.ToggleCollider(collides);
        }
    }

    void SetTrackers(ref List<PointTracker> trackers, bool render, bool collides, Color c)
    {
        foreach (PointTracker tracker in trackers)
        {
            tracker.ToggleRender(render);
            tracker.ToggleCollider(collides);
            tracker.SetColor(c);
        }
    }

    public void SetAllTrackers(bool render, bool collides, Color c)
    {
        SetTrackers(ref m_PointTrackersLeft, render, collides, c);
        SetTrackers(ref m_PointTrackersRight, render, collides, c);
    }

    public void ToggleAllTrackers(bool render, bool collides)
    {
        ToggleTrackers(ref m_PointTrackersLeft, render, collides);
        ToggleTrackers(ref m_PointTrackersRight, render, collides);
    }

    void CreateTrackers(int n)
    {
        for(int i = 0; i < n; ++i)
        {
            GameObject newtracker = Instantiate(m_PointPrefab.gameObject);
            newtracker.transform.SetParent(transform);
            PointTracker newpt = newtracker.GetComponent<PointTracker>();
            m_PointsPool.Add(newpt);
        }
    }

    void AlignPoints(ref List<Vector3> points, out List<PointTracker> trackers, bool leftHand)
    {
        trackers = new List<PointTracker>();
        int id = 0;
        foreach(Vector3 point in points)
        {
            Debug.Log("Pool ID " + m_LastActiveIndex + " aligned");
            PointTracker currtracker = m_PointsPool[m_LastActiveIndex];

            //Transform point to local space of player
            Vector3 currpoint = point;
            currtracker.transform.position = currpoint;

            //Setup the tracker
            currtracker.Resize(m_TrackerRadius);
            currtracker.Setup(leftHand, id++);

            trackers.Add(currtracker);
            m_LastActiveIndex++;
        }
    }

    void Awake() {
        instance = this;
    }

    void CallSuccessEvent()
    {
        if (m_SuccessEvent != null)
        {
            m_SuccessEvent();
        }
    }

    void CallFailureEvent()
    {
        if(m_FailureEvent != null)
        {
            m_FailureEvent();
        }
        else
        {
            DefaultFailureEvent();
        }
    }

    public bool TrackerCollisionCheck (PointTracker tracker)
    {
        //No tracking being done
        if(!m_IsTrackingLeft && !m_IsTrackingRight) { return false; }
        //Wrong hand tracker
        if(m_IsTrackingLeft && !tracker.m_LeftHanded) { return false; }
        if(m_IsTrackingRight && tracker.m_LeftHanded) { return false; }

        //Find relevant hand information to check
        List<PointTracker> trackpoints = tracker.m_LeftHanded ? m_PointTrackersLeft : m_PointTrackersRight;
        int findID = tracker.m_LeftHanded ? m_IDLeftCurr : m_IDRightCurr;
        float trackerVel = tracker.m_LeftHanded ? GestureInput.instance.GetLeftTrackerVelocityMagnitude() : GestureInput.instance.GetRightTrackerVelocityMagnitude();

        //Not moving fast enough to trigger the point
        if(trackerVel < m_MinVelocityMag) { return false; }

        //Found the right one, gesture is on track
        if(findID == tracker.m_ID)
        {
            //toggle the tracker
            tracker.SetColor(Color.green);
            tracker.ToggleCollider(false);

            //Check if the path is finished
            if(tracker.m_ID >= trackpoints.Count - 1)
            {
                //Path is complete! Gesture has been performed successfully for this hand!
                if(tracker.m_LeftHanded) { m_LeftHandSuccess = true; }
                else { m_RightHandSuccess = true; }

                if(m_RightHandSuccess && m_LeftHandSuccess)
                {
                    CallSuccessEvent();
                }
            }
            //Path is not done, get the next tracker
            else
            {
                //Increment tracker counter
                if(tracker.m_LeftHanded)
                {
                    findID = ++m_IDLeftCurr;
                }
                else
                {
                    findID = ++m_IDRightCurr;
                }

                //Update the next tracker to find
                trackpoints[findID].SetColor(Color.yellow);

                //Update the tracker time limit for the hand
                if(tracker.m_LeftHanded)
                {
                    m_TrackerLeftMax = CalcTimeLimit(tracker, trackpoints[findID]);
                }
                else
                {
                    m_TrackerRightMax = CalcTimeLimit(tracker, trackpoints[findID]);
                }
            }

            return true;
        }

        return false;
    }
	
    void DefaultFailureEvent()
    {
        StartTracking();
        //SetAllTrackers(false, false, Color.black);
    }

    bool HasFailedGesture()
    {
        return !m_IsTrackingLeft && !m_IsTrackingRight;
    }

	// Update is called once per frame
	void Update () {

        //Debug update visibility and collision for tracker points
		if(m_VisibleTrackers != m_TrackersAreVisible
            || m_TrackersCollide != m_TrackersCanCollide)
        {
            m_TrackersAreVisible = m_VisibleTrackers;
            m_TrackersCanCollide = m_TrackersCollide;
            ToggleAllTrackers(m_TrackersAreVisible, m_TrackersCanCollide);
        }

        //Update left trackers
        if(m_IsTrackingLeft)
        {
            m_TrackTimerLeft += Time.deltaTime * m_TrackSpeedScale;
            //Out of time to track
            if(m_TrackTimerLeft >= m_TrackerLeftMax)
            {
                m_IsTrackingLeft = false;
                CallFailureEvent();
            }
        }

        //Update right trackers
        if(m_IsTrackingRight)
        {
            m_TrackTimerRight += Time.deltaTime * m_TrackSpeedScale;
            //Out of time to track
            if (m_TrackTimerRight >= m_TrackerRightMax)
            {
                m_IsTrackingRight = false;
                CallFailureEvent();
            }
        }

        foreach(var tracker in m_TrackedGestures)
        {
            if(tracker.Value.IsTracking())
            {
                tracker.Value.Update();
            }
        }
	}
}
