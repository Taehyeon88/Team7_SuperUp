using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UiManager : MonoBehaviour
{
    public static UiManager instance;

    [Header("Title UI")]
    //Tile씬 변수
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button ExitButton;

    [Header("Pause UI")]
    //Pause씬 변수
    [SerializeField] private Button reGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingButton2;
    [SerializeField] private Button exitGameButton;

    [Header("Setting UI")]
    [SerializeField] private GameObject settingCanvas;

    [Header("Ending UI")]
    [SerializeField] private Button rePlayButton;
    [SerializeField] private Button exitGameButton2;
    [SerializeField] private GameObject endingCanvas;
    //내부변수들
    private Scene currentScene;
    private GameObject pauseCanvas;

    //한번만 실행
    public bool isPlayGame = false;

    //Pause씬 활성화전달변수
    [HideInInspector] public bool onPause = false;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }

        currentScene = SceneManager.GetActiveScene();
        Initialize_T();
        SetButtonDeligate_T();
    }

    void Update()
    {
        CheckPauseCondition();

        //사진촬영용
        //if (Input.GetKeyDown(KeyCode.V))
        //{
        //    Time.timeScale = 0f;
        //}
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)  //초기화함수
    {
        currentScene = scene;

        Time.timeScale = 1f;  //일정지 후 변수 초기화
        onPause = false;

        if (scene.name == "TitleScene") Initialize_T();
        else if (scene.name == "ProtoTypeScene")
        {
            Initialize_P();
        }
    }

    private void Initialize_T()     //타이틀 씬 초기화 함수
    {
        playButton = GameObject.Find("PlayButton").GetComponent<Button>();
        settingButton = GameObject.Find("SettingButton").GetComponent<Button>();
        ExitButton = GameObject.Find("ExitButton").GetComponent<Button>();

        settingCanvas = GameObject.Find("SettingCanvas");

        if (settingCanvas != null)
        {
            settingCanvas.SetActive(false);
        }
        SetButtonDeligate_T();

        isPlayGame = false;  //게임시작 준비
    }
    private void Initialize_P()   //게임 플레이 씬 초기화 함수
    {
        //Debug.Log("초기화 된다");

        pauseCanvas = GameObject.Find("PauseCanvas");
        reGameButton = GameObject.Find("ReGameButton").GetComponent<Button>();
        continueButton = GameObject.Find("ContinueButton").GetComponent<Button>();
        settingButton2 = GameObject.Find("SettingButton").GetComponent<Button>();
        exitGameButton = GameObject.Find("ExitGameButton").GetComponent<Button>();

        rePlayButton = GameObject.Find("RePlayButton").GetComponent<Button>();          //엔딩용
        exitGameButton2 = GameObject.Find("ExitGameButton2").GetComponent<Button>();
        endingCanvas = GameObject.Find("EndingCanvas");

        reGameButton.onClick.RemoveAllListeners();
        continueButton.onClick.RemoveAllListeners();
        settingButton2.onClick.RemoveAllListeners();
        exitGameButton.onClick.RemoveAllListeners();
        rePlayButton.onClick.RemoveAllListeners();                
        exitGameButton2.onClick.RemoveAllListeners();


        reGameButton.onClick.AddListener(StartPlay);
        continueButton.onClick.AddListener(ContinueGame);
        settingButton2.onClick.AddListener(OnSettingMenu);
        exitGameButton.onClick.AddListener(GoToTitle);
        pauseCanvas.SetActive(false);

        settingCanvas = GameObject.Find("SettingCanvas");
        settingCanvas.SetActive(false);

        rePlayButton.onClick.AddListener(StartPlay);                 //엔딩용
        exitGameButton2.onClick.AddListener(GoToTitle);
        endingCanvas.SetActive(false);
    }


    private void SetButtonDeligate_T()  //Ui 버튼 클릭 델리게이트 등록함수
    {
        if (currentScene.name == "TitleScene")
        {
            playButton.onClick.AddListener(StartPlay);
            settingButton.onClick.AddListener(OnSettingMenu);
            ExitButton.onClick.AddListener(Exit);
        }
    }

    private void StartPlay()  //게임 시작함수
    {
        if (!isPlayGame)
        {
            isPlayGame = true;
            SceneManager.LoadScene("ProtoTypeScene");
        }
    }

    private void OnSettingMenu()  //세팅 시작함수
    {
        settingCanvas.SetActive(true);
    }

    private void Exit()   //게임종료함수
    {
        Application.Quit();
    }

    private void CheckPauseCondition()
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.isGameEnd) return;  //게임 종료시 일시정지 금지
        }

        if (currentScene.name == "ProtoTypeScene")
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!onPause) PauseGame();
                else ContinueGame();
            }
        }
    }

    private void PauseGame()   //게임 일시정지 함수
    {
        isPlayGame = false;    //게임 재시작용 준비

        Cursor.lockState = CursorLockMode.None;

        SoundManager.instance.PauseAllSounds();
        pauseCanvas.SetActive(true);
        onPause = true;
        Time.timeScale = 0f;
    }

    private void ContinueGame()   //게임 계속하기 함수
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        SoundManager.instance.RePlayAllSounds();
        pauseCanvas.SetActive(false);
        onPause = false;
    }

    private void GoToTitle()   //타이틀화면으로 이동함수
    {
        SceneManager.LoadScene("TitleScene");
    }
}
