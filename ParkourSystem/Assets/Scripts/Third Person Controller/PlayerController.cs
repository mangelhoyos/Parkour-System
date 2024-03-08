using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Move settings")]
    [SerializeField] float moveSpeed = 5;
    [SerializeField] float rotationSpeed = 500f;

    [Header("Ground check settings")]
    [SerializeField] float groundCheckRadius = 0.2f;
    [SerializeField] Vector3 groundCheckOffset;
    [SerializeField] LayerMask groundCheckLayerMask;

    bool isGrounded;
    bool hasControl = true;

    public bool InAction { get; private set; }
    public bool IsHanging { get; set; }

    Vector3 desiredMoveDir;
    Vector3 moveDir;
    Vector3 velocity;

    public bool IsOnLedge { get; set; }
    public LedgeData LedgeData { get; set; }

    float ySpeed;
    Quaternion targetRotation;

    [Header("Cash variables")]
    [SerializeField] CameraController cameraController;
    [SerializeField] Animator anim;
    [SerializeField] CharacterController characterController;
    [SerializeField] EnvironmentScanner envScanner;

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 moveInput = new Vector3(h, 0, v).normalized;

        float moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));

        desiredMoveDir = cameraController.PlanarRotation * moveInput;
        moveDir = desiredMoveDir;

        if (!hasControl)
            return;

        if (IsHanging)
            return;

        velocity = Vector3.zero;

        GroundCheck();
        anim.SetBool("isGrounded", isGrounded);
        if(isGrounded)
        {
            ySpeed = -0.5f;
            velocity = desiredMoveDir * moveSpeed;

            IsOnLedge = envScanner.ObstacleLedgeCheck(desiredMoveDir, out LedgeData ledgeData);
            if(IsOnLedge)
            {
                LedgeData = ledgeData;
                LedgeMovement();
            }

            anim.SetFloat("moveAmount", velocity.magnitude / moveSpeed, 0.2f, Time.deltaTime);
        }
        else
        {
            ySpeed += Physics.gravity.y * Time.deltaTime;

            velocity = transform.forward * moveSpeed / 2;
        }

        
        velocity.y = ySpeed;

        characterController.Move(velocity * Time.deltaTime);
        

        if (moveAmount > 0 && moveDir.magnitude > .2f)
        {
            targetRotation = Quaternion.LookRotation(moveDir);
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        
    }

    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius, groundCheckLayerMask);
    }

    void LedgeMovement()
    {
        float signedAngle = Vector3.SignedAngle(LedgeData.surfaceHit.normal, desiredMoveDir, Vector3.up);
        float angle = Mathf.Abs(signedAngle);

        if(Vector3.Angle(desiredMoveDir, transform.forward) >= 80)
        {
            //Dont move but rotate
            velocity = Vector3.zero;
            return;
        }

        if(angle < 40)
        {
            velocity = Vector3.zero;
            moveDir = Vector3.zero;
        }
        else if(angle < 90)
        {

            Vector3 left = Vector3.Cross(Vector3.up, LedgeData.surfaceHit.normal);
            Vector3 dir = left * Mathf.Sign(signedAngle);

            velocity = velocity.magnitude * dir;
            moveDir = dir;
        }
    }

    public IEnumerator DoAction(string animName, MatchTargetParams matchParams = null ,Quaternion targetRotation = new Quaternion(), bool rotate = false,
        float postDelay = 0f, bool mirror = false)
    {
        InAction = true;

        anim.SetBool("mirrorAction", mirror);
        anim.CrossFadeInFixedTime(animName, 0.2f);

        yield return null;

        AnimatorStateInfo stateInfo = anim.GetNextAnimatorStateInfo(0);
        if (!stateInfo.IsName(animName))
        {
            Debug.LogError("The parkour animation name is wrong!");
        }

        float rotateStartTime = (matchParams != null) ? matchParams.startTime : 0f;

        float timer = 0;
        while (timer < stateInfo.length)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / stateInfo.length;

            if (rotate && normalizedTime > rotateStartTime)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);

            if (matchParams != null)
                MatchTarget(matchParams);

            if (anim.IsInTransition(0) && timer > .5f)
                break;

            yield return null;
        }

        yield return new WaitForSeconds(postDelay);

        InAction = false;
    }

    void MatchTarget(MatchTargetParams mp)
    {
        Debug.DrawRay(mp.pos, Vector3.up * 2, Color.magenta, 5f);

        if (anim.isMatchingTarget)
            return;

        anim.MatchTarget(mp.pos, transform.rotation, mp.bodyPart,
            new MatchTargetWeightMask(mp.posWeight, 0), mp.startTime, mp.targetTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius);
    }

    public void SetControl(bool hasControl)
    {
        this.hasControl = hasControl;
        characterController.enabled = hasControl;

        if(!hasControl)
        {
            anim.SetFloat("moveAmount", 0f);
            targetRotation = transform.rotation;
        }
    }

    public void EnableCharacterController(bool enabled)
    {
        characterController.enabled = enabled;
    }

    public void ResTargetRotation()
    {
        targetRotation = transform.rotation;
    }

    public bool HasControl
    {
        get => hasControl;
        set => hasControl = value;
    }
    public float RotationSpeed => rotationSpeed;
}

public class MatchTargetParams
{
    public Vector3 pos;
    public AvatarTarget bodyPart;
    public Vector3 posWeight;
    public float startTime;
    public float targetTime;
}