using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuu : MonoBehaviour
{
    public AudioClip musicClip; // Assign this in the Inspector

    private AudioSource grahh;

    public void Start()
    {
        if (musicClip == null)
        {
            Debug.LogError("No AudioClip assigned. Please assign an AudioClip in the Inspector.");
            return;
        }

        // Create and configure the AudioSource component at runtime
        grahh = gameObject.AddComponent<AudioSource>();
        grahh.clip = musicClip;
        grahh.playOnAwake = false;

        // Play the audio
        grahh.Play();
    }

    public void PlayGame(string sceneName)
    {
        // Stop the audio when the start button is pressed
        if (grahh != null && grahh.isPlaying)
        {
            grahh.Stop();
        }

        SceneManager.LoadSceneAsync(sceneName);
    }
}
