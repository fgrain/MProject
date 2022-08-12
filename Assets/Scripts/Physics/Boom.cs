using UnityEngine;
using UnityEngine.EventSystems;

public class Boom : MonoBehaviour
{
    //GameObject particle;
    public float _boomsize = 10f;  //��ը�뾶

    private RaycastHit hit;
    public float _boomPower = 1000f;  //��ը������
    public bool _suction = false;

    // Use this for initialization
    private void Start()
    {
        //particle = Resources.Load("Boom") as GameObject;  //��Ч
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                //GameObject.Instantiate(particle, hit.point, Quaternion.identity);
                Collider[] collider = Physics.OverlapSphere(hit.point, _boomsize);  //��ȡ�����ΰ뾶X���ڵ���������
                foreach (Collider c in collider)
                {
                    if (c.TryGetComponent(out Rigidbody rigidbody))
                    {
                        int i = _suction ? -1 : 1;
                        rigidbody.AddExplosionForce(_boomPower * i, hit.point, _boomsize);
                    }
                    else
                    {
                        Debug.Log(c.name);
                    }
                }
            }
        }
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.DrawWireSphere(transform.position, _boomsize);
    //}
}