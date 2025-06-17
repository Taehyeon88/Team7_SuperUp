using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndingObject : MonoBehaviour
{
    private bool isOneTime = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isOneTime)
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.choiceds.Count < 4) return;

                isOneTime = true;
                Cursor.lockState = CursorLockMode.None;
                other.transform.position = new Vector3(14.4f, 849f, -94.1f);    //아무소리도 안들리는 위치로 이동

                if (UiManager.instance != null)
                {
                    UiManager.instance.isPlayGame = false;       //게임 재시작용 준비
                }

                GameManager.Instance.isGameEnd = true;

                if (StoryManager.Instance != null)
                {
                    StoryManager.Instance.EndGame();
                    Debug.Log("End");
                }

                if (SoundManager.instance != null)
                {
                    SoundManager.instance.PauseAllSounds();
                    SoundManager.instance.PlaySound("GameEnd");
                }

                //Time.timeScale = 0f;
            }
        }
    }
}
