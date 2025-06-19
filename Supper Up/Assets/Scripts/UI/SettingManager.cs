using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    public static float mouseSenesitivity = 100.0f;
    public static float sound_Master = 0.8f;
    public static float sound_BGM = 0.35f;
    public static float sound_SFX = 0.4f;

    public Slider mouseSensitivitySlider;
    public Slider sound_MasterSlider;
    public Slider sound_BGMSlider;
    public Slider sound_SFXSlider;

    private CameraController CameraController;

    void Awake()
    {
        mouseSensitivitySlider.value = mouseSenesitivity / 200;
        sound_MasterSlider.value = sound_Master;
        sound_BGMSlider.value = sound_BGM;
        sound_SFXSlider.value = sound_SFX;

        mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSenesitivity);
        sound_MasterSlider.onValueChanged.AddListener(SetMasterVolumeValue);
        sound_BGMSlider.onValueChanged.AddListener(SetBGMVolumeValue);
        sound_SFXSlider.onValueChanged.AddListener(SetSFXVolumeValue);

        CameraController = GameObject.FindObjectOfType<CameraController>();
        if (CameraController != null)
        {
            CameraController.mouseSenesitivity = mouseSenesitivity;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TurnOffSetting();
        }
    }

    void TurnOffSetting()
    {
        gameObject.SetActive(false);
    }

    void SetMouseSenesitivity(float value)
    {
        mouseSenesitivity = value * 200;
        //Debug.Log("마우스 감도 : " + mouseSenesitivity);
        if (CameraController != null)
        {
            CameraController.mouseSenesitivity = mouseSenesitivity;
        }
    }

    void SetMasterVolumeValue(float value)
    {
        sound_Master = value;
    }
    void SetBGMVolumeValue(float value)
    {
        sound_BGM = value;
    }
    void SetSFXVolumeValue(float value)
    {
        sound_SFX = value;
    }

}
