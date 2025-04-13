using System.Collections;
using DG.Tweening;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PlayerController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera thirdPersonCamera;
    //���� ������
    private Vector3 movement = Vector3.zero;

    //�÷��̾��� ������ �ӵ��� �����ϴ� ����
    [Header("Player Movement")]
    public float moveSpeed = 5.0f;
    public float jumpForce = 5.0f;
    public float rotationSpeed = 10f;     //ȸ���ӵ�
    private float currentSpeed = 0;
    private float currentRotateSpeed = 0;

    [Header("Player Rotation")]
    public float velocity = 1;
    public float max_velocity = 3f;
    private float min_velocity = 1f;
    private float speedTimer = 0;
    //���ڸ� ȸ������
    private float rotateTimer = 0;
    public float rotateTime = 3f;
    public float rotateDegree = 30;
    Quaternion toRoation = Quaternion.identity;
    private float moveDegree = 0;
    public bool onRotate = false;

    [Header("Veriable for Ground Check")]
    public bool isFalling = false;
    public bool isJumping = false;
    private bool wasGrounded = false;
    public bool isLanding = false;
    public bool startLanding = false;
    public LayerMask groundLayer;

    [Header("Wall Climbing Setting")]
    public float heightValue = 1f;
    public float frontValue = 0.3f;
    public Vector3 boxHalfExtents = Vector3.zero;
    public LayerMask wallLayer;
    public bool isClimbing = false;
    public Vector3 climbOffset;
    private Vector3 targetHandPos;
    private Vector3 targetPos;
    private float timer = 0f;


    //���� ������
    private Rigidbody rb;
    private Animator playerAnimator;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;          //���콺 Ŀ���� ��װ� �����

        rb = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
        currentSpeed = moveSpeed;
        currentRotateSpeed = rotationSpeed;
    }

    void Update()
    {
        Landing();
        Climbing();
    }

    //�÷��̾� �ൿó�� �Լ�
    public void Move()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");         //�¿� �Է�(1, -1)
        float moveVertical = Input.GetAxis("Vertical");             //�յ� �Է�(1, -1)

        ConstraintsMove();

        AdjustSpeed(moveVertical);
        //�ִϸ��̼�
        playerAnimator.SetFloat("FMove", moveVertical * velocity);
        playerAnimator.SetFloat("RMove", moveHorizontal);

        //�̵� ���� ���
        if (!onRotate) movement = (transform.forward * moveVertical + transform.right * moveHorizontal).normalized;
        else movement = transform.forward * moveVertical;
        moveDegree = movement.magnitude;

        rb.MovePosition(rb.position + movement * (moveSpeed + velocity) * Time.deltaTime);

        // �߰��� �̲����� ȿ�� �ڵ�
        // �̲����� ȿ�� �߰�
        if (IsOnIce()) // ���� ���� ���� ���
        {
            Vector3 slipForce = movement * (moveSpeed * 50f) * Time.deltaTime; // moveSpeed�� ����Ͽ� �̲������� ���� �߰�
            rb.AddForce(slipForce, ForceMode.Acceleration);
        }

        RotateDiagonal(moveHorizontal, moveVertical, movement);
    }

    // �÷��̾ ���Ǳ濡 �ִ��� Ȯ���ϴ� �޼��� �߰�
    private bool IsOnIce()
    {
        // Raycast�� ����Ͽ� �Ʒ��� �ִ� ������Ʈ�� ���̾ Ȯ��
        return Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer);
    }


    public void ConstraintsMove() //���߿� ���� ��, �̼Ӱ���.
    {
        if (CheckDistance() > 1.1f)
        {
            moveSpeed = 0.1f;
            velocity = min_velocity;
            speedTimer = 0;
        }
        else moveSpeed = currentSpeed;
    }
    //����, ����, �������� ���� �̵��� �ʱ�ȭ
    public void ResetVelocity()
    {
        velocity = min_velocity;
        speedTimer = 0;
    }

    //�ð��� �帧�� ���� �ӵ��� �÷��ִ� �ڵ�
    private void AdjustSpeed(float moveVertical)
    {
        if (Mathf.Abs(moveVertical) > 0.95f)
        {
            speedTimer = Mathf.Min(speedTimer += Time.deltaTime * 1.2f, max_velocity);
        }
        else
        {
            speedTimer = Mathf.Max(speedTimer -= Time.deltaTime * 2f, 0);
        }
        velocity = Mathf.Clamp(speedTimer, min_velocity, max_velocity);
    }

    public void Rotate(bool OnRotateAni)
    {
        Vector3 cameraForward = thirdPersonCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        if (moveDegree > 0.1 && !onRotate)                                      //ī�޶� ���� ĳ����ȸ��
        {
            toRoation = Quaternion.LookRotation(cameraForward, Vector2.up);
            rotationSpeed = currentRotateSpeed;
        }

        float dot = Vector3.Dot(transform.forward, cameraForward);               //ȸ��ó���� �� �����ϱ�
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;

        if (angle >= rotateDegree && moveDegree < 0.1)                           //n���Ŀ� ī�޶�������� ȸ��
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

    public void RotateDiagonal(float moveHorizontal, float moveVertical, Vector3 movement)
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
        rb.AddForce(Vector3.up * jumpForce + movement * velocity * 2.5f, ForceMode.Impulse);
    }

    public void SupperLanding()
    {
        float fallingSpeed = Mathf.Abs(rb.velocity.y);
        playerAnimator.SetFloat("LandSpeed", Mathf.Clamp(fallingSpeed / 10, 0.8f, 2));
        isFalling = false;
    }
   public void Landing()
   {

        if (IsGrounded() && !wasGrounded && isJumping)
        {
            //Debug.Log("�ȴ�");
            isLanding = true;
            isJumping = false;
        }
        if (isLanding)
        {
            AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Jumping Down") && stateInfo.normalizedTime > 0.3)
            {
                isLanding = false;
            }
            else if(stateInfo.IsName("Movement")) isLanding = false;
        }

        wasGrounded = IsGrounded();
   }

    public void Climbing()
    {
        Vector3 origin = transform.position + transform.up * heightValue + transform.forward * frontValue;
        Collider[] target = Physics.OverlapBox(origin, boxHalfExtents, transform.rotation, wallLayer);

        float climbHeight = origin.y + boxHalfExtents.y;
        if (target.Length >= 1)
        {
            //Debug.Log($"{target[0].transform.position.y}, {target[0].transform.localScale.y / 2}");
            float height = target[0].transform.position.y + target[0].transform.localScale.y / 2;
            if (height <= climbHeight && !isClimbing)
            {
                //isClimbing = true;
                Vector3 vel = rb.velocity;
                vel.y = 0;
                rb.velocity = vel;
                //playerAnimator.applyRootMotion = true;
                //GetComponent<Collider>().isTrigger = true;

                Vector3 forward = transform.position + transform.forward * 0.5f;           //ĳ���Ͱ� �̵��� ��ġ
                targetPos = new Vector3(forward.x, height + 0.05f, forward.z);

                Vector3 Handforward = transform.position + transform.forward * 0.5f;       //ĳ������ ���� ��ġ�� ��
                targetHandPos = new Vector3(Handforward.x, height + 0.05f, Handforward.z);

                Vector3 temp = new Vector3(0, 1.43f, 0.31f);                               //���� ��ġ�� offset��ŭ�� �Ÿ��� �ǰ� �̵�
                Vector3 desiredPos = targetHandPos - transform.rotation * temp;

                //transform.position = Vector3.Lerp(transform.position, desiredPos, timer += Time.deltaTime);

                Vector3 original = new Vector3(transform.position.x, height - 0.05f, transform.position.z);
                /*
                if (Physics.Raycast(original, transform.forward, out RaycastHit hit, 2))
                {
                    toRoation = Quaternion.LookRotation(-hit.normal, Vector2.up);
                    rotationSpeed = currentRotateSpeed;

                    if (Vector3.Angle(transform.forward, -hit.normal) < 0.3f)
                    {
                        transform.position = desiredPos;
                        isClimbing = true;
                        playerAnimator.applyRootMotion = true;
                        GetComponent<Collider>().isTrigger = true;
                    }
                }*/
            }
        }
        if (isClimbing)
        { 
            timer += Time.deltaTime;
            if (timer >= 2.4f)                                   //�ִϸ��̼��� ������ ��, �ʱ�ȭ
            {
                transform.position = targetPos;
                playerAnimator.applyRootMotion = false;
                timer = 0;
                Debug.Log("�ȴ�");
                GetComponent<Collider>().isTrigger = false;
                isClimbing = false;
            }
        }
    }

    public float CheckDistance()
    {
        RaycastHit hit;
        Vector3 temp = transform.position + Vector3.up * 1;
        if (Physics.Raycast(temp, Vector3.down, out hit)) return hit.distance;
        return 10f;
    }

    public bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float radius = 0.3f;
        return Physics.CheckSphere(origin, radius, groundLayer);
    }
    
    private void OnAnimatorIK(int layerIndex)
    {
        AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Climbing"))
        {
            Vector3 leftHandPos = targetHandPos - transform.right * 0.4f;
            Vector3 rightHandPos = targetHandPos + transform.right * 0.4f;

            playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
            playerAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
            playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);

            playerAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPos);
            playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPos);
        }
        else
        {
            playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            playerAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }

    }

    private void OnDrawGizmos()
    {
        //�÷��̾� �ٴ�üũ��
        Vector3 start = transform.position + Vector3.up * 0.1f;
        float radius = 0.3f;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(start, radius);

        //�÷��̾� ��üũ��

        Gizmos.matrix = Matrix4x4.TRS(transform.position + transform.up * heightValue + transform.forward * frontValue, transform.rotation, Vector3.one);

        Vector3 origin = transform.position + transform.up * heightValue + transform.forward * frontValue;
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2);
    }

}
