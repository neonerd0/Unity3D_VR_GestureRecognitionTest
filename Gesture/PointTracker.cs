using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(SphereCollider))]
public class PointTracker : MonoBehaviour {

    public bool m_LeftHanded = false;
    public int m_ID = 0;

    GestureTracker m_Owner;

    SphereCollider m_SphereCollider;
    MeshRenderer m_MeshRenderer;

    public void ToggleRender(bool show)
    {
        m_MeshRenderer.enabled = show;
    }

    public void ToggleCollider(bool enable)
    {
        m_SphereCollider.enabled = enable;
    }

    public void Resize(float radius)
    {
        transform.localScale = new Vector3(radius, radius, radius);
    }

    public void Reset()
    {
        SetColor(Color.red);
    }

    public void SetOwner(GestureTracker owner)
    {
        m_Owner = owner;
    }

    public void Setup(bool leftHand, int id)
    {
        m_LeftHanded = leftHand;
        m_ID = id;
        name = (leftHand ? "left_" : "right_") + id;
    }

    public void Arm(float radius, bool leftHand, int id)
    {
        Reset();
        Resize(radius);
        Setup(leftHand, id);
    }

    public void SetColor(Color c)
    {
        m_MeshRenderer.material.color = c;
    }

    void CollisionResponse()
    {
        SetColor(Color.green);
    }

	// Use this for initialization
	void Start () {
        m_SphereCollider = GetComponent<SphereCollider>();
        m_MeshRenderer = GetComponent<MeshRenderer>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnTriggerEnter(Collider col)
    {
        if(col.gameObject.CompareTag("PlayerHand"))
        {
            string hirechy = "";
            Transform obj = col.transform;
            while (obj != null)
            {
              hirechy += obj.name + " <- ";
              obj = obj.parent;
            }
            hirechy += "null";

            bool isLeftHand = hirechy.Contains("Left");

            if (isLeftHand == m_LeftHanded)
            {
                if(m_Owner != null)
                {
                    m_Owner.CheckTrackerCollision(this);
                }
                else
                {
                    GestureDetector.instance.TrackerCollisionCheck(this);
                }
            }
        }
        else
        {
            string hirechy = "";
            Transform obj = col.transform;
            while (obj != null)
            {
              hirechy += obj.name + " <- ";
              obj = obj.parent;
            }
            hirechy += "null";
            //Debug.Log(m_ID + ": Collided with " + col.gameObject.name + " tagged as " + col.tag);
            //Debug.Log(m_ID + ": " + hirechy);
         }
    }
}
