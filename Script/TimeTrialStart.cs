using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TimeTrialStart : MonoBehaviour
{
    public Transform player; // Reference to the player
    public AudioClip trialMusic; // The audio clip to be played
    public Text timerText; // Reference to the UI Text for displaying the timer
    public GameObject endObject; // Reference to the end object

    private AudioSource audioSource; // Audio source for the music
    private float timer = 0f;
    private bool isTiming = false;

    void Start()
    {
        // Create and configure the AudioSource component at runtime
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = trialMusic;
        audioSource.playOnAwake = false;
        audioSource.loop = true; // Loop the audio if needed

        // Ensure the timer text is empty at the start
        if (timerText != null)
            timerText.text = "Time: 0.00s";

        // Set the end object to inactive at the start
        if (endObject != null)
        {
            endObject.SetActive(false);
            Debug.Log("End part " + endObject.name + " set to inactive.");
        }
        else
        {
            Debug.LogWarning("End object not assigned in the Inspector.");
        }
    }

    void Update()
    {
        // Increment the timer if timing is active
        if (isTiming)
        {
            timer += Time.deltaTime;

            // Update the timer text
            if (timerText != null)
                timerText.text = "Time: " + timer.ToString("F2") + "s";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Start the trial if the player collides with the start trigger
        if (other.transform == player && !isTiming)
        {
            isTiming = true;
            audioSource.Play();

            if (endObject != null)
            {
                endObject.SetActive(true);
                Debug.Log("End part " + endObject.name + " set to active.");
            }

            Debug.Log("Time trial started!");
        }
    }

    public void StopTiming()
    {
        if (isTiming)
        {
            isTiming = false;
            audioSource.Stop();
            Debug.Log("Time trial stopped.");
            StartCoroutine(ResetTimerAfterDelay(2f)); // Reset timer after 2 seconds
        }
    }

    private IEnumerator ResetTimerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        timer = 0f;
        if (timerText != null)
            timerText.text = "Time: 0.00s";
    }

    public bool IsTimingActive()
    {
        return isTiming;
    }
}
