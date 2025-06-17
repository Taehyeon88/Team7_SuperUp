using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.ComponentModel;


public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance {  get; private set; }

    [Header("Story References")]
    [SerializeField] private StoryDatebaseSO storyDatabase;

    [Header("UI References")]
    [SerializeField] private GameObject narrationPanel;
    [SerializeField] private TextMeshProUGUI narrationText;
    [SerializeField] private List<TextMeshProUGUI> choiceTexts = new List<TextMeshProUGUI>();
    public List<GameObject> choiceObjects = new List<GameObject>();

    [Header("Narration Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private bool useTypewriterEffect = true;

    [Header("EndGame Settings")]
    [SerializeField] private GameObject endGamePanal;
    [SerializeField] private TextMeshProUGUI endingText;

    private bool isTyping = false;
    private Coroutine typingCoroutine;

    private List<int> selectList = new List<int>();
    private StoryNarrationSO currentNarration;
    private int currentStoryId = 1;

    private int choiceCount = 0;
    private int soundPlayId = 0;

    private bool playEventNarration = false;
    private int eventCount = 0;
    private string eventSoundPlayText;

    private string currentSoundNarration;   //최근 마지막 사운드 나레이션

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (storyDatabase != null)
        {
            storyDatabase.Initialize();
        }
        else
        {
            Debug.LogError("Story Database is not assigned to Dialog Manager");
        }

        ChoiceStoryObject[] choiceDialogObjects = FindObjectsOfType<ChoiceStoryObject>();
        var temps = new Dictionary<int, ChoiceStoryObject>();

        foreach (var obj in choiceDialogObjects)
            temps.Add(obj.choiceObjectId, obj);

        for (int i = 1; i <= choiceDialogObjects.Length; i++)
        {
            if (temps.TryGetValue(i, out ChoiceStoryObject choiceDialogObject))
            {
                choiceObjects.Add(choiceDialogObject.gameObject);
                choiceTexts.Add(choiceDialogObject.choiceText);
            }
        }
    }
    public void StartNarration(int storyId, bool useCheat = false)
    {
        if (!useCheat)
        {
            if (storyId != currentStoryId) return;                                  //Id체크 
        }
        else
        {
            currentStoryId = storyId;
        }

        StoryNarrationSO narration = storyDatabase.GetNarrationById(storyId);   //현재 아이디에 맞는 나레이션 데이터 찾기
        if (narration != null)
        {
            //lastStoryNextId = narration.nId;                                     //다음 순서가 선택 또는 이벤트 인지 확인용 Id할당
            soundPlayId = narration.id;

            currentNarration = narration;                                           //현재 나레이션 데이터를 최근 나레이션으로 할당(필드로 쓰기 위해서)
            currentStoryId = currentNarration.nextId;                               //다음으로 실행될 Id 할당
            narrationPanel.SetActive(true);                                         //나레이션 패널 활성화

            if (!string.IsNullOrEmpty(currentSoundNarration))                   //이전(최근) 나레이션 종료
            {
                SoundManager.instance.PauseSound(currentSoundNarration);
            }
            SoundManager.instance.PlaySound("Narration_" + soundPlayId);         //해당 Id에 맞는 나레이션 실행
            currentSoundNarration = $"Narration_{soundPlayId}";                  //최근 나레이션 갱신

            ShowStory(currentNarration.text);                                    //나레이션 Text생성

            if (currentStoryId < 0 && currentStoryId >= -1)                       //선택지일 경우, 실행
            {
                if (currentNarration.choices != null && currentNarration.choices.Count > 0)
                {
                    ShowChoices();
                }
            }
            else if (currentStoryId < -1)                                           //이벤트일 경우, 실행
            {
                if (currentNarration.events != null && currentNarration.events.Count > 0)
                {
                    if (eventCount > 3) return;

                    playEventNarration = true;
                    string eventText = "";

                    foreach (var s in GameManager.Instance.choiceds)
                    {
                        StoryEventSO data = currentNarration.events.Find(p => p.condition == s);

                        if (data == null) continue;
                        eventText = data.eventText;
                        eventSoundPlayText = data.condition;
                        currentStoryId = data.nextId;
                    }

                    ShowStory(eventText);                                           //이벤트 Text생성
                }
            }
        }
        else
        {
            Debug.LogError($"Narration with ID {storyId} not found!");
        }
    }

    public void ShowStory(string text)
    {
        if (playEventNarration)
        {
            StartCoroutine(PauseWhileTyping(text));
            playEventNarration = false;
        }
        else
        {
            StartTypingEffect(text);
        }
    }
    private IEnumerator PauseWhileTyping(string text)
    {
        yield return new WaitUntil(() => !isTyping);
        narrationPanel.SetActive(true);                                           //나레이션 패널 활성화

        SoundManager.instance.PlaySound("Narration_" + eventSoundPlayText);    //해당 Id에 맞는 나레이션 실행
        currentSoundNarration = $"Narration_{eventSoundPlayText}";                  //최근 나레이션 갱신

        eventSoundPlayText = "";
        StartTypingEffect(text);
    }

    private void StartTypingEffect(string text)
    {
        //Debug.Log(text);

        isTyping = true;
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(text));
    }

    //텍스트 타이핑 효과 코루틴
    private IEnumerator TypeText(string text)
    {
        narrationText.text = "";
        foreach (char c in text)
        {
            narrationText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        if(playEventNarration) yield return new WaitForSeconds(3f);
        else yield return new WaitForSeconds(7f);

        CloseNarration();
        isTyping = false;
    }

    private void CloseNarration()
    {
        narrationPanel.SetActive(false);
        StopTypingEffect();
    }

    //타이핑 효과 중지
    private void StopTypingEffect()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }

    public void ClearChoices()
    {
        //선택글끄기
    }

    //선택지 표시
    private void ShowChoices()
    {
        foreach (var choice in currentNarration.choices)
        {
            selectList.Add(choiceCount + 1);                 //현재 나레이션에 할당되어있는 선택지 받기(필드로 저장)
            if (choiceTexts.Count > 0)
            {
                choiceTexts[choiceCount].text = choice.text;  //해당 선택대상(발판)에 선택지 텍스트 할당
                choiceCount++;
            }
            else
            {
                //Debug.Log("선택 텍스트가 없습니다.");
            }
        }
    }

    public void SelectChoice(int id)                        //선택지(발판 충돌) 감지
    {
        StoryChoiceSO choice = storyDatabase.GetStoryChoiceByText(choiceTexts[id - 1].text);   //선택된 발판의 Id로 데이터를 찾아서 받기

        foreach (var num in selectList)
        {
            if (id != num)                             //선택 대상들 중, 선택되지 못한 발판들의 선택기능 비활성화시키기
            {
                var temp = choiceObjects.Find(p => p.GetComponent<ChoiceStoryObject>().choiceObjectId == num);
                temp.GetComponent<ChoiceStoryObject>().isOneTime = true;
            }
        }

        if (choice != null)
        {
            GameManager.Instance.choiceds.Add(choice.text);  //선택한 텍스트 저장

            if (choice.nextId > 0)
            {
                currentStoryId = choice.nextId;                  //선택지 다음 순서의 Id번호 할당
                StartNarration(choice.nextId);                      //선택후, 해당 선택지에 대한 나레이션 시작
            }
            else
            {
                currentStoryId = 13;
            }
        }
        else
        {
            //Debug.Log("해당 선택지를 찾을 수 없습니다.");
        }
    }

    public void EndGame()
    {
        endGamePanal.SetActive(true);
        StoryEndingSO endingData = storyDatabase.GetEndingById(GameManager.Instance.CheckEnding());

        //해당 엔딩 텍스트 적용
        if (endingData == null) return;
        string[] t_temp = endingData.text.Split("_");
        string _endingText = $"{t_temp[0]}-{t_temp[1]}";
        _endingText += "\n";
        _endingText += "\n";
        _endingText += t_temp[2];

        endingText.text = _endingText;


        //해당 엔딩 나레이션 작동
        string i_temp = endingData.id.ToString();
        i_temp = i_temp.Substring(i_temp.Length - 1);
        int id = int.Parse(i_temp);

        //Debug.Log(id);

        StartCoroutine(WaitforEnding(id));
        //엔딩 사운드 추가

        //엔딩 텍스트가 희미하보기 시작하는 연출 추가
    }

    IEnumerator WaitforEnding(int id)
    {
        yield return new WaitForSeconds(2f);
        SoundManager.instance.PlaySound("Ending_" + id);
    }
}
