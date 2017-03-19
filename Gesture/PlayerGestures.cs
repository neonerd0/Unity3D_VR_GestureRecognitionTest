using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct GesturePair
{
    public string GestureMotionName;
    public string PlayerActionName;
}

public class PlayerGestures : MonoBehaviour {

    public static PlayerGestures instance;

    public GesturePair[] m_RecognizedPlayerActions;

    void Awake()
    {
        instance = this;
    }
}
