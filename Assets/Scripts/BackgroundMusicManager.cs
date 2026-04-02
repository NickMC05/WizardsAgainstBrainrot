using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicManager : MonoBehaviour
{
    // ==================== 🎵 MUSIC SECTION ====================

    [Header("🎵 Music Playlist")]
    [Tooltip("List of music clips to play")]
    [SerializeField] private List<AudioClip> musicPlaylist = new List<AudioClip>();

    [Header("🔁 Playback Settings")]
    [Tooltip("Play clips in order or shuffle randomly")]
    [SerializeField] private PlaybackMode playbackMode = PlaybackMode.Sequential;

    [Tooltip("Restart playlist after last clip (Sequential mode only)")]
    [SerializeField] private bool loopPlaylist = true;

    [Tooltip("Delay between tracks (seconds)")]
    [SerializeField, Range(0f, 5f)] private float transitionDelay = 1f;

    [Tooltip("Prevent same track from playing twice in a row (Random mode)")]
    [SerializeField] private bool preventRepeatInRandom = true;

    [Header("🎚️ Audio Settings")]
    [Tooltip("Crossfade duration for smooth transitions")]
    [SerializeField, Range(0f, 3f)] private float fadeDuration = 1f;

    [Tooltip("Master volume (0-1)")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 0.8f;

    [Tooltip("Allow music to persist across scenes")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("🔊 Debug")]
    [SerializeField, ReadOnly] private string currentlyPlaying = "None";
    [SerializeField, ReadOnly] private int tracksPlayed = 0;

    public enum PlaybackMode { Sequential, Random }

    // Music runtime fields
    private AudioSource musicSource;
    private int currentTrackIndex = -1;
    private bool isPlaying = false;
    private bool isFading = false;
    private Coroutine transitionCoroutine;
    private int lastPlayedIndex = -1;

    public bool IsPlaying => isPlaying;
    public AudioClip CurrentlyPlayingClip { get; private set; }
    public float Volume { get => masterVolume; set => SetVolume(value); }

    // ==================== 🔊 SFX & VOICE SECTION ====================

    [Header("🔊 Sound Effects (SFX)")]
    [SerializeField] private AudioClip clickSFX;
    [SerializeField] private AudioClip magicPlaySFX;
    [SerializeField] private AudioClip spellCastedSFX;
    [SerializeField] private AudioClip spellExplodeSFX;

    [Header("🗣️ Voice Lines (Key = String ID)")]
    [SerializeField] private List<VoiceLine> voiceLines = new List<VoiceLine>();

    [System.Serializable]
    public class VoiceLine
    {
        [Tooltip("Unique identifier to play this voice line")]
        public string key;
        public AudioClip clip;
    }

    [Header("⚙️ Volume Settings")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [Tooltip("Base volume for voice lines (0-1)")]
    [SerializeField, Range(0f, 1f)] private float voiceVolume = 1f;

    [Tooltip("🔊 Voice volume multiplier. Default 3.0 = 3x louder. Lower to 1.0-2.0 if clipping occurs.")]
    [SerializeField] private float voiceVolumeMultiplier = 3.0f;

    [SerializeField, Range(1, 8)] private int sfxPoolSize = 4;
    [SerializeField] private bool spatializeSFX = false;

    private List<AudioSource> sfxPool = new List<AudioSource>();
    private AudioSource voiceSource;
    private Dictionary<string, AudioClip> voiceDict = new Dictionary<string, AudioClip>();
    private HashSet<AudioSource> positionalSources = new HashSet<AudioSource>();

    // ==================== UNITY MESSAGES ====================

    private void Awake()
    {
        musicSource = GetComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = false;

        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = spatializeSFX ? 1f : 0f;

        for (int i = 0; i < Mathf.Max(1, sfxPoolSize); i++)
        {
            var sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = spatializeSFX ? 1f : 0f;
            sfxSource.priority = 128;
            sfxPool.Add(sfxSource);
        }

        BuildVoiceDictionary();

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetVolume(masterVolume);
        SetSFXVolume(sfxVolume);

        if (musicPlaylist.Count > 0)
            PlayMusic();
        else
            Debug.LogWarning("🎵 BackgroundMusicManager: Music playlist is empty!");
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    // ==================== 🎵 MUSIC METHODS ====================

    public void PlayMusic()
    {
        if (musicPlaylist.Count == 0) return;
        if (isPlaying) return;
        isPlaying = true;

        if (playbackMode == PlaybackMode.Random && currentTrackIndex == -1)
        {
            currentTrackIndex = Random.Range(0, musicPlaylist.Count);
            lastPlayedIndex = -1;
        }
        else if (currentTrackIndex == -1)
        {
            currentTrackIndex = 0;
        }

        PlayTrackByIndex(currentTrackIndex);
    }

    public void PauseMusic() { if (!isPlaying) return; musicSource.Pause(); isPlaying = false; }
    public void ResumeMusic() { if (isPlaying) return; musicSource.UnPause(); isPlaying = true; }

    public void StopMusic()
    {
        StopAllCoroutines();
        musicSource.Stop();
        isPlaying = false;
        CurrentlyPlayingClip = null;
        currentlyPlaying = "None";
    }

    public void NextTrack()
    {
        if (musicPlaylist.Count == 0) return;
        AdvanceTrackIndex();
        PlayTrackByIndex(currentTrackIndex, force: true);
    }

    public void PreviousTrack()
    {
        if (musicPlaylist.Count == 0) return;
        if (playbackMode == PlaybackMode.Random)
        {
            AdvanceTrackIndex();
        }
        else
        {
            currentTrackIndex = (currentTrackIndex - 1 + musicPlaylist.Count) % musicPlaylist.Count;
        }
        PlayTrackByIndex(currentTrackIndex, force: true);
    }

    public bool PlaySpecificTrack(string clipName)
    {
        var clip = musicPlaylist.FirstOrDefault(c => c != null && c.name.Equals(clipName, System.StringComparison.OrdinalIgnoreCase));
        if (clip != null)
        {
            int index = musicPlaylist.IndexOf(clip);
            currentTrackIndex = index;
            lastPlayedIndex = index;
            PlayTrackByIndex(index, force: true);
            return true;
        }
        Debug.LogWarning($"🎵 Track '{clipName}' not found");
        return false;
    }

    public void SetVolume(float newVolume, bool fade = false)
    {
        newVolume = Mathf.Clamp01(newVolume);
        masterVolume = newVolume;
        if (fade && !isFading)
            StartCoroutine(FadeVolumeRoutine(musicSource, musicSource.volume, newVolume));
        else
            musicSource.volume = newVolume;
    }

    // ==================== 🔊 SFX PUBLIC API ====================

    public void PlayClickSFX() => PlayOneShotSFX(clickSFX, sfxVolume);
    public void PlayMagicPlaySFX() => PlayOneShotSFX(magicPlaySFX, sfxVolume);
    public void PlaySpellCastedSFX() => PlayOneShotSFX(spellCastedSFX, sfxVolume);
    public void PlaySpellExplodeSFX() => PlayOneShotSFX(spellExplodeSFX, sfxVolume);

    public bool PlayVoiceLine(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        string lookupKey = key.ToLowerInvariant();

        if (voiceDict.TryGetValue(lookupKey, out AudioClip clip) && clip != null)
        {
            PlayOneShotVoice(clip, voiceVolume);
            return true;
        }
        Debug.LogWarning($"🗣️ Voice line '{key}' not found. Available: {string.Join(", ", voiceDict.Keys)}");
        return false;
    }

    public void PlayCustomSFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        var source = GetAvailableSFXSource();
        if (source != null)
        {
            source.pitch = pitch;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        var source = GetAvailableSFXSource();
        if (source != null)
        {
            Vector3 originalPos = source.transform.position;
            source.transform.position = position;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
            positionalSources.Add(source);
            StartCoroutine(ResetPositionAfterPlayback(source, originalPos));
        }
    }

    public void SetSFXVolume(float newVolume) => sfxVolume = Mathf.Clamp01(newVolume);
    public void SetVoiceVolume(float newVolume) => voiceVolume = Mathf.Clamp01(newVolume);

    public void StopAllSFX()
    {
        foreach (var source in sfxPool) if (source.isPlaying) source.Stop();
        if (voiceSource.isPlaying) voiceSource.Stop();
    }

    public bool HasVoiceLine(string key) => !string.IsNullOrEmpty(key) && voiceDict.ContainsKey(key.ToLowerInvariant());
    public List<string> GetAvailableVoiceKeys() => voiceDict.Keys.ToList();

    // ==================== 🔊 SFX INTERNAL METHODS ====================

    private void PlayOneShotSFX(AudioClip clip, float volume)
    {
        if (clip == null) return;
        var source = GetAvailableSFXSource();
        if (source != null) source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // 🔊 UPDATED: 3x louder voice playback
    private void PlayOneShotVoice(AudioClip clip, float volume)
    {
        if (clip == null) return;
        if (voiceSource.isPlaying) voiceSource.Stop();
        // 🔊 Multiplier applied (default 3.0x). Unity allows >1.0 but may clip if too high.
        voiceSource.PlayOneShot(clip, volume * voiceVolumeMultiplier);
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (var source in sfxPool) if (!source.isPlaying) return source;
        Debug.LogWarning("⚠️ SFX pool full - reusing source (may cut off sound)");
        return sfxPool[0];
    }

    private IEnumerator ResetPositionAfterPlayback(AudioSource source, Vector3 originalPos)
    {
        float waitTime = source.clip != null ? source.clip.length + 0.1f : 0.1f;
        yield return new WaitForSeconds(waitTime);
        source.transform.position = originalPos;
        positionalSources.Remove(source);
    }

    private void BuildVoiceDictionary()
    {
        voiceDict.Clear();
        foreach (var line in voiceLines)
        {
            if (!string.IsNullOrEmpty(line.key) && line.clip != null)
            {
                string key = line.key.ToLowerInvariant();
                if (voiceDict.ContainsKey(key))
                    Debug.LogWarning($"⚠️ Duplicate voice key '{key}' - using latest");
                voiceDict[key] = line.clip;
            }
        }
    }

    // ==================== 🎵 MUSIC INTERNAL METHODS ====================

    private void PlayTrackByIndex(int index, bool force = false)
    {
        if (musicPlaylist.Count == 0 || index < 0 || index >= musicPlaylist.Count) return;
        var clip = musicPlaylist[index];
        if (clip == null) { AdvanceTrackIndex(); PlayTrackByIndex(currentTrackIndex); return; }

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        CurrentlyPlayingClip = clip;
        currentlyPlaying = clip.name;
        lastPlayedIndex = index;
        tracksPlayed++;

        if (fadeDuration > 0f && musicSource.isPlaying && !force)
            StartCoroutine(CrossfadeToClipRoutine(clip));
        else
        {
            musicSource.clip = clip;
            musicSource.Play();
            musicSource.volume = masterVolume;
            StartCoroutine(WaitForTrackEndRoutine());
        }
        Debug.Log($"🎵 Now Playing: {clip.name} (Track #{tracksPlayed}, Mode: {playbackMode})");
    }

    private IEnumerator CrossfadeToClipRoutine(AudioClip newClip)
    {
        isFading = true;
        float startVol = musicSource.volume;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
            yield return null;
        }
        musicSource.clip = newClip;
        musicSource.Play();
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, masterVolume, elapsed / fadeDuration);
            yield return null;
        }
        musicSource.volume = masterVolume;
        isFading = false;
        StartCoroutine(WaitForTrackEndRoutine());
    }

    private IEnumerator WaitForTrackEndRoutine()
    {
        yield return new WaitForSeconds(musicSource.clip.length + transitionDelay);
        if (isPlaying && musicPlaylist.Count > 0)
        {
            AdvanceTrackIndex();
            PlayTrackByIndex(currentTrackIndex);
        }
    }

    private void AdvanceTrackIndex()
    {
        if (playbackMode == PlaybackMode.Sequential)
        {
            currentTrackIndex++;
            if (currentTrackIndex >= musicPlaylist.Count)
            {
                if (loopPlaylist)
                    currentTrackIndex = 0;
                else
                {
                    StopMusic();
                    return;
                }
            }
        }
        else // Random
        {
            if (musicPlaylist.Count <= 1)
            {
                currentTrackIndex = 0;
                return;
            }

            int newIndex;
            int attempts = 0;
            const int MAX_ATTEMPTS = 10;

            do
            {
                newIndex = Random.Range(0, musicPlaylist.Count);
                attempts++;
                if (!preventRepeatInRandom || newIndex != lastPlayedIndex) break;
            } while (attempts < MAX_ATTEMPTS);

            if (preventRepeatInRandom && newIndex == lastPlayedIndex && musicPlaylist.Count > 1)
            {
                newIndex = (lastPlayedIndex + Random.Range(1, musicPlaylist.Count)) % musicPlaylist.Count;
            }

            currentTrackIndex = newIndex;
        }
    }

    private IEnumerator FadeVolumeRoutine(AudioSource source, float from, float to)
    {
        isFading = true;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        source.volume = to;
        isFading = false;
    }

    // ==================== 🛠️ EDITOR UTILITIES ====================

#if UNITY_EDITOR
    [ContextMenu("Print Playlist")]
    private void PrintPlaylist()
    {
        Debug.Log($"📋 Music Playlist ({musicPlaylist.Count}):");
        for (int i = 0; i < musicPlaylist.Count; i++)
            Debug.Log($"  {i + 1}. {musicPlaylist[i]?.name ?? "NULL"}");
    }

    [ContextMenu("Print Voice Lines")]
    private void PrintVoiceLines()
    {
        Debug.Log($"🗣️ Voice Lines ({voiceLines.Count}):");
        foreach (var v in voiceLines)
            Debug.Log($"  '{v.key}' → {v.clip?.name ?? "NULL"}");
    }

    [ContextMenu("Auto-Detect Music in Resources")]
    private void AutoPopulateMusic()
    {
        var clips = Resources.LoadAll<AudioClip>("Music");
        if (clips.Length > 0)
        {
            musicPlaylist = new List<AudioClip>(clips);
            Debug.Log($"✅ Loaded {clips.Length} music clips from Resources/Music");
        }
        else
        {
            Debug.LogWarning("⚠️ No clips found in Assets/Resources/Music/");
        }
    }

    [ContextMenu("Show SFX Pool Status")]
    private void ShowSFXPoolStatus()
    {
        int playing = 0;
        foreach (var s in sfxPool) if (s.isPlaying) playing++;
        Debug.Log($"🔊 SFX Pool: {playing}/{sfxPool.Count} playing | Voice: {(voiceSource.isPlaying ? "Yes" : "No")}");
    }

    [ContextMenu("Test Random Mode")]
    private void TestRandomMode()
    {
        if (musicPlaylist.Count < 2)
        {
            Debug.LogWarning("Need at least 2 tracks to test random mode");
            return;
        }

        Debug.Log($"🎲 Testing Random Mode (10 selections):");
        var results = new Dictionary<string, int>();
        int last = -1;

        for (int i = 0; i < 10; i++)
        {
            int idx;
            do { idx = Random.Range(0, musicPlaylist.Count); }
            while (preventRepeatInRandom && idx == last && musicPlaylist.Count > 2);

            string name = musicPlaylist[idx]?.name ?? "NULL";
            if (!results.ContainsKey(name)) results[name] = 0;
            results[name]++;
            Debug.Log($"  {i + 1}. {name} {(idx == last ? "⚠️ REPEAT" : "")}");
            last = idx;
        }

        Debug.Log("📊 Distribution:");
        foreach (var kvp in results)
            Debug.Log($"  {kvp.Key}: {kvp.Value}x");
    }
#endif
}

// ==================== 📦 CUSTOM ATTRIBUTE FOR READ-ONLY INSPECTOR ====================

public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif