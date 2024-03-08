using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkourController : MonoBehaviour
{
    [SerializeField] List<ParkourAction> parkourActions;
    [SerializeField] ParkourAction jumpDownAction;
    [SerializeField] private float autoDropHeightLimit = 2;

    [SerializeField] private EnvironmentScanner envScanner;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator anim;

    private void Update()
    {
        ObstacleHitData hitData = envScanner.ObstacleCheck();

        if (!playerController.InAction && !playerController.IsHanging && Input.GetButton("Jump"))
        {
            
            if (hitData.forwardHitFound)
            {
                foreach (ParkourAction action in parkourActions)
                {
                    if(action.CheckIfPossible(hitData, transform))
                    {
                        StartCoroutine(DoParkourAction(action));
                        break;
                    }
                }
            }
        }

        if(playerController.IsOnLedge && !playerController.InAction && !hitData.forwardHitFound)
        {
            bool shouldJump = true;
            if (playerController.LedgeData.height > autoDropHeightLimit && !Input.GetButton("Jump"))
                shouldJump = false;

            if(shouldJump && playerController.LedgeData.angle <= 50)
            {
                playerController.IsOnLedge = false;
                StartCoroutine(DoParkourAction(jumpDownAction));
            }
        } 
    }

    IEnumerator DoParkourAction(ParkourAction action)
    {
        playerController.SetControl(false);

        MatchTargetParams matchParam = null;
        if(action.EnableTargetMatching)
        {
            matchParam = new MatchTargetParams()
            {
                pos = action.MatchPos,
                bodyPart = action.MatchBodyPart,
                posWeight = action.MatchPosWeigth,
                startTime = action.MatchStartTime,
                targetTime = action.MatchTargetTime
            };
        }

        yield return playerController.DoAction(action.AnimName, matchParam, action.TargetRotation, action.RotateToObstacle, action.PostActionDelay, action.Mirror);

        playerController.SetControl(true);
    }

}
