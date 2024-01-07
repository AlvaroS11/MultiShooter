using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioController : MonoBehaviour
{

    public AudioMixer audioMixer;

    public Slider slider;
    
    public void SetMasterVolume()
    {
        audioMixer.SetFloat("Master", slider.value);
        PlayerPrefs.SetFloat("masterVolume", slider.value );
        PlayerPrefs.Save();
        Debug.Log(slider.value);
        float aa = -100;
        audioMixer.GetFloat("Master", out aa);
        Debug.Log(aa);
    }

    public void Start()
    {
        float audio = PlayerPrefs.GetFloat("masterVolume");
        audioMixer.SetFloat("Master", audio);
        slider.value = audio;
    }
}
