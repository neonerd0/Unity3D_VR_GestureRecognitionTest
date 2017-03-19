using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class Gesture
{
    public enum HANDEDNESS {LEFT,RIGHT};
    public HANDEDNESS m_Hand;
    public string m_Name;
    public List<SerializableVector3> m_Points;

    public Gesture(string name, HANDEDNESS hand, List<Vector3> points)
    {
        m_Name = name;
        m_Hand = hand;
        m_Points = new List<SerializableVector3>();
        foreach(Vector3 p in points)
        {
            m_Points.Add(p);
        }
    }

    List<Vector3> GetPoints()
    {
        List<Vector3> points = new List<Vector3>();
        foreach(SerializableVector3 v in m_Points)
        {
            points.Add(v);
        }
        return points;
    }
}

public class GestureLibrary {

    //Dictionary<string, Gesture> m_Gestures = new Dictionary<string, Gesture>();
    Dictionary<string, Dictionary<Gesture.HANDEDNESS, Gesture>> m_Gestures = new Dictionary<string, Dictionary<Gesture.HANDEDNESS, Gesture>>();

    string m_DataPath = "";
    public string DataPath { get; set; }

    public GestureLibrary(string path)
    {
        m_DataPath = path;
        LoadGestures();
    }

    public void AddGesture(string name, Gesture.HANDEDNESS hand, List<Vector3> points)
    {
        if(!m_Gestures.ContainsKey(name))
        {
            m_Gestures[name] = new Dictionary<Gesture.HANDEDNESS, Gesture>();
        }
        m_Gestures[name][hand] = new Gesture(name, hand, points);
    }

    public void DeleteGesture(string name)
    {
        if(m_Gestures.ContainsKey(name))
            m_Gestures.Remove(name);
    }

    public void SaveGestures()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream fs = File.Create(m_DataPath);
        Debug.Log("Saved: " + m_DataPath);

        List<Gesture> gestures = new List<Gesture>();
        foreach(var hand in m_Gestures)
        {
            //Left hand saving
            var leftHandGesture = hand.Value[Gesture.HANDEDNESS.LEFT];
            gestures.Add(leftHandGesture);

            //Right hand saving
            var rightHandGesture = hand.Value[Gesture.HANDEDNESS.RIGHT];
            gestures.Add(rightHandGesture);
        }

        bf.Serialize(fs, gestures);
        fs.Close();
    }

    public void LoadGestures()
    {
        if(!File.Exists(m_DataPath)) { return; }

        BinaryFormatter bf = new BinaryFormatter();
        FileStream fs = File.Open(m_DataPath, FileMode.Open);

        List<Gesture> gestures = (List<Gesture>)bf.Deserialize(fs);
        foreach(Gesture g in gestures)
        {
            if(!m_Gestures.ContainsKey(g.m_Name))
            {
                m_Gestures[g.m_Name] = new Dictionary<Gesture.HANDEDNESS, Gesture>();
            }
            m_Gestures[g.m_Name][g.m_Hand] = g;
            Debug.Log("Loaded gesture: " + g.m_Name + ((g.m_Hand == Gesture.HANDEDNESS.LEFT) ? "(Left)" : "(Right)"));
        }

        fs.Close();
    }

    public bool HasGesture(string name)
    {
        return m_Gestures.ContainsKey(name);
    }

    public bool GetGesture(string name, out List<Vector3> leftHandPoints, out List<Vector3> rightHandPoints)
    {
        leftHandPoints = new List<Vector3>();
        rightHandPoints = new List<Vector3>();

        //Not in library
        if (!m_Gestures.ContainsKey(name)) { return false; }

        //Left hand loading
        foreach (var point in m_Gestures[name][Gesture.HANDEDNESS.LEFT].m_Points)
        {
            leftHandPoints.Add(point);
        }

        //Right hand loading
        foreach (var point in m_Gestures[name][Gesture.HANDEDNESS.RIGHT].m_Points)
        {
            rightHandPoints.Add(point);
        }

        return true;
    }
}
