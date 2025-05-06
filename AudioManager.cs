using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    public AudioClip background;
    public AudioClip checkpoint;
    public AudioClip death;
    public AudioClip staminaDeplete;
    public AudioClip jumpGrunt;
    public AudioClip jumpFeet;
    public AudioClip beep;

    private void Start()
    {
        musicSource.clip = background;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
   
}