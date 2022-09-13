using UnityEngine;
using UnityEngine.InputSystem;

public class LockTarget : MonoBehaviour
{
    [SerializeField] private float noticeZone = 20f;
    [SerializeField] private float maxNoticeAngle = 60f;
    [SerializeField] private LayerMask character = -1;
    [SerializeField] Transform lockOnCanvas;
    [SerializeField] float crossHair_Scale = 0.1f;
    [SerializeField] float lookAtSmoothing = 2;
    [SerializeField] Transform enemyTarget_Locator;
    [SerializeField] CameraFollow camFollow;
    private float currentYOffset;
    private Transform cam;
    private Transform currentTarget;
    bool enemyLocked;
    Vector3 UIpos;
    private void Start()
    {
        cam = Camera.main.transform;
    }

    private void Update()
    {
        camFollow.lockedTarget = enemyLocked;
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            if (currentTarget)
            {
                //If there is already a target, Reset.
                ResetTarget();
                return;
            }

            if (currentTarget = ScanNearBy()) FoundTarget(); else ResetTarget();
        }

        if (enemyLocked)
        {
            if (!TargetOnRange()) ResetTarget();
            LookAtTarget();
        }
    }

    private Transform ScanNearBy()
    {
        Collider[] nearbyTargets = Physics.OverlapSphere(transform.position, noticeZone, character);
        float closestAngle = maxNoticeAngle;
        Transform closestTarget = null;
        if (nearbyTargets.Length <= 0) return null;

        for (int i = 0; i < nearbyTargets.Length; i++)
        {
            Vector3 dir = nearbyTargets[i].transform.position - cam.position;
            dir.y = 0;
            float _angle = Vector3.Angle(cam.forward, dir);

            if (_angle < closestAngle)
            {
                closestTarget = nearbyTargets[i].transform;
                closestAngle = _angle;
            }
        }

        if (!closestTarget) return null;
        float h1 = closestTarget.GetComponent<CapsuleCollider>().height;
        float h2 = closestTarget.localScale.y;
        float h = h1 * h2;
        float half_h = (h / 2) / 2;
        currentYOffset = h - half_h;
        //if (zeroVert_Look && currentYOffset > 1.6f && currentYOffset < 1.6f * 3) currentYOffset = 1.6f;
        Vector3 tarPos = closestTarget.position + new Vector3(0, currentYOffset, 0);
        //if (Blocked(tarPos)) return null;
        return closestTarget;
    }

    void FoundTarget()
    {
        Vector3 lastCamPos = cam.position;
        CameraController.SetCameraMode(ECameraMode.EnemyTarger);
        camFollow.LockTarget = currentTarget;
        cam.position = lastCamPos;
        lockOnCanvas.gameObject.SetActive(true);
        enemyLocked = true;
    }

    void ResetTarget()
    {
        CameraController.SetCameraMode(ECameraMode.Character);
        lockOnCanvas.gameObject.SetActive(false);
        currentTarget = null;
        camFollow.LockTarget = null;
        enemyLocked = false;
    }
    bool TargetOnRange()
    {
        float dis = (transform.position - UIpos).magnitude;
        if (dis / 2 > noticeZone) return false; else return true;
    }
    private void LookAtTarget()
    {
        if (currentTarget == null)
        {
            ResetTarget();
            return;
        }
        UIpos = currentTarget.position + new Vector3(0, currentYOffset, 0);
        lockOnCanvas.position = UIpos;
        lockOnCanvas.localScale = Vector3.one * ((cam.position - UIpos).magnitude * crossHair_Scale);
        enemyTarget_Locator.position = UIpos;
        Vector3 dir = currentTarget.position - transform.position;
        dir.y = 0;
        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * lookAtSmoothing);
    }

    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireSphere(transform.position, noticeZone);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward) ;
    }
}