using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static AudioManager Instance { get; private set; }
    
    [System.Serializable]
    public class SoundClip
    {
        public string name;
        public AudioClip clip;
        public AudioMixerGroup mixerGroup;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;
        [Range(0f, 1f)] public float spatialBlend = 0f;
        public float minDistance = 1f;
        public float maxDistance = 500f;
        [HideInInspector] public AudioSource source;
    }
    
    [Header("Audio Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup uiMixerGroup;
    
    [Header("Sound Clips")]
    [SerializeField] private SoundClip[] soundClips;
    [SerializeField] private SoundClip[] musicClips;
    [SerializeField] private SoundClip[] uiSoundClips;
    
    [Header("Game Specific Sounds")]
    [SerializeField] private SoundClip ballBounceSound;
    [SerializeField] private SoundClip ballHitSound;
    [SerializeField] private SoundClip ballNetHitSound;
    [SerializeField] private SoundClip ballOutSound;
    [SerializeField] private SoundClip pointWonSound;
    [SerializeField] private SoundClip gameWonSound;
    [SerializeField] private SoundClip matchWonSound;
    [SerializeField] private SoundClip[] crowdSounds;
    
    [Header("Music")]
    [SerializeField] private string menuMusic = "MenuTheme";
    [SerializeField] private string gameMusic = "GameTheme";
    [SerializeField] private string victoryMusic = "VictoryTheme";
    [SerializeField] private float musicFadeDuration = 1.5f;
    
    // 音量設定
    private float sfxVolume = 1f;
    private float musicVolume = 0.5f;
    private float uiVolume = 0.8f;
    
    // 現在再生中の音楽
    private string currentMusic = "";
    
    // オーディオソース
    private AudioSource mainAudioSource;
    private Dictionary<string, SoundClip> soundLookup = new Dictionary<string, SoundClip>();
    
    private void Awake()
    {
        // シングルトンの設定
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // メインのオーディオソースを取得
        mainAudioSource = GetComponent<AudioSource>();
        
        // サウンドクリップの初期化
        InitializeSoundClips();
        
        // 音量設定を読み込み
        LoadVolumeSettings();
    }
    
    private void Start()
    {
        // GameManagerからイベントを登録
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += OnGameStateChanged;
        }
        
        // 初期音楽の再生
        PlayMusic(menuMusic);
    }
    
    private void OnDestroy()
    {
        // GameManagerからイベント登録解除
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= OnGameStateChanged;
        }
    }
    
    private void InitializeSoundClips()
    {
        // SFXクリップの初期化
        foreach (SoundClip clip in soundClips)
        {
            if (clip.clip != null)
            {
                GameObject soundObject = new GameObject($"Sound_{clip.name}");
                soundObject.transform.SetParent(transform);
                
                AudioSource source = soundObject.AddComponent<AudioSource>();
                source.clip = clip.clip;
                source.volume = clip.volume;
                source.pitch = clip.pitch;
                source.loop = clip.loop;
                source.spatialBlend = clip.spatialBlend;
                source.minDistance = clip.minDistance;
                source.maxDistance = clip.maxDistance;
                source.playOnAwake = false;
                source.outputAudioMixerGroup = clip.mixerGroup != null ? clip.mixerGroup : sfxMixerGroup;
                
                clip.source = source;
                
                if (!soundLookup.ContainsKey(clip.name))
                {
                    soundLookup.Add(clip.name, clip);
                }
                else
                {
                    Debug.LogWarning($"Duplicate sound clip name: {clip.name}");
                }
            }
        }
        
        // 音楽クリップの初期化
        foreach (SoundClip clip in musicClips)
        {
            if (clip.clip != null)
            {
                GameObject musicObject = new GameObject($"Music_{clip.name}");
                musicObject.transform.SetParent(transform);
                
                AudioSource source = musicObject.AddComponent<AudioSource>();
                source.clip = clip.clip;
                source.volume = clip.volume;
                source.pitch = clip.pitch;
                source.loop = true; // 音楽は常にループ
                source.spatialBlend = 0f; // 音楽は常に2D
                source.playOnAwake = false;
                source.outputAudioMixerGroup = clip.mixerGroup != null ? clip.mixerGroup : musicMixerGroup;
                
                clip.source = source;
                
                if (!soundLookup.ContainsKey(clip.name))
                {
                    soundLookup.Add(clip.name, clip);
                }
                else
                {
                    Debug.LogWarning($"Duplicate music clip name: {clip.name}");
                }
            }
        }
        
        // UIサウンドクリップの初期化
        foreach (SoundClip clip in uiSoundClips)
        {
            if (clip.clip != null)
            {
                GameObject uiSoundObject = new GameObject($"UI_{clip.name}");
                uiSoundObject.transform.SetParent(transform);
                
                AudioSource source = uiSoundObject.AddComponent<AudioSource>();
                source.clip = clip.clip;
                source.volume = clip.volume;
                source.pitch = clip.pitch;
                source.loop = clip.loop;
                source.spatialBlend = 0f; // UIサウンドは常に2D
                source.playOnAwake = false;
                source.outputAudioMixerGroup = clip.mixerGroup != null ? clip.mixerGroup : uiMixerGroup;
                
                clip.source = source;
                
                if (!soundLookup.ContainsKey(clip.name))
                {
                    soundLookup.Add(clip.name, clip);
                }
                else
                {
                    Debug.LogWarning($"Duplicate UI sound clip name: {clip.name}");
                }
            }
        }
        
        // 特定のサウンドの初期化
        InitializeSpecificSound(ballBounceSound, "BallBounce", sfxMixerGroup);
        InitializeSpecificSound(ballHitSound, "BallHit", sfxMixerGroup);
        InitializeSpecificSound(ballNetHitSound, "BallNetHit", sfxMixerGroup);
        InitializeSpecificSound(ballOutSound, "BallOut", sfxMixerGroup);
        InitializeSpecificSound(pointWonSound, "PointWon", sfxMixerGroup);
        InitializeSpecificSound(gameWonSound, "GameWon", sfxMixerGroup);
        InitializeSpecificSound(matchWonSound, "MatchWon", sfxMixerGroup);
        
        // 群衆サウンドの初期化
        for (int i = 0; i < crowdSounds.Length; i++)
        {
            InitializeSpecificSound(crowdSounds[i], $"Crowd_{i}", sfxMixerGroup);
        }
    }
    
    private void InitializeSpecificSound(SoundClip clip, string name, AudioMixerGroup defaultMixerGroup)
    {
        if (clip != null && clip.clip != null)
        {
            // 名前が指定されていなければデフォルト名を使用
            if (string.IsNullOrEmpty(clip.name))
            {
                clip.name = name;
            }
            
            GameObject soundObject = new GameObject($"Sound_{clip.name}");
            soundObject.transform.SetParent(transform);
            
            AudioSource source = soundObject.AddComponent<AudioSource>();
            source.clip = clip.clip;
            source.volume = clip.volume;
            source.pitch = clip.pitch;
            source.loop = clip.loop;
            source.spatialBlend = clip.spatialBlend;
            source.minDistance = clip.minDistance;
            source.maxDistance = clip.maxDistance;
            source.playOnAwake = false;
            source.outputAudioMixerGroup = clip.mixerGroup != null ? clip.mixerGroup : defaultMixerGroup;
            
            clip.source = source;
            
            if (!soundLookup.ContainsKey(clip.name))
            {
                soundLookup.Add(clip.name, clip);
            }
            else
            {
                Debug.LogWarning($"Duplicate specific sound clip name: {clip.name}");
            }
        }
    }
    
    // サウンド再生
    public void PlaySound(string name)
    {
        if (soundLookup.TryGetValue(name, out SoundClip clip))
        {
            if (clip.source != null)
            {
                clip.source.Play();
            }
            else
            {
                Debug.LogWarning($"Sound source for {name} is null!");
            }
        }
        else
        {
            Debug.LogWarning($"Sound {name} not found!");
        }
    }
    
    // 位置指定サウンド再生
    public void PlaySoundAtPosition(string name, Vector3 position)
    {
        if (soundLookup.TryGetValue(name, out SoundClip clip))
        {
            if (clip.source != null)
            {
                clip.source.transform.position = position;
                clip.source.Play();
            }
            else
            {
                Debug.LogWarning($"Sound source for {name} is null!");
            }
        }
        else
        {
            Debug.LogWarning($"Sound {name} not found!");
        }
    }
    
    // サウンド停止
    public void StopSound(string name)
    {
        if (soundLookup.TryGetValue(name, out SoundClip clip))
        {
            if (clip.source != null)
            {
                clip.source.Stop();
            }
        }
    }
    
    // 音楽再生
    public void PlayMusic(string name)
    {
        if (currentMusic == name) return;
        
        // 現在の音楽をフェードアウト
        if (!string.IsNullOrEmpty(currentMusic) && soundLookup.TryGetValue(currentMusic, out SoundClip currentClip))
        {
            StartCoroutine(FadeOutMusic(currentClip.source));
        }
        
        // 新しい音楽をフェードイン
        if (soundLookup.TryGetValue(name, out SoundClip newClip))
        {
            StartCoroutine(FadeInMusic(newClip.source));
            currentMusic = name;
        }
        else
        {
            Debug.LogWarning($"Music {name} not found!");
        }
    }
    
    // ゲーム状態に応じた音楽再生
    private void OnGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.MainMenu:
                PlayMusic(menuMusic);
                break;
                
            case GameManager.GameState.SetupMatch:
            case GameManager.GameState.Serving:
            case GameManager.GameState.Rally:
                PlayMusic(gameMusic);
                break;
                
            case GameManager.GameState.GameOver:
                PlayMusic(victoryMusic);
                break;
        }
    }
    
    // 音楽のフェードイン
    private IEnumerator FadeInMusic(AudioSource musicSource)
    {
        float volume = 0f;
        musicSource.volume = 0f;
        musicSource.Play();
        
        while (volume < musicVolume)
        {
            volume += Time.deltaTime / musicFadeDuration;
            musicSource.volume = Mathf.Min(volume, musicVolume);
            yield return null;
        }
    }
    
    // 音楽のフェードアウト
    private IEnumerator FadeOutMusic(AudioSource musicSource)
    {
        float startVolume = musicSource.volume;
        float volume = startVolume;
        
        while (volume > 0f)
        {
            volume -= Time.deltaTime / musicFadeDuration;
            musicSource.volume = Mathf.Max(volume, 0f);
            yield return null;
        }
        
        musicSource.Stop();
        musicSource.volume = startVolume;
    }
    
    // 音量設定の読み込み
    private void LoadVolumeSettings()
    {
        sfxVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        uiVolume = PlayerPrefs.GetFloat("UIVolume", 0.8f);
        
        // ミキサーに適用
        UpdateMixerVolumes();
    }
    
    // ミキサー音量の更新
    private void UpdateMixerVolumes()
    {
        if (audioMixer != null)
        {
            // デシベル値に変換（-80dB〜0dB）
            float sfxDb = ConvertToDecibel(sfxVolume);
            float musicDb = ConvertToDecibel(musicVolume);
            float uiDb = ConvertToDecibel(uiVolume);
            
            audioMixer.SetFloat("SFXVolume", sfxDb);
            audioMixer.SetFloat("MusicVolume", musicDb);
            audioMixer.SetFloat("UIVolume", uiDb);
        }
    }
    
    // リニア値をデシベル値に変換
    private float ConvertToDecibel(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }
    
    // SFX音量の設定
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SoundVolume", sfxVolume);
        UpdateMixerVolumes();
    }
    
    // 音楽音量の設定
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        UpdateMixerVolumes();
        
        // 現在再生中の音楽の音量も更新
        if (!string.IsNullOrEmpty(currentMusic) && soundLookup.TryGetValue(currentMusic, out SoundClip currentClip))
        {
            currentClip.source.volume = musicVolume;
        }
    }
    
    // UI音量の設定
    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("UIVolume", uiVolume);
        UpdateMixerVolumes();
    }
    
    // UI効果音の再生
    public void PlayUISound(string name)
    {
        PlaySound(name);
    }
    
    #region Game Specific Sound Methods
    
    // ボールバウンド音の再生
    public void PlayBallBounceSound(Vector3 position)
    {
        if (ballBounceSound != null && ballBounceSound.source != null)
        {
            // ランダムピッチで変化をつける
            ballBounceSound.source.pitch = Random.Range(0.9f, 1.1f);
            PlaySoundAtPosition(ballBounceSound.name, position);
        }
    }
    
    // ボールヒット音の再生
    public void PlayBallHitSound(Vector3 position, float power)
    {
        if (ballHitSound != null && ballHitSound.source != null)
        {
            // パワーに応じてピッチと音量を調整
            float normalizedPower = Mathf.Clamp01(power / 20f); // 最大パワーを20と仮定
            ballHitSound.source.pitch = Mathf.Lerp(0.8f, 1.2f, normalizedPower);
            ballHitSound.source.volume = Mathf.Lerp(0.5f, 1.0f, normalizedPower) * sfxVolume;
            
            PlaySoundAtPosition(ballHitSound.name, position);
        }
    }
    
    // ネットヒット音の再生
    public void PlayNetHitSound(Vector3 position)
    {
        if (ballNetHitSound != null)
        {
            PlaySoundAtPosition(ballNetHitSound.name, position);
        }
    }
    
    // アウト音の再生
    public void PlayOutSound(Vector3 position)
    {
        if (ballOutSound != null)
        {
            PlaySoundAtPosition(ballOutSound.name, position);
        }
    }
    
    // ポイント獲得音の再生
    public void PlayPointWonSound()
    {
        if (pointWonSound != null)
        {
            PlaySound(pointWonSound.name);
        }
    }
    
    // ゲーム獲得音の再生
    public void PlayGameWonSound()
    {
        if (gameWonSound != null)
        {
            PlaySound(gameWonSound.name);
        }
    }
    
    // マッチ獲得音の再生
    public void PlayMatchWonSound()
    {
        if (matchWonSound != null)
        {
            PlaySound(matchWonSound.name);
        }
    }
    
    // 群衆の歓声の再生
    public void PlayRandomCrowdSound()
    {
        if (crowdSounds != null && crowdSounds.Length > 0)
        {
            int index = Random.Range(0, crowdSounds.Length);
            if (crowdSounds[index] != null)
            {
                PlaySound(crowdSounds[index].name);
            }
        }
    }
    
    #endregion
}
