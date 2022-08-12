using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraModel : MonoBehaviour
{
    //��ת����
    private float xspeed = -0.05f; // X����
    private float yspeed = 0.1f;   // y����

    private Vector3 center; // �ӽ����ĵ�

    enum RotationAxes { MouseXAndY, MouseX, MouseY }
    RotationAxes axes = RotationAxes.MouseXAndY;

    float sensitivityX = 15;
    float sensitivityY = 15;
    float sensitivityC = 50;
    float minimumY = -80;
    float maximumY = 80;
    private float rotationY = 0;
    public float min_distance = 1; //���С����
    public float max_distance = 150; // ������

    private void Start()
    {
        center = Vector3.zero;
    }

    private void Update()
    {
        if (Input.GetMouseButton(1)) // �Ҽ���ת
        {
            if (axes == RotationAxes.MouseXAndY)
            {
                float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;
                rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
                transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
            }
            else if (axes == RotationAxes.MouseX)
            {
                transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
            }
            else
            {
                rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
                transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
            }
        }

        if (Input.GetMouseButton(2)) // �м�ƽ��
        {
            Vector3 p0 = transform.position;
            Vector3 p01 = p0 - transform.right * Input.GetAxisRaw("Mouse X") * 1 * Time.timeScale;
            Vector3 p03 = p01 - transform.up * Input.GetAxisRaw("Mouse Y") * 1 * Time.timeScale;

            center = new Vector3(p03.x, 0, p03.z);
            transform.position = p03;
        }
        var c = Camera.main;
        if (Input.GetAxis("Mouse ScrollWheel") > 0) // �ӽ�����
        {
            float d = Vector3.Distance(center, transform.position);
            if (d >= min_distance)
            {
                var dir = transform.position - center;
                dir = dir.normalized * (d - 10 * sensitivityC * Time.deltaTime);
                transform.position = dir + center;
                if (d <= min_distance)
                {
                    dir = dir.normalized * (min_distance);
                    transform.position = dir + center;
                }
            }
        }

        if (Input.GetAxis("Mouse ScrollWheel") < 0) // �ӽ���Զ
        {
            float d = Vector3.Distance(center, transform.position);
            if (Camera.main.fieldOfView <= max_distance)
            {
                var dir = transform.position - center;
                dir = dir.normalized * (d + 10 * sensitivityC * Time.deltaTime);
                transform.position = dir + center;
                if (c.fieldOfView >= max_distance)
                {
                    dir = dir.normalized * (max_distance);
                    transform.position = dir + center;
                }
            }
        }
    }
}

