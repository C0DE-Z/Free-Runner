using UnityEngine;
using UnityEngine.UI;

public class TimeTrial : MonoBehaviour
{
    public Transform player; // Reference to the player
    public AudioClip trialMusic; // The audio clip to be played
    public Text timerText; // Reference to the UI Text for displaying the timer
    public GameObject endObject; // Reference to the end part

    private AudioSource audioSource; // Audio source for the music
    private float timer = 0f;
    private bool isTiming = false;
    private bool trialComplete = false;

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

        // Set the end part to be invisible at the start
        if (endObject != null)
        {
            endObject.SetActive(false);
            print("End part " + endObject.name + " set to inactive.");
        }
    }

    void Update()
    {
        // Increment the timer if timing is active and trial is not complete
        if (isTiming && !trialComplete)
        {
            timer += Time.deltaTime;

            // Update the timer text
            if (timerText != null)
                timerText.text = "Time: " + timer.ToString("F2") + "s";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Start the trial if player collides with the start trigger
        if (other.transform == player && !isTiming)
        {
            isTiming = true;
            audioSource.Play();

            if (endObject != null)
            {
                endObject.SetActive(true);
            }
            print("Time trial started!");
        }
        // Check for trial end only if timing is active
        else if (other.gameObject == endObject && isTiming)
        {
            trialComplete = true;
            isTiming = false;
            print("Time trial completed!");
            
            // Stop the timer and the music if desired
            audioSource.Stop();
        }
    }
}