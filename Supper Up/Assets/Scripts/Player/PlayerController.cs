using System.Collections;
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

    [Header("Ground Check Setting")]
    public float fallingThrexhold = -0.1f;            //�������°����� ������ ���� �ӵ� �Ӱ谪
    public float groundCheckDistance = 0.3f;
    public float slopedLimit = 45f;                  //��� ���� �ִ� ���
    public bool isFalling = false;
    public bool isJumping = false;
    private bool wasGrounded = false;
    public bool isLanding = false;
    public LayerMask groundLayer;

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
    }

    //�÷��̾� �ൿó�� �Լ�
    public void Move()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");         //�¿� �Է�(1, -1)
        float moveVertical = Input.GetAxis("Vertical");             //�յ� �Է�(1, -1)

        ConstraintsMove();

        Debug.Log($"{CheckDistance()}, {moveVertical}");

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

    private void OnDrawGizmos()
    {
        Vector3 cameraForward = thirdPersonCamera.transform.forward; //ī�޶� �� ����
        cameraForward.y = 0f;  //���� ���� ����
        cameraForward.Normalize();  //���� ���� ����ȭ(0~1) ������ ������ ������ش�.

        Gizmos.color = Color.yellow;
        Debug.DrawRay(transform.position, cameraForward);

        Gizmos.color = Color.red;
        Debug.DrawRay(transform.position + Vector3.up, movement * 2f);


        Vector3 origin = transform.position + Vector3.up * 0.5f;

        //-------------------------------------------------------------

        Vector3 start = transform.position + Vector3.up * 0.1f;
        float radius = 0.2f;

        // ���� ��
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(start, radius);
    }

}
