using UnityEngine;
using System.Collections;

public class TimeTrialEnd : MonoBehaviour
{
    public GameObject particleEffect; // Particle effect GameObject to be toggled
    public Camera playerCamera; // Reference to the player's camera
    private TimeTrialStart timeTrialStart; // Reference to the TimeTrialStart script

    void Start()
    {
        // Find the TimeTrialStart script in the parent GameObject
        timeTrialStart = GetComponentInParent<TimeTrialStart>();

        if (timeTrialStart == null)
        {
            Debug.LogError("TimeTrialEnd could not find the parent TimeTrialStart script.");
        }

        // Ensure the particle effect is initially inactive
        if (particleEffect != null)
            particleEffect.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player triggered the end and if the time trial is active
        if (timeTrialStart != null && timeTrialStart.IsTimingActive() && other.transform == timeTrialStart.player)
        {
            timeTrialStart.StopTiming();
            Debug.Log("Time trial completed!");

            // Activate the particle effect and set a timer to deactivate it
            if (particleEffect != null)
            {
                particleEffect.SetActive(true);
                StartCoroutine(DeactivateParticleEffect(2f)); // Visible for 2 seconds
            }
        }
    }

    private IEnumerator DeactivateParticleEffect(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (particleEffect != null)
            particleEffect.SetActive(false);
    }
}
