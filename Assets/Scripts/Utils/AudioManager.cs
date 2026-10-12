using UnityEngine;

namespace AllinDefense.Utils
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip gameplayMusic;
        [SerializeField] private AudioClip victoryMusic;
        [SerializeField] private AudioClip gameOverMusic;

        [Header("SFX")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip arrowShootSfx;
        [SerializeField] private AudioClip fireballSfx;
        [SerializeField] private AudioClip explosionSfx;
        [SerializeField] private AudioClip enemyDieSfx;
        [SerializeField] private AudioClip goldPickupSfx;
        [SerializeField] private AudioClip levelUpSfx;
        [SerializeField] private AudioClip towerPlaceSfx;
        [SerializeField] private AudioClip uiClickSfx;

        [Header("Settings")]
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.4f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            PlayMusic(gameplayMusic);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null) return;
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void PlayArrowShoot()  => PlaySFX(arrowShootSfx);
        public void PlayFireball()    => PlaySFX(fireballSfx);
        public void PlayExplosion()   => PlaySFX(explosionSfx);
        public void PlayEnemyDie()    => PlaySFX(enemyDieSfx);
        public void PlayGoldPickup()  => PlaySFX(goldPickupSfx);
        public void PlayLevelUp()     => PlaySFX(levelUpSfx);
        public void PlayTowerPlace()  => PlaySFX(towerPlaceSfx);
        public void PlayUIClick()     => PlaySFX(uiClickSfx);

        public void SetMusicVolume(float vol)
        {
            musicVolume = vol;
            if (musicSource != null) musicSource.volume = vol;
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = vol;
        }

        public void PlayVictory()
        {
            PlayMusic(victoryMusic);
        }

        public void PlayGameOver()
        {
            PlayMusic(gameOverMusic);
        }
    }
}
