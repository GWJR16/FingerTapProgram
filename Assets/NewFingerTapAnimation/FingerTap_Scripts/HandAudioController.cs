using UnityEngine;

public class HandAudioController : MonoBehaviour
{
    [Tooltip("AudioSource on the hand object. Assign a short click/pulse.")]
    public AudioSource audioSource;

    [Tooltip("Which OUT peak should fire (2 or 3). 0 = off")]
    public int targetPeak = 0;

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    // Called by Animation Events. Set the event's Int parameter to the OUT peak index (1,2,3,...).
    public void OnAbductionPeak(int peakIndex)
    {
        if (targetPeak > 0 && peakIndex == targetPeak && audioSource && audioSource.clip)
        {
            audioSource.Play();
        }
    }

    // Optional: if you ever add an event with no parameter
    public void PlayTapSound()
    {
        if (audioSource && audioSource.clip) audioSource.Play();
    }

    public void StopSound()
    {
        if (audioSource) audioSource.Stop();
    }
}
