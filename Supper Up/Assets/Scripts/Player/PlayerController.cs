using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera thirdPersonCamera;
    //내부 변수들
    private Vector3 movement = Vector3.zero;

    //플레이어의 움직임 속도를 설정하는 변수
    [Header("Player Movement")]
    public float moveSpeed = 5.0f;
    public float jumpForce = 5.0f;
    public float jumpfrontForce = 2.5f;
    public float rotationSpeed = 10f;     //회전속도
    private float currentSpeed = 0;
    private float currentRotateSpeed = 0;

    [Header("Player Rotation")]
    public float velocity = 1;
    public float max_velocity = 3f;
    private float min_velocity = 1f;
    private float speedTimer = 0;
    //제자리 회전변수
    private float rotateTimer = 0;
    public float rotateTime = 3f;
    public float rotateDegree = 30;
    Quaternion toRoation = Quaternion.identity;
    private float moveDegree = 0;
    public bool onRotate = false;

    [Header("Veriable for Ground Check")]
    private bool wasGrounded = false;
    public float maxSlopeAngle = 30f;
    public Vector3 groundHalfExtents;
    public LayerMask groundLayer;

    [HideInInspector] public bool isFalling = false;
    [HideInInspector] public bool isJumping = false;
    [HideInInspector] public bool isLanding = false;
    [HideInInspector] public bool isHightLanding = false;
    [HideInInspector] public bool isSuperLanding = false;
    [HideInInspector] public bool isTrusted = false;
    [HideInInspector] public bool isOneTime = false;

    [HideInInspector] public float cheatSpeed = 1f;

    //내부 변수들
    private Rigidbody rb;
    private Animator playerAnimator;
    private RaycastHit slopeHit;
    private string currentPlane;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;          //마우스 커서를 잠그고 숨긴다

        rb = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
        currentSpeed = moveSpeed;
        currentRotateSpeed = rotationSpeed;
    }

    //플레이어 행동처리 함수
    public void Move(bool useMoveSound)
    {
        float moveHorizontal = Input.GetAxis("Horizontal");         //좌우 입력(1, -1)
        float moveVertical = Input.GetAxis("Vertical");             //앞뒤 입력(1, -1)

        float temp = Mathf.Max(Mathf.Abs(moveVertical), Mathf.Abs(moveHorizontal));

        ConstraintsMove();

        //이동 백터 계산
        if (!onRotate) movement = (transform.forward * moveVertical + transform.right * moveHorizontal).normalized * temp;  //대각이동시, 직진(예외처리)
        else movement = transform.forward * moveVertical;
        moveDegree = movement.magnitude;

        if (CheckHitWall(movement.normalized))                                                         //벽에 일정거리 이상일 경우, 이동멈추기(점프오류방지용)
        {
            //Debug.Log("벽에서 가깝다!");
            AdjustSpeed(moveVertical, true);
            movement = Vector3.zero;
        }
        else
        {
            AdjustSpeed(moveVertical, false);
            RotateDiagonal(moveHorizontal, moveVertical, movement);
        }
        
        playerAnimator.SetFloat("FMove", moveVertical * velocity);                          //애니메이션
        playerAnimator.SetFloat("RMove", moveHorizontal);

        if (SoundManager.instance != null)
        {
            if (!SoundManager.instance.CheckSoundPlay(GetGroundTypeString()) && movement.magnitude > 0.1f && useMoveSound)
            {
                if (currentPlane != GetGroundTypeString())  //바닥의 재질이 바뀔 경우 변경
                {
                    isOneTime = false;
                }

                if (!isOneTime)
                {
                    if (GetGroundTypeString() == "Walk(wood)")
                    {
                        SoundManager.instance.FadeSound("Walk(wood)", 1f);
                        SoundManager.instance.FadeSound("Walk(stone)", 0f);
                        SoundManager.instance.FadeSound("Walk(dirt)", 0f);
                    }
                    else if (GetGroundTypeString() == "Walk(stone)")
                    {
                        SoundManager.instance.FadeSound("Walk(stone)", 1f);
                        SoundManager.instance.FadeSound("Walk(wood)", 0f);
                        SoundManager.instance.FadeSound("Walk(dirt)", 0f);
                    }
                    else if(GetGroundTypeString() == "Walk(dirt)")
                    {
                        SoundManager.instance.FadeSound("Walk(dirt)", 1f);
                        SoundManager.instance.FadeSound("Walk(wood)", 0f);
                        SoundManager.instance.FadeSound("Walk(stone)", 0f);
                    }
                    currentPlane = GetGroundTypeString();
                    isOneTime = true;
                }
            }
            else if (movement.magnitude < 0.05f)
            {
                if (isOneTime)
                {
                    //SoundManager.instance.FadeSound(GetGroundTypeString(), 0f);
                    SoundManager.instance.FadeSound("Walk(dirt)", 0f);
                    SoundManager.instance.FadeSound("Walk(wood)", 0f);
                    SoundManager.instance.FadeSound("Walk(stone)", 0f);
                    isOneTime = false;
                }
            }
            float velocityforSound = Mathf.Max(1, velocity * 0.9f);
            SoundManager.instance.IncreasePitch(GetGroundTypeString(), velocityforSound);
        }

        bool isOnSlope = IsOnSlope();                                                       //경사이동용, 코드
        movement = isOnSlope ? AdjustDirectionToSlope(movement.normalized) : movement;

        rb.MovePosition(rb.position + movement * (moveSpeed * cheatSpeed + velocity) * Time.deltaTime);

    }

    private bool IsOnSlope()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        
        if (Physics.Raycast(origin, Vector3.down, out slopeHit, 0.5f, groundLayer))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle != 0 && angle < maxSlopeAngle; 
        }
        return false;
    }

    private Vector3 AdjustDirectionToSlope(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private void ConstraintsMove() //공중에 있을 시, 이속감소.
    {
        if (CheckDistance() > 1.1f && !IsGrounded())
        {
            moveSpeed = 0.1f;
            velocity = min_velocity;
            speedTimer = 0;
        }
        else moveSpeed = currentSpeed;
    }
    //파쿠르, 착지, 슈퍼착지 이후 이동값 초기화
    public void ResetVelocity()
    {
        velocity = min_velocity;
        speedTimer = 0;
        rb.velocity = new Vector3(0, rb.velocity.y, 0);
    }

    //시간이 흐름에 따라 속도를 올려주는 코드
    private void AdjustSpeed(float moveVertical, bool isDownVelocity)   //1번 매개변수 : 인풋이 없을 시, 속도감소 | 2번 매개변수 
    {
        if (isDownVelocity) speedTimer = Mathf.Max(speedTimer -= Time.deltaTime * 2f, 0);
        else
        {
            if (Mathf.Abs(moveVertical) > 0.95f)
            {
                speedTimer = Mathf.Min(speedTimer += Time.deltaTime * 1.2f, max_velocity);
            }
            else speedTimer = Mathf.Max(speedTimer -= Time.deltaTime * 2f, 0);
        }
        velocity = Mathf.Clamp(speedTimer, min_velocity, max_velocity);
    }

    public void Rotate(bool OnRotateAni)
    {
        Vector3 cameraForward = thirdPersonCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        if (moveDegree > 0.1 && !onRotate)                                      //카메라에 따라서 캐릭터회전
        {
            toRoation = Quaternion.LookRotation(cameraForward, Vector2.up);
            rotationSpeed = currentRotateSpeed;
        }

        float dot = Vector3.Dot(transform.forward, cameraForward);               //회전처리를 할 각구하기
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;

        if (angle >= rotateDegree && moveDegree < 0.1)                           //n초후에 카메라방향으로 회전
        {
            rotateTimer += Time.deltaTime;
            if (rotateTimer > rotateTime)
            {
                toRoation = Quaternion.LookRotation(cameraForward, Vector2.up);
                rotationSpeed = currentRotateSpeed / (angle / 90);
                rotateTimer = 0;
                Vector3 left = -transform.right;
                float temp = Vector3.Angle(cameraForward, left);

                if (OnRotateAni)
                {
                    if (temp <= 90) playerAnimator.SetTrigger("Lturn");
                    else if (temp <= 180) playerAnimator.SetTrigger("Rturn");
                }
            }
        }
        transform.rotation = Quaternion.Slerp(transform.rotation, toRoation, rotationSpeed * Time.deltaTime);
    }

    private void RotateDiagonal(float moveHorizontal, float moveVertical, Vector3 movement)
    {
        if (!onRotate)
        {
            if (velocity < 1.5 || Mathf.Abs(moveHorizontal) < 0.3f || Mathf.Abs(moveVertical) < 0.9f) return;
            Vector3 temp = movement;
            if (moveVertical < -0.9f) temp = -temp;
            toRoation = Quaternion.LookRotation(temp, Vector2.up);
            rotationSpeed = currentRotateSpeed;
            onRotate = true;
        }
        else
        {
            if (Mathf.Abs(moveHorizontal) > 0.9f && Mathf.Abs(moveVertical) > 0.3f) return;
            onRotate = false;
        }
    }

    public void Jumping()
    {
        rb.velocity = Vector3.up * jumpForce + movement * velocity * jumpfrontForce;
    }

    public void Landing(bool isSupper)
    {
        if (isSupper)
        {
            float fallingSpeed = Mathf.Abs(rb.velocity.y);
            playerAnimator.SetFloat("LandSpeed", Mathf.Clamp(fallingSpeed / 10, 0.8f, 2));
            isFalling = false;
        }
        else
        {
            float fallingSpeed = Mathf.Abs(rb.velocity.y);
            playerAnimator.SetFloat("LandSpeed", Mathf.Clamp(fallingSpeed / 15, 1f, 2));
        }
    }
   public void CheckLanding()
   {
        //Debug.Log("실시간 착지 여부: " + CheckDistance());
        if (isJumping)
        {
            if (IsGrounded() && !wasGrounded)  //점프후, 착지모션
            {
                isLanding = true;
            }
        }
        else if (CheckDistance() < 1.5f && !IsGrounded() && CheckFalling(1) && !isHightLanding)  //이동후, 높은 곳에서 착지모션  //2.2f
        {
            if (!isSuperLanding)
            {
                isHightLanding = true;
            }

            //Debug.Log("현재착지여부: " + CheckDistance());
            //Debug.Log("일반착지 활성화");
        }

        if (isLanding || isHightLanding)
        {
            AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Jumping Down") && stateInfo.normalizedTime > 0.3)
            {
                isLanding = false;
                isHightLanding = false;
            }
        }

        wasGrounded = IsGrounded();
   }

    public float CheckDistance()
    {
        RaycastHit hit;
        Vector3 temp = transform.position + Vector3.up * 1;
        if (Physics.SphereCast(temp, 0.3f, Vector3.down, out hit)) return hit.distance;
        //if (Physics.Raycast(temp, Vector3.down, out hit)) return hit.distance;
        return 10f;
    }

    public bool IsGrounded()
    {
        //Debug.Log("바닥여부체크: " + Physics.CheckBox(transform.position, groundHalfExtents, Quaternion.identity, groundLayer));
        return Physics.CheckBox(transform.position, groundHalfExtents, transform.rotation, groundLayer);
    }
    public string GetGroundTypeString()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit _hit, 10.5f, groundLayer))
        {
            if (_hit.collider.CompareTag("Wood")) return "Walk(wood)";
            else if (_hit.collider.CompareTag("Stone")) return "Walk(stone)";
            else if (_hit.collider.CompareTag("Dirt")) return "Walk(dirt)";
        }
        return "";
    }
    public bool CheckFalling(float value)
    {
        if (rb.velocity.y < - value) return true;
        return false;
    }

    public float GetVelocityMegitude()
    {
        return rb.velocity.magnitude;
    }

    private bool CheckHitWall(Vector3 movement)
    {
        float distance = 0.33f;

        //if (isJumping) distance = 0f;

        List<Vector3> rayPos = new List<Vector3>();
        rayPos.Add(transform.position + transform.up * 0.3f);
        rayPos.Add(transform.position + transform.up * 0.8f);
        rayPos.Add(transform.position + transform.up * 1.3f);
        rayPos.Add(transform.position + transform.up * 1.8f);

        foreach (var origin in rayPos)
        {
            Debug.DrawRay(origin, movement * distance, Color.red);
            if (Physics.Raycast(origin, movement, distance, groundLayer))
            {
                return true;
            }
        }
        return false;
    }

    //private void OnDrawGizmos()
    //{
    //    //플레이어 바닥체크용

    //    Gizmos.color = Color.green;
    //    Gizmos.DrawWireCube(transform.position, groundHalfExtents * 2);
    //    //Gizmos.DrawWireSphere(start, radius);
    //}

}
