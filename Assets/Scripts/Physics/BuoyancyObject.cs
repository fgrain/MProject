using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BuoyancyObject : MonoBehaviour
{
    public Transform[] floaters;
    public float underWaterDrag = 3f;
    public float underWaterAngularDrag = 1f;

    public float airDrag = 0f;
    public float airAngularDrag = 0.05f;

    public float floatingpower = 15f;

    public float waterHeight = 0f;

    private Rigidbody m_Rigidbody;

    private int floatersUnderwater;

    private bool underwater;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        floatersUnderwater = 0;
        if (underwater)
        {
            for (int i = 0; i < floaters.Length; i++)
            {
                float diff = floaters[i].position.y - waterHeight;

                if (diff < 0)
                {
                    m_Rigidbody.AddForceAtPosition(Vector3.up * floatingpower * Mathf.Abs(diff), floaters[i].position, ForceMode.Force);
                    floatersUnderwater += 1;
                }
            }
        }
        SwitchState(underwater);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Water")
        {
            underwater = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Water")
        {
            underwater = false;
        }
    }

    private void SwitchState(bool isUnderWater)
    {
        if (isUnderWater)
        {
            m_Rigidbody.drag = underWaterDrag;
            m_Rigidbody.angularDrag = underWaterAngularDrag;
        }
        else
        {
            m_Rigidbody.drag = airDrag;
            m_Rigidbody.angularDrag = airAngularDrag;
        }
    }
}