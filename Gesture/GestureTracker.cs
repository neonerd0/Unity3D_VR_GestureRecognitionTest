using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureTracker {

    string m_GestureName;
    bool m_IsTracking = false;
    bool m_Loop = false;

    float m_LoopDelay = 0.15f;
    float m_LoopDelayTimer = 0.0f;

    float m_VelMagMin = 0.25f;

    int m_CurrIndexLeft = 0;
    int m_CurrIndexRight = 0;

    float m_TimeLimit = 0.0f;

    OnGestureSuccessEvent m_GestureSuccessEvent;
    OnGestureFailureEvent m_GestureFailureEvent;

    List<PointTracker> m_TrackersLeftHand = new List<PointTracker>();
    List<PointTracker> m_TrackersRightHand = new List<PointTracker>();

    public bool IsTracking() { return m_IsTracking; }

    public int TrackersCount() { return m_TrackersLeftHand.Count + m_TrackersRightHand.Count; }

    public void SetUpGestureTracking(string gestureName,
        List<PointTracker> trackersLeft, List<PointTracker> trackersRight,
        OnGestureSuccessEvent successEvent, OnGestureFailureEvent failureEvent = null,
        bool loop = true, float loopDelay = 0.1f, float velMagMin = 0.25f)
    {
        m_GestureName = gestureName;
        m_TrackersLeftHand = trackersLeft;
        m_TrackersRightHand = trackersRight;
        m_GestureSuccessEvent = successEvent;
        m_GestureFailureEvent = failureEvent;
        m_Loop = loop;
        m_LoopDelay = loopDelay;
        m_VelMagMin = velMagMin;

        foreach(PointTracker pt in m_TrackersLeftHand)
        {
            pt.SetOwner(this);
        }

        foreach(PointTracker pt in m_TrackersRightHand)
        {
            pt.SetOwner(this);
        }
    }

    public void StartTracking()
    {
        m_IsTracking = true;
        m_CurrIndexLeft = m_CurrIndexRight = 0;
        SetAllTrackers(GestureDetector.instance.VisibleTrackers(), true, Color.red);
        if (m_TrackersLeftHand.Count != 0) { m_TrackersLeftHand[0].SetColor(Color.yellow); }
        if (m_TrackersRightHand.Count != 0) { m_TrackersRightHand[0].SetColor(Color.yellow); }
    }

    public void StopTracking()
    {
        m_IsTracking = false;
        SetAllTrackers(GestureDetector.instance.VisibleTrackers(), false, Color.red);
        if (m_Loop) { m_LoopDelayTimer = m_LoopDelay; }
    }

    public bool CheckTrackerCollision(PointTracker tracker)
    {
        //Check for tracking
        if(!m_IsTracking) { return false; }

        //Check for ID
        int expectedID = tracker.m_LeftHanded ? m_CurrIndexLeft : m_CurrIndexRight;
        if(tracker.m_ID != expectedID) { return false; }

        List<PointTracker> trackers = tracker.m_LeftHanded ? m_TrackersLeftHand : m_TrackersRightHand;
        float currVel = tracker.m_LeftHanded ? GestureInput.instance.GetLeftTrackerVelocityMagnitude() : GestureInput.instance.GetRightTrackerVelocityMagnitude();

        //Too slow to hit the tracker
        if(currVel < m_VelMagMin) { return false; }

        //Successfully hit an expected tracker, toggle it off
        tracker.SetColor(Color.green);
        tracker.ToggleCollider(false);

        int nextID = tracker.m_ID + 1;
        if(nextID >= trackers.Count)
        {
            //Finished gesture, return success if both hands are complete
            if(tracker.m_LeftHanded)
            {
                m_CurrIndexLeft = 0;
            }
            else
            {
                m_CurrIndexRight = 0;
            }

            if(m_CurrIndexRight == 0 && m_CurrIndexLeft == 0)
            {
                CallSuccessEvent();
            }
        }
        else
        {
            //Update the next tracker
            trackers[nextID].SetColor(Color.yellow);
            if(tracker.m_LeftHanded)
            {
                m_CurrIndexLeft = nextID;
            }
            else
            {
                m_CurrIndexRight = nextID;
            }
        }

        return true;
    }

    void DefaultFailureEvent()
    {
        SetAllTrackers(GestureDetector.instance.VisibleTrackers(), false, Color.black);
    }

    void SetAllTrackers(bool render, bool collides, Color c)
    {
        SetTrackers(ref m_TrackersLeftHand, render, collides, c);
        SetTrackers(ref m_TrackersRightHand, render, collides, c);
    }

    void SetTrackers(ref List<PointTracker> trackers, bool render, bool collides, Color c)
    {
        foreach(PointTracker pt in trackers)
        {
            pt.SetColor(c);
            pt.ToggleCollider(collides);
            pt.ToggleRender(render);
        }
    }

    void CallSuccessEvent()
    {
        if(m_GestureSuccessEvent != null)
        {
            m_GestureSuccessEvent();
        }
        StopTracking();
    }

    void CallFailureEvent()
    {
        if(m_GestureFailureEvent != null)
        {
            m_GestureFailureEvent();
        }
        else
        {
            DefaultFailureEvent();
        }
        StopTracking();
    }
	
	// Update is called once per frame
	public void Update () {
        //Auto loop tracking
		if(m_Loop && m_LoopDelayTimer <= 0.0f && !m_IsTracking)
        {
            StartTracking();
        }

        if(m_LoopDelayTimer > 0)
        {
            m_LoopDelayTimer -= Time.deltaTime;
        }

        if(m_TimeLimit > 0)
        {
            m_TimeLimit -= Time.deltaTime;
        }

        if(m_IsTracking && m_TimeLimit <= 0.0f)
        {
            CallFailureEvent();
        }
	}
}
