using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;


namespace UnknownTechnology.Audio
{
    public class AudioManager : MonoBehaviour
    {

        [SerializeField] private AudioClip testMusic;
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioClip[] footsteps;
        [SerializeField] private AudioClip buttonClickSound;

        [Header("Scepter")]
        [FormerlySerializedAs("scepterGrabSound")]
        [Tooltip("Plays when the scepter successfully picks up an object.")]
        [SerializeField] private AudioClip scepterPickupSound;
        [Tooltip("Optional loop that plays while the scepter carries an object.")]
        [SerializeField] private AudioClip scepterCarryLoop;
        [FormerlySerializedAs("scepterReleaseSound")]
        [Tooltip("Plays when an object is dropped, rather than placed into a slot.")]
        [SerializeField] private AudioClip scepterDropSound;
        [Tooltip("Plays when an object actually arrives in a slot.")]
        [SerializeField] private AudioClip scepterPlaceSound;
        [SerializeField, Range(0f, 1f)] private float scepterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float scepterCarryVolume = 0.4f;

        private AudioSource scepterCarrySource;

        private void OnEnable()
        {
            GameEvents.CarryablePlaced += HandleCarryablePlaced;
        }

        private void OnDisable()
        {
            GameEvents.CarryablePlaced -= HandleCarryablePlaced;
            StopScepterCarrySound();
        }

        private void Awake()
        {
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

        public void PlayMainMenuMusic()
        {
            if(testMusic == null)
            {
                return;
            }
            musicSource.clip = testMusic;
            musicSource.Play();
        }

        private void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null || !isActiveAndEnabled)
            {
                return;
            }
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        public void PlayScepterPickupSound()
        {
            PlaySFX(scepterPickupSound, scepterVolume);
        }

        public void PlayScepterDropSound()
        {
            PlaySFX(scepterDropSound, scepterVolume);
        }

        public void StartScepterCarrySound()
        {
            if (scepterCarryLoop == null || sfxSource == null || !isActiveAndEnabled)
            {
                return;
            }

            if (scepterCarrySource == null)
            {
                // A dedicated source lets the hum stop without cutting off
                // footsteps or other one-shot effects on the shared SFX source.
                var sourceObject = new GameObject("ScepterCarrySource");
                sourceObject.transform.SetParent(transform, false);
                scepterCarrySource = sourceObject.AddComponent<AudioSource>();
                scepterCarrySource.playOnAwake = false;
                scepterCarrySource.loop = true;
                scepterCarrySource.spatialBlend = 0f;
            }

            if (scepterCarrySource.isPlaying)
            {
                return;
            }

            scepterCarrySource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
            scepterCarrySource.volume = scepterCarryVolume;
            scepterCarrySource.clip = scepterCarryLoop;
            scepterCarrySource.Play();
        }

        public void StopScepterCarrySound()
        {
            if (scepterCarrySource != null && scepterCarrySource.isPlaying)
            {
                scepterCarrySource.Stop();
            }
        }

        private void HandleCarryablePlaced(CarryableItem item)
        {
            if (Game.Phase == GamePhase.Exploring && !SaveSystem.IsRestoring)
            {
                PlaySFX(scepterPlaceSound, scepterVolume);
            }
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

        public void PlayButtonClickSound()
        {
            PlayUI(buttonClickSound);
        }

        public void PlayFootstepSound()
        {
            if (footsteps.Length == 0)
            {
                return;
            }
            int index = Random.Range(0, footsteps.Length);
            PlaySFX(footsteps[index]);
        }

        private void Start()
        {
            ApplySettings(Game.Settings);
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
