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
    public LayerMask groundLayer;

    [Header("Wall Climbing Setting")]
    public float heightValue = 1f;
    public float frontValue = 0.3f;
    public Vector3 boxHalfExtents = Vector3.zero;
    public LayerMask wallLayer;
    public float climbSpeed = 1f;
    public bool isClimbing = false;
    private Vector3 TargetPos;
    private float timer = 0;
    public Vector3 climbOffset;
    private Vector3 targetHandPos;

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

        RotateDiagonal(moveHorizontal, moveVertical, movement);
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

        if (moveDegree > 0.1 && !onRotate)                               //ī�޶� ���� ĳ����ȸ��
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
            isLanding = true;
            isJumping = false;
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
            Debug.Log($"{target[0].transform.position.y}, {target[0].transform.localScale.y / 2}");
            float height = target[0].transform.position.y + target[0].transform.localScale.y / 2;
            if (height <= climbHeight && !isClimbing)
            {
                isClimbing = true;
                Vector3 vel = rb.velocity;
                vel.y = 0;
                rb.velocity = vel;

                Vector3 forward = transform.position + transform.forward * 0.25f;
                TargetPos = new Vector3(transform.position.x, height + 0.1f, forward.z);
                rb.useGravity = false;
            }
        }
        if (isClimbing)
        {
            playerAnimator.SetFloat("ClimbSpeed", climbSpeed);

            timer += climbSpeed * Time.deltaTime;
            if (timer >= 1)
            {
                transform.position = TargetPos;
                timer = 0;
                Debug.Log("�ȴ�");
                rb.useGravity = true;
                isClimbing = false;
            }
        }
    }

    private Vector3 StartClimbPos()
    {
        return transform.position + transform.forward * climbOffset.z + transform.up * climbOffset.y + transform.right * climbOffset.x;
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
        float radius = 0.2f;
        return Physics.CheckSphere(origin, radius, groundLayer);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);

        playerAnimator.SetIKPosition(AvatarIKGoal.RightHand, targetHandPos);  //�����ʿ�
        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, targetHandPos);

    }

    private void OnDrawGizmos()
    {
        //�÷��̾� �ٴ�üũ��
        Vector3 start = transform.position + Vector3.up * 0.1f;
        float radius = 0.2f;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(start, radius);

        //�÷��̾� ��üũ��

        Gizmos.matrix = Matrix4x4.TRS(transform.position + transform.up * heightValue + transform.forward * frontValue, transform.rotation, Vector3.one);

        Vector3 origin = transform.position + transform.up * heightValue + transform.forward * frontValue;
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2);
    }

}
