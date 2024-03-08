using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbController : MonoBehaviour
{
    ClimbPoint currentPoint;

    [SerializeField] private EnvironmentScanner envScanner;
    [SerializeField] private PlayerController playerController;

    void Update()
    {
        if (!playerController.IsHanging)
        {
            if (Input.GetButton("Jump") && !playerController.InAction)
            {
                if (envScanner.ClimbLedgeCheck(transform.forward, out RaycastHit ledgeHit))
                {
                    currentPoint = GetNearestClimbPoint(ledgeHit.transform, ledgeHit.point);

                    playerController.SetControl(false);
                    StartCoroutine(JumpToLedge("Idle to hang", currentPoint.transform, 0.40f, 0.58f));
                }
            }

            if(Input.GetButton("Drop") && !playerController.InAction)
            {
                if(envScanner.DropLedgeCheck(out RaycastHit ledgeHit))
                {
                    currentPoint = GetNearestClimbPoint(ledgeHit.transform, ledgeHit.point);

                    playerController.SetControl(false);
                    StartCoroutine(JumpToLedge("DropHang", currentPoint.transform, 0.30f, 0.45f, handOffset: new Vector3(0.25f, 0f, -0.15f)));
                }
            }
        }
        else
        {
            if(Input.GetButton("Drop") && !playerController.InAction)
            {
                StartCoroutine(JumpFromHang());
                return;
            }

            float h = Mathf.Round(Input.GetAxisRaw("Horizontal"));
            float v = Mathf.Round(Input.GetAxisRaw("Vertical"));
            Vector2 inputDir = new Vector2(h, v);

            if (playerController.InAction || inputDir == Vector2.zero) return;

            if (currentPoint.MountPoint && inputDir.y == 1)
            {
                StartCoroutine(MountFromHang());
                return;
            }

            Neighbour neighbour = currentPoint.GetNeighbour(inputDir);
            if (neighbour == null) return;

            if(neighbour.connectionType == ConnectionType.JUMP && Input.GetButton("Jump"))
            {
                currentPoint = neighbour.point;

                if (neighbour.direction.y == 1)
                    StartCoroutine(JumpToLedge("Hang up", currentPoint.transform, 0.35f, 0.65f));
                else if(neighbour.direction.y == -1)
                    StartCoroutine(JumpToLedge("Hang drop", currentPoint.transform, 0.31f, 0.65f));
                else if (neighbour.direction.x == 1)
                    StartCoroutine(JumpToLedge("Hang right", currentPoint.transform, 0.20f, 0.50f, handOffset: new Vector3(0.25f, -0.1f, 0.25f)));
                else if (neighbour.direction.x == -1)
                    StartCoroutine(JumpToLedge("Hang left", currentPoint.transform, 0.20f, 0.50f, handOffset: new Vector3(0.25f, -0.1f, 0.25f)));
            }
            else if(neighbour.connectionType == ConnectionType.MOVE)
            {
                currentPoint = neighbour.point;

                if(neighbour.direction.x == 1)
                   StartCoroutine(JumpToLedge("Braced shimmy right", currentPoint.transform, 0f, 0.38f, handOffset: new Vector3(0.25f, 0.02f, 0.2f)));
                else if (neighbour.direction.x == -1)
                    StartCoroutine(JumpToLedge("Braced shimmy left", currentPoint.transform, 0f, 0.38f, AvatarTarget.LeftHand ,handOffset: new Vector3(0.25f, 0.02f, 0.2f)));
            }
        }
    }

    IEnumerator JumpToLedge(string anim, Transform ledge, float matchStartTime, float matchTargetTime, 
        AvatarTarget hand = AvatarTarget.RightHand, Vector3? handOffset = null)
    {
        MatchTargetParams matchParams = new MatchTargetParams()
        {
            pos = GetHandPos(ledge, hand, handOffset),
            bodyPart = hand,
            startTime = matchStartTime,
            targetTime = matchTargetTime,
            posWeight = Vector3.one
        };

        Quaternion targetRot = Quaternion.LookRotation(-ledge.forward);

        yield return playerController.DoAction(anim, matchParams, targetRot, true);

        playerController.IsHanging = true;
    }

    Vector3 GetHandPos(Transform ledge, AvatarTarget hand, Vector3? handOffset)
    {
        Vector3 offsetValue = (handOffset != null) ? handOffset.Value : new Vector3(0.25f, 0f, 0.25f);

        Vector3 hDir = hand == AvatarTarget.RightHand ? ledge.right : -ledge.right;
        return ledge.position + ledge.forward * offsetValue.z + Vector3.up * offsetValue.y  - hDir * offsetValue.x;
    }

    IEnumerator JumpFromHang()
    {
        playerController.IsHanging = false;
        yield return StartCoroutine(playerController.DoAction("Jump from wall"));

        playerController.ResTargetRotation();
        playerController.SetControl(true);
    }

    IEnumerator MountFromHang()
    {
        playerController.IsHanging = false;
        yield return playerController.DoAction("ClimbFromHang");

        playerController.EnableCharacterController(true);

        yield return new WaitForSeconds(0.5f);

        playerController.ResTargetRotation();
        playerController.SetControl(true);
    }

    ClimbPoint GetNearestClimbPoint(Transform ledge, Vector3 hitPoint)
    {
        ClimbPoint[] points = ledge.GetComponentsInChildren<ClimbPoint>();

        ClimbPoint nearestPoint = null;
        float nearestPointDistance = Mathf.Infinity;

        foreach(ClimbPoint point in points)
        {
            float distance = Vector3.Distance(point.transform.position, hitPoint);

            if (distance < nearestPointDistance)
            {
                nearestPoint = point;
                nearestPointDistance = distance;
            }
        }

        return nearestPoint;
    }

}
