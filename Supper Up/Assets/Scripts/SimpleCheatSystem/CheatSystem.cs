using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 텔레포트 위치를 저장하는 클래스
[System.Serializable]
public class TeleportPoint
{
    public string name;     // "1telp", "2telp" 등
    public Vector3 position; // 위치 좌표
}

public class CheatSystem : MonoBehaviour
{
    public static CheatSystem instance { get; private set; }

    [Header("UI 레퍼런스")]
    public GameObject cheatPanel; // 치트 UI 패널
    public TMP_InputField commandInput; // 명령어 입력창
    public TextMeshProUGUI outputText; // 출력 텍스트

    [Header("플레이어 참조")]
    public Transform playerTransform; // 플레이어 Transform
    public Rigidbody playerRigidbody; // 플레이어 Rigidbody

    public Transform endingPos;  //엔딩 시연 지점

    // 인스펙터에서 설정 가능한 텔레포트 위치 리스트
    public List<TeleportPoint> teleportPoints;

    // 내부 딕셔너리 변환용
    private Dictionary<string, Vector3> teleportPositionDict = new Dictionary<string, Vector3>();

    // 저장 위치
    private Vector3 savePos;

    private Vector3 startPosition; // 시작 위치 저장
    private bool isPanelActive = false; // 치트창 활성화 여부
    private bool isFlying = false; // 비행 모드 여부
    private bool isSpeedUp = false;
    private bool isSaveCheat = false;

    private PlayerController playerController;
    private PlayerStateMachine playerSM;
    private float cheatMoveSpeed = 10f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            // 시작 위치 저장
            if (playerTransform != null)
                startPosition = playerTransform.position;

            // 리스트를 딕셔너리로 변환
            foreach (var tp in teleportPoints)
            {
                teleportPositionDict[tp.name.ToLower()] = tp.position;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (cheatPanel != null)
            cheatPanel.SetActive(false);
        Log("치트 시스템 준비 완료. F1 키로 열기");

        playerController = playerTransform.gameObject.GetComponent<PlayerController>();
        playerSM = playerTransform.gameObject.GetComponent<PlayerStateMachine>();
    }

    private void Update()
    {
        // F1 키로 치트 패널 토글
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleCheatPanel();
        }

        // 치트창 열려 있고 엔터 누르면 명령 실행
        if (isPanelActive && Input.GetKeyDown(KeyCode.Return))
        {
            ExecuteCommand();
            ToggleCheatPanel();
        }

        // 비행 모드일 때
        if (isFlying)
        {
            FlyMode();
        }

        if (isSaveCheat)
        {
            Save();
        }
    }

    private void ToggleCheatPanel()
    {
        isPanelActive = !isPanelActive;
        playerSM.enabled = !isPanelActive;
        if (cheatPanel != null)
        {
            cheatPanel.SetActive(isPanelActive);
            if (isPanelActive && commandInput != null)
            {
                commandInput.ActivateInputField();
                ClearOutputText();
            }
        }
    }

    public void ExecuteCommand()
    {
        string cmd = commandInput.text.Trim().ToLower();
        if (string.IsNullOrEmpty(cmd))
            return;

        Log("> " + cmd);

        if (cmd.StartsWith("fly"))
        {
            ToggleFly();
        }
        else if (cmd.StartsWith("home"))
        {
            ReturnHome();
        }
        else if (teleportPoints.Exists(tp => tp.name.ToLower() == cmd))
        {
            TeleportToPosition(cmd);
        }
        else if (cmd.StartsWith("help"))
        {
            ShowHelp();
        }
        else if (cmd.StartsWith("speedup"))
        {
            ToggleSpeedUp();
        }
        else if (cmd.StartsWith("save"))
        {
            ToggleSaveCheat();
        }
        else if(cmd.StartsWith("reset"))
        {
            ResetAll();
        }
        else if(cmd.StartsWith("end"))
        {
            EndingCheat();
        }
        else
        {
            Log("알 수 없는 명령어");
        }

        commandInput.text = "";
        commandInput.ActivateInputField();
    }

    void ToggleFly()
    {
        if (playerRigidbody != null)
        {
            isFlying = !isFlying;

            playerController.enabled = !isFlying;
            playerRigidbody.useGravity = !isFlying;
            string flyStatus = isFlying ? "시작" : "종료";
            Log($"플레이어 비행 모드 {flyStatus}");
        }
        else
        {
            Log("플레이어 Rigidbody를 찾을 수 없음", true);
        }
    }

    private void ToggleSpeedUp()
    {
        if (playerController != null)
        {
            isSpeedUp = !isSpeedUp;
            SpeedUp();

            string speedStatus = isSpeedUp ? "시작" : "종료";
            Log($"플레이어 속도 상승 모드 {speedStatus}");
        }
        else
        {
            Log("PlayerController를 찾을 수 없음", true);
        }
    }

    private void SpeedUp()
    {
        if (isSpeedUp)
        {
            cheatMoveSpeed = 30f;
        }
        else
        {
            cheatMoveSpeed = 10f;
        }
    }

    private void ToggleSaveCheat()
    {
        if (playerController != null)
        {
            isSaveCheat = !isSaveCheat;

            string _isSaveCheat = isSaveCheat ? "시작" : "종료";
            Log($"플레이어 저장 모드 {_isSaveCheat}");
        }
        else
        {
            Log("PlayerController를 찾을 수 없음", true);
        }
    }

    private void Save()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (playerController.transform.position.y < savePos.y) return;
            else Log("해당위치로 저장할 수 없습니다.", true);

            if (playerController.IsGrounded())
            {
                savePos = playerController.transform.position;
                Log($"위치가 저장되었습니다.");
            }
            else
            {
                Log("저장되지 않았습니다", true);
            }
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (playerController.transform.position.y > savePos.y) return;
            else Log("저장된 위치로 이동할 수 없습니다.", true);

            playerController.transform.position = savePos;
            Log($"위치이동에 성공하셨습니다.");

        }
    }

    private void EndingCheat()
    {
        GameManager.Instance.EndingCheat(1);
        StoryManager.Instance.StartNarration(13, true);

        if (playerController != null)
        {
            playerController.transform.position = endingPos.position;
        }

    }

    void FlyMode()
    {
        // 카메라 또는 플레이어의 앞 방향 벡터
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        // 수평 평면으로만 계산 (Y 성분 제거)
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // 움직임 벡터 초기화
        Vector3 moveDirection = Vector3.zero;

        // 위로 상승
        if (Input.GetKey(KeyCode.Space))
        {
            playerTransform.position += Vector3.up * Time.deltaTime * cheatMoveSpeed; // 상승 속도
        }
        // 아래로 내려가기
        if (Input.GetKey(KeyCode.LeftShift))
        {
            playerTransform.position += Vector3.down * Time.deltaTime * cheatMoveSpeed; // 하강 속도
        }

        // WASD 방향 이동
        if (Input.GetKey(KeyCode.W))
            moveDirection += forward;
        if (Input.GetKey(KeyCode.S))
            moveDirection -= forward;
        if (Input.GetKey(KeyCode.A))
            moveDirection -= right;
        if (Input.GetKey(KeyCode.D))
            moveDirection += right;

        // 방향 정규화하여 일정한 속도 유지
        if (moveDirection != Vector3.zero)
            moveDirection.Normalize();

        // 최종 이동
        playerTransform.position += moveDirection * cheatMoveSpeed * Time.deltaTime;
    }


    void ReturnHome()
    {
        if (playerTransform != null)
        {
            playerTransform.position = startPosition;
            Log("처음 위치로 돌아감");
        }
        else
        {
            Log("플레이어 참조 없음", true);
        }
    }

    void TeleportToPosition(string command)
    {
        string key = command.ToLower();
        if (teleportPositionDict.TryGetValue(key, out Vector3 targetPosition))
        {
            playerTransform.position = targetPosition;
            Log($"플레이어가 {key} 위치로 이동했습니다");
        }
        else
        {
            Log("이동할 위치를 찾을 수 없음", true);
        }
    }

    void ShowHelp()
    {
        Log("사용 가능한 명령어:");
        Log("fly - 비행 모드 토글");
        Log("home - 처음 위치로 이동");
        Log("speedUp - 속도 증가 토글");
        Log("save - 임시 저장 토글");
        Log("reset - 모든 치트설정 초기화");
        Log("end - 엔딩 1 설정 및 엔딩구간으로 이동");
        foreach (var tp in teleportPoints)
        {
            Log($"{tp.name} - 해당 위치로 이동");
        }
        Log("help - 도움말 표시");
    }

    private void Log(string message, bool isError = false)
    {
        string msg = isError ? $"<color=red>{message}</color>" : message;
        if (outputText != null)
        {
            outputText.text += msg + "\n";
        }
        else
        {
            Debug.Log(message);
        }
    }


    private void ResetAll()
    {
        if (isFlying) ToggleFly();
        if (isSpeedUp) ToggleSpeedUp();
        if (isSaveCheat) ToggleSaveCheat();
        //속도 상승
        //스토리 스킵
    }

    private void ClearOutputText()
    {
        if (outputText != null)
        {
            outputText.text = ""; // 텍스트 비우기
        }
    }
}
