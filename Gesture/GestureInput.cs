using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OVRTouchSample;

public class GestureInput : MonoBehaviour {

    public static GestureInput instance;

    [Header("Core Variables")]
    [SerializeField]
    [Range(0.01f, 1.0f)]
    float m_TimeStep = 0.01f;

    [SerializeField]
    float m_MemoryLimitKB = 8.0f;

    [SerializeField]
    TrackedController m_TCLeft, m_TCRight;
    VelocityTracker m_VTLeft, m_VTRight;

    List<Vector3> m_LeftMotionPoints = new List<Vector3>();
    List<Vector3> m_RightMotionPoints = new List<Vector3>();

    [SerializeField]
    Text m_StatusTextLeft, m_StatusTextRight, m_StatusGeneral1, m_StatusGeneral2;

    [SerializeField]
    GameObject m_PointPrefab;

    [SerializeField]
    bool m_UseLineRenderer = true;
    [SerializeField]
    bool m_UsePointPrefab = false;

    [SerializeField]
    LineRenderer m_LineRendererLeft, m_LineRendererRight;

    public enum INPUT_MODE { RECORD, PLAY};
    [Header("Input mode variables")]
    [SerializeField]
    INPUT_MODE m_InputMode = INPUT_MODE.RECORD;

    [Header("Gesture Library Variables")]
    [SerializeField]
    InputField m_InputField;
    [SerializeField]
    InputField m_PreviewField;

    public GestureLibrary m_GestureLib;

    void Awake()
    {
        instance = this;
        m_GestureLib = new GestureLibrary(Application.dataPath + "/BHZ.GLIB");
        m_VTLeft = m_TCLeft.GetComponent<VelocityTracker>();
        m_VTRight = m_TCRight.GetComponent<VelocityTracker>();
    }

    public float GetLeftTrackerVelocityMagnitude() { return m_VTLeft.TrackedLinearVelocity.magnitude; }
    public float GetRightTrackerVelocityMagnitude() { return m_VTRight.TrackedLinearVelocity.magnitude; }

    // Use this for initialization
    void Start () {
        //Approx 24kb of memory used for point capture on init
        m_LeftMotionPoints.Capacity = 1000;
        m_RightMotionPoints.Capacity = 1000;
        m_LineRendererLeft.startColor = m_LineRendererLeft.endColor = Color.red;
        m_LineRendererRight.startColor = m_LineRendererRight.endColor = Color.blue;
        ToggleLineRenderers(false);
        StartCoroutine(ControllerRecord());

        m_GestureLib.LoadGestures();
        m_InputField.gameObject.SetActive(false);
        m_PreviewField.gameObject.SetActive(false);
    }

    void ToggleTrackedPoints(bool active)
    {
        foreach(Transform child in transform)
        {
            child.gameObject.SetActive(active);
        }
    }

    void ToggleLineRenderers(bool toggle)
    {
        m_LineRendererLeft.enabled = m_LineRendererRight.enabled = toggle;
    }

    void DestroyTrackedPoints()
    {
        if(m_UseLineRenderer)
        {
            ToggleLineRenderers(false);
        }
        if(m_UsePointPrefab)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    float AxisPointCatMulSpline(float a1, float a2, float a3, float a4, float t, float t2, float t3)
    {
        return 0.5f * ((2.0f * a2) + (-a1 + a3)  * t + (2.0f * a1 - 5.0f * a2 + 4 * a3 - a4) * t2 +
        (-a1 + 3.0f * a2 - 3.0f * a3 + a4) * t3);
    }

    Vector3 CatMulRomSpline(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
    {
        Vector3 newp = Vector3.zero;

        float t2 = t * t;
        float t3 = t2 * t;

        newp.x = AxisPointCatMulSpline(p1.x, p2.x, p3.x, p4.x, t, t2, t3);
        newp.y = AxisPointCatMulSpline(p1.y, p2.y, p3.y, p4.y, t, t2, t3);
        newp.z = AxisPointCatMulSpline(p1.z, p2.z, p3.z, p4.z, t, t2, t3);

        return newp;
    }

    void SmoothTrackedPoints(ref List<Vector3> points)
    {
        //Not enough points to smooth
        if(points.Count < 4)
        {
            return;
        }

        if(points.Count*2*12 > m_MemoryLimitKB*1000)
        {
            return;
        }

        List<Vector3> newpoints = new List<Vector3>();
        int numpoints = points.Count;
        //Begin catmulrom-spline
        for(int i = 0; i < points.Count-3; ++i)
        {
            for(int j = 0; j < numpoints; ++j)
            {
                newpoints.Add(CatMulRomSpline(points[i], points[i + 1], points[i + 2], points[i + 3], (1.0f / numpoints) * j));
            }
        }
        newpoints.Add(points[points.Count - 2]);
        points = newpoints;
    }

    void CreateTrackedPoints(ref List<Vector3> points, ref LineRenderer lr)
    {
        if(m_UseLineRenderer)
        {
            lr.numPositions = points.Count;
            lr.SetPositions(points.ToArray());
            lr.enabled = true;
        }
        if (m_UsePointPrefab)
        {
            foreach (Vector3 point in points)
            {
                Instantiate(m_PointPrefab, point, Quaternion.identity, transform);
            }
        }
    }

    IEnumerator ControllerRecord()
    {
        while(true)
        {
            if(m_InputMode == INPUT_MODE.RECORD)
            {
                if (m_TCLeft.Trigger > 0.0f && GetMemoryUsage() < m_MemoryLimitKB)
                {
                    m_LeftMotionPoints.Add(m_TCLeft.transform.position);
                    CreateTrackedPoints(ref m_LeftMotionPoints, ref m_LineRendererLeft);
                    m_StatusTextLeft.text = "recording";
                }
                else
                {
                    m_StatusTextLeft.text = "idle";
                }

                if (m_TCRight.Trigger > 0.0f && GetMemoryUsage() < m_MemoryLimitKB)
                {
                    m_RightMotionPoints.Add(m_TCRight.transform.position);
                    CreateTrackedPoints(ref m_RightMotionPoints, ref m_LineRendererRight);
                    m_StatusTextRight.text = "recording";
                }
                else
                {
                    m_StatusTextRight.text = "idle";
                }
            }
            else if (m_InputMode == INPUT_MODE.PLAY)
            {
                if(m_TCLeft.OnTriggerDown())
                {
                    GestureDetector.instance.StartTracking();
                }
                else if (m_TCLeft.OnTriggerUp())
                {
                    GestureDetector.instance.StopTracking();
                }
            }

            //MouseDebugPoints();
            yield return new WaitForSeconds(m_TimeStep);
        }
    }

    void MouseDebugPoints()
    {
        if(Input.GetMouseButton(1) && GetMemoryUsage() < m_MemoryLimitKB)
        {
            Vector3 mp = Input.mousePosition;
            mp.z = 10.0f;
            mp = Camera.main.ScreenToWorldPoint(mp);
            m_LeftMotionPoints.Add(mp);
        }
    }

    void KeyboardInput()
    {
        if(Input.GetKeyDown(KeyCode.Return))
        {
            CreateAllTrackedPoints();
        }

        if(Input.GetKeyDown(KeyCode.Backspace))
        {
            ClearAllTrackedPoints();
        }

        if(Input.GetKeyDown(KeyCode.RightShift) && GetMemoryUsage() < m_MemoryLimitKB)
        {
            SmoothAllTrackedPoints();
        }

        //Add to gesture library
        if(Input.GetKeyDown(KeyCode.F1))
        {
            m_InputField.gameObject.SetActive(true);
            m_InputField.Select();
        }

        //Serialize library
        if(Input.GetKeyDown(KeyCode.F2))
        {
            m_GestureLib.SaveGestures();
        }

        //Preview Gesture
        if(Input.GetKeyDown(KeyCode.F3))
        {
            m_PreviewField.gameObject.SetActive(true);
            m_PreviewField.Select();
        }

        //Try Detect Gesture
    }

    public void NormalizePoints(Vector3 cameraPos, Quaternion cameraRot, ref List<Vector3> points)
    {
        for(int i = 0; i < points.Count; ++i)
        {
            points[i] -= cameraPos;
            points[i] = Quaternion.Inverse(cameraRot) * points[i];
        }
    }

    public void TransformPointsToCameraSpace(ref List<Vector3> points)
    {
        for(int i = 0; i < points.Count; ++i)
        {
            points[i] = GestureDetector.instance.transform.rotation * points[i];
            points[i] += GestureDetector.instance.transform.position;
        }
    }

    public void AddGestureToLibrary()
    {
        if(m_InputField.text == "") {
            m_InputField.gameObject.SetActive(false);
            return;
        }
        Transform camTransform = GestureDetector.instance.transform;
        NormalizePoints(camTransform.position, camTransform.rotation, ref m_LeftMotionPoints);
        NormalizePoints(camTransform.position, camTransform.rotation, ref m_RightMotionPoints);
        m_GestureLib.AddGesture(m_InputField.text, Gesture.HANDEDNESS.LEFT, m_LeftMotionPoints);
        m_GestureLib.AddGesture(m_InputField.text, Gesture.HANDEDNESS.RIGHT, m_RightMotionPoints);
        Debug.Log(m_InputField.text + " added to gesture library");
        m_InputField.text = "";
        m_InputField.gameObject.SetActive(false);
    }

    public bool TryLoadGesture(string name, out List<Vector3> leftMotionPoints, out List<Vector3> rightMotionPoints)
    {
        if(m_GestureLib.GetGesture(name, out leftMotionPoints, out rightMotionPoints))
        {
            TransformPointsToCameraSpace(ref leftMotionPoints);
            TransformPointsToCameraSpace(ref rightMotionPoints);
            return true;
        }
        return false;
    }

    public void LoadAndUseGesture(string name, OnGestureSuccessEvent successEvent, OnGestureFailureEvent failureEvent = null)
    {
        //Try to load the gesture from the library
        if(!m_GestureLib.GetGesture(name, out m_LeftMotionPoints, out m_RightMotionPoints)) { return; }

        TransformPointsToCameraSpace(ref m_LeftMotionPoints);
        TransformPointsToCameraSpace(ref m_RightMotionPoints);

        //CreateAllTrackedPoints();
        GestureDetector.instance.SetupTrackers(ref m_LeftMotionPoints, ref m_RightMotionPoints);
        GestureDetector.instance.SetupEvents(successEvent, failureEvent);
        GestureDetector.instance.StartTracking();
    }

    public void PreviewGesture()
    {
        if(!m_GestureLib.HasGesture(m_PreviewField.text)) {
            if(m_PreviewField.text != "")
                Debug.Log(m_PreviewField.text + " does not exist");
            m_PreviewField.text = "";
            m_PreviewField.gameObject.SetActive(false);
            return;
        }
        m_GestureLib.GetGesture(m_PreviewField.text, out m_LeftMotionPoints, out m_RightMotionPoints);
        TransformPointsToCameraSpace(ref m_LeftMotionPoints);
        TransformPointsToCameraSpace(ref m_RightMotionPoints);
        CreateAllTrackedPoints();

        GestureDetector.instance.SetupTrackers(ref m_LeftMotionPoints, ref m_RightMotionPoints);

        Debug.Log(m_PreviewField.text + " loaded");
        m_PreviewField.text = "";
        m_PreviewField.gameObject.SetActive(false);
    }
	
    void CreateAllTrackedPoints()
    {
        CreateTrackedPoints(ref m_LeftMotionPoints, ref m_LineRendererLeft);
        CreateTrackedPoints(ref m_RightMotionPoints, ref m_LineRendererRight);
    }

    void SmoothAllTrackedPoints()
    {
        SmoothTrackedPoints(ref m_LeftMotionPoints);
        SmoothTrackedPoints(ref m_RightMotionPoints);
    }

    void ClearAllTrackedPoints()
    {
        DestroyTrackedPoints();
        m_LeftMotionPoints.Clear();
        m_RightMotionPoints.Clear();
    }

    float GetMemoryUsage()
    {
        return ((float)(m_LeftMotionPoints.Count + m_RightMotionPoints.Count) * 12) / 1000.0f;
    }

    void UpdateGeneralStatus()
    {
        float res = ((float)(m_LeftMotionPoints.Capacity + m_RightMotionPoints.Capacity) * 12) / 1000.0f;
        float mem = GetMemoryUsage();
        m_StatusGeneral1.text = res.ToString();
        m_StatusGeneral2.text = mem.ToString();
    }

    void GesturePlayUpdate()
    {
        if (m_InputMode != INPUT_MODE.PLAY) { return; }

        if (m_TCLeft.OnTriggerDown())
        {
            GestureDetector.instance.StartTracking();
            Debug.Log("Down");
        }
        else if (m_TCLeft.OnTriggerUp())
        {
            GestureDetector.instance.StopTracking();
            Debug.Log("Up");
        }
    }

	void FixedUpdate()
    {
        ////Render tracked points
        //if (m_TCLeft.Button1)
        //{
        //    CreateAllTrackedPoints();
        //}

        //Smooth then render tracked points
        if (m_TCLeft.Button2)
        {
            SmoothAllTrackedPoints();
            CreateAllTrackedPoints();
        }

        //Destroy tracked points
        if (m_TCRight.Button2)
        {
            ClearAllTrackedPoints();
            GestureDetector.instance.ToggleAllTrackers(false, false);
            //GestureDetector.instance.StopTracking();
        }

        if(m_TCLeft.Button1 && m_TCRight.Button1)
        {
            GestureDetector.instance.StartTracking();
        }

        KeyboardInput();
        GesturePlayUpdate();
        //Update general status and mem usage
        UpdateGeneralStatus();
    }
}
