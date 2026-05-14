using UnityEngine;

namespace JunqiGame.UI
{
    /// <summary>
    /// 音效管理器
    /// 负责播放游戏中的各种音效
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("音频源")]
        [Tooltip("主音频源")]
        public AudioSource mainAudioSource;
        
        [Tooltip("UI音频源")]
        public AudioSource uiAudioSource;

        [Header("音效剪辑")]
        [Tooltip("点击音效")]
        public AudioClip clickSound;
        
        [Tooltip("移动音效")]
        public AudioClip moveSound;
        
        [Tooltip("吃子音效")]
        public AudioClip captureSound;
        
        [Tooltip("战斗音效")]
        public AudioClip combatSound;
        
        [Tooltip("胜利音效")]
        public AudioClip victorySound;
        
        [Tooltip("失败音效")]
        public AudioClip defeatSound;
        
        [Tooltip("按钮点击音效")]
        public AudioClip buttonClickSound;

        [Header("音量设置")]
        [Range(0, 1)]
        [Tooltip("主音量")]
        public float masterVolume = 1f;
        
        [Range(0, 1)]
        [Tooltip("UI音量")]
        public float uiVolume = 0.8f;
        
        [Range(0, 1)]
        [Tooltip("音效音量")]
        public float sfxVolume = 1f;

        private static AudioManager instance;
        public static AudioManager Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 如果没有设置音频源，创建默认的
            if (mainAudioSource == null)
            {
                mainAudioSource = gameObject.AddComponent<AudioSource>();
                mainAudioSource.playOnAwake = false;
            }

            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
                uiAudioSource.playOnAwake = false;
            }
        }

        /// <summary>
        /// 播放点击音效
        /// </summary>
        public void PlayClickSound()
        {
            PlaySFX(clickSound);
        }

        /// <summary>
        /// 播放移动音效
        /// </summary>
        public void PlayMoveSound()
        {
            PlaySFX(moveSound);
        }

        /// <summary>
        /// 播放吃子音效
        /// </summary>
        public void PlayCaptureSound()
        {
            PlaySFX(captureSound);
        }

        /// <summary>
        /// 播放战斗音效
        /// </summary>
        public void PlayCombatSound()
        {
            PlaySFX(combatSound);
        }

        /// <summary>
        /// 播放胜利音效
        /// </summary>
        public void PlayVictorySound()
        {
            PlaySFX(victorySound);
        }

        /// <summary>
        /// 播放失败音效
        /// </summary>
        public void PlayDefeatSound()
        {
            PlaySFX(defeatSound);
        }

        /// <summary>
        /// 播放按钮点击音效
        /// </summary>
        public void PlayButtonClickSound()
        {
            PlayUISound(buttonClickSound);
        }

        /// <summary>
        /// 播放音效（主音频源）
        /// </summary>
        private void PlaySFX(AudioClip clip)
        {
            if (clip != null && mainAudioSource != null)
            {
                mainAudioSource.volume = sfxVolume * masterVolume;
                mainAudioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// 播放UI音效
        /// </summary>
        private void PlayUISound(AudioClip clip)
        {
            if (clip != null && uiAudioSource != null)
            {
                uiAudioSource.volume = uiVolume * masterVolume;
                uiAudioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// 设置主音量
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 设置音效音量
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 设置UI音量
        /// </summary>
        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
        }
    }
}
