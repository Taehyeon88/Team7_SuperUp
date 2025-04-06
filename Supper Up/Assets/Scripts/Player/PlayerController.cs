using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera thirdPersonCamera;      //3��Ī

    //�÷��̾��� ������ �ӵ��� �����ϴ� ����
    [Header("Player Movement")]
    public MovementController movementController;

    //���� ������
    private Vector3 movement = Vector3.zero;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;          //���콺 Ŀ���� ��װ� �����
        StartCoroutine(movementController.Falling());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) movementController.Jumping();

        movementController.Landing();

        movementController.Rotate();

        if (movementController.IsGrounded())
        {
            //Debug.Log("���̴�");
        }

        //Debug.Log(movementController.isJumping);
    }
    private void FixedUpdate()
    {
        HandleMovement();
    }

    //�÷��̾� �ൿó�� �Լ�
    public void HandleMovement()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");         //�¿� �Է�(1, -1)
        float moveVertical = Input.GetAxis("Vertical");             //�յ� �Է�(1, -1)

        movementController.Move(moveVertical, moveHorizontal, movement);

        //�̵� ���� ���
        if (!movementController.onRotate) movement = (transform.forward * moveVertical + transform.right * moveHorizontal).normalized;
        else movement = transform.forward * moveVertical;

        movementController.RotateDiagonal(moveHorizontal, moveVertical, movement);
    }

    private void OnDrawGizmos()
    {
        Vector3 cameraForward = thirdPersonCamera.transform.forward; //ī�޶� �� ����
        cameraForward.y = 0f;  //���� ���� ����
        cameraForward.Normalize();  //���� ���� ����ȭ(0~1) ������ ������ ������ش�.

        Gizmos.color = Color.yellow;
        Debug.DrawRay(transform.position, cameraForward);

        Gizmos.color = Color.red;
        Debug.DrawRay(transform.position, transform.forward);


        Vector3 origin = transform.position + Vector3.up * 0.5f;

        //-------------------------------------------------------------

        Vector3 start = transform.position + Vector3.up * 0.1f;
        float radius = 0.2f;

        // ���� ��
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(start, radius);
    }

}
