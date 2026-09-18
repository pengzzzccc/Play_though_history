using UnityEngine;
using UnityEngine.Audio;


namespace UnknownTechnology.Audio
{
    public class AudioManager : MonoBehaviour
    {

        [SerializeField] private AudioClip testMusic;
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        private void Awake()
        {
            ApplySettings(Game.Settings);
            Game.Settings.Changed += ApplySettings;
        }

        private void OnDestroy()
        {
            Game.Settings.Changed -= ApplySettings;
        }
        public void SetMasterVolume(float volume)
        {
            audioMixer.SetFloat("MasterVolume", VolumeToDecibels(volume));
        }

        public void SetMusicVolume(float volume)
        {
            audioMixer.SetFloat("MusicVolume", VolumeToDecibels(volume));
        }

        public void SetSFXVolume(float volume)
        {
            audioMixer.SetFloat("SFXVolume", VolumeToDecibels(volume));
        }

        public void SetUIVolume(float volume)
        {
            audioMixer.SetFloat("UIVolume", VolumeToDecibels(volume));
        }

        // Helper Function to Convert Linear Volume to Decibels
        private float VolumeToDecibels(float volume)
        {
            //Convert linear volume (0.0 to 1.0) to decibels (-80dB to 0dB)
            return Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
        }

        public void PlayMusic(AudioClip clip)
        {
            if(clip == null)
            {
                return;
            }
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if(clip == null)
            {
                return;
            }
            sfxSource.PlayOneShot(clip);
        }

        public void PlayUI(AudioClip clip)
        {
            if(clip == null)
            {
                return;
            }
            uiSource.PlayOneShot(clip);
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void Start()
        {
            if (testMusic != null)
            {
                PlayMusic(testMusic);
            }
        }

        private void ApplySettings(GameSettings settings)
        {
            SetMasterVolume(settings.masterVolume);
            SetMusicVolume(settings.musicVolume);
            SetSFXVolume(settings.sfxVolume);
            SetUIVolume(settings.uiVolume);
        }
    }
}
