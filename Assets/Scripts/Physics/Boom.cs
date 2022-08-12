using UnityEngine;
using UnityEngine.EventSystems;

public class Boom : MonoBehaviour
{
    //GameObject particle;
    public float _boomsize = 10f;  //爆炸半径

    private RaycastHit hit;
    public float _boomPower = 1000f;  //爆炸的力度
    public bool _suction = false;

    // Use this for initialization
    private void Start()
    {
        //particle = Resources.Load("Boom") as GameObject;  //特效
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
                Collider[] collider = Physics.OverlapSphere(hit.point, _boomsize);  //获取点球形半径X的内的所有物体
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