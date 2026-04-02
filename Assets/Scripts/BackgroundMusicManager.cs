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

    [SerializeField, Range(1, 16)] private int sfxPoolSize = 8;
    [SerializeField, Range(1, 8)] private int voicePoolSize = 4;
    [SerializeField] private bool spatializeSFX = false;

    private List<AudioSource> sfxPool = new List<AudioSource>();
    private List<AudioSource> voicePool = new List<AudioSource>();
    private Dictionary<string, AudioClip> voiceDict = new Dictionary<string, AudioClip>();
    private HashSet<AudioSource> positionalSources = new HashSet<AudioSource>();

    // ==================== UNITY MESSAGES ====================

    private void Awake()
    {
        musicSource = GetComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = false;

        // Create SFX pool
        for (int i = 0; i < Mathf.Max(1, sfxPoolSize); i++)
        {
            var sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = spatializeSFX ? 1f : 0f;
            sfxSource.priority = 128;
            sfxPool.Add(sfxSource);
        }

        // Create Voice pool (for simultaneous voice lines)
        for (int i = 0; i < Mathf.Max(1, voicePoolSize); i++)
        {
            var voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = spatializeSFX ? 1f : 0f;
            voiceSource.priority = 64; // Higher priority for voices
            voicePool.Add(voiceSource);
        }

        BuildVoiceDictionary();

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetVolume(masterVolume);
        SetSFXVolume(sfxVolume);
        SetVoiceVolume(voiceVolume);

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

    /// <summary>
    /// Play a voice line (multiple can play simultaneously)
    /// </summary>
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

    /// <summary>
    /// Play a voice line and get the AudioSource reference (for stopping/customizing)
    /// </summary>
    public AudioSource PlayVoiceLineWithReference(string key, float volumeOverride = -1)
    {
        if (string.IsNullOrEmpty(key)) return null;
        string lookupKey = key.ToLowerInvariant();

        if (voiceDict.TryGetValue(lookupKey, out AudioClip clip) && clip != null)
        {
            float finalVolume = volumeOverride >= 0 ? volumeOverride : voiceVolume;
            return PlayOneShotVoiceWithReference(clip, finalVolume);
        }
        Debug.LogWarning($"🗣️ Voice line '{key}' not found");
        return null;
    }

    /// <summary>
    /// Stop a specific voice line by its AudioSource
    /// </summary>
    public void StopVoiceLine(AudioSource voiceSource)
    {
        if (voiceSource != null && voiceSource.isPlaying)
            voiceSource.Stop();
    }

    /// <summary>
    /// Stop all currently playing voice lines
    /// </summary>
    public void StopAllVoiceLines()
    {
        foreach (var source in voicePool)
            if (source.isPlaying) source.Stop();
    }

    /// <summary>
    /// Play multiple voice lines sequentially with optional delay between them
    /// </summary>
    public void PlayVoiceLineSequence(List<string> keys, float delayBetween = 0.5f)
    {
        if (keys == null || keys.Count == 0) return;
        StartCoroutine(PlayVoiceSequenceCoroutine(keys, delayBetween));
    }

    /// <summary>
    /// Play a custom SFX (multiple can play simultaneously)
    /// </summary>
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

    /// <summary>
    /// Play custom SFX and get reference to the AudioSource
    /// </summary>
    public AudioSource PlayCustomSFXWithReference(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return null;
        var source = GetAvailableSFXSource();
        if (source != null)
        {
            source.pitch = pitch;
            source.volume = Mathf.Clamp01(volume);
            source.clip = clip;
            source.Play();
            StartCoroutine(ClearClipAfterPlayback(source));
            return source;
        }
        return null;
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

    /// <summary>
    /// Play an SFX that loops until stopped
    /// </summary>
    public AudioSource PlayLoopingSFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return null;
        var source = GetAvailableSFXSource();
        if (source != null)
        {
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.loop = true;
            source.Play();
            return source;
        }
        return null;
    }

    /// <summary>
    /// Stop a looping SFX
    /// </summary>
    public void StopLoopingSFX(AudioSource source)
    {
        if (source != null)
        {
            source.loop = false;
            source.Stop();
            source.clip = null;
        }
    }

    public void SetSFXVolume(float newVolume)
    {
        sfxVolume = Mathf.Clamp01(newVolume);
        // Update volume for all currently playing SFX
        foreach (var source in sfxPool)
        {
            if (source.isPlaying && !source.loop)
            {
                // For one-shot clips, we can't change volume mid-play
                // For looping clips, we can
                if (source.loop)
                    source.volume = sfxVolume;
            }
        }
    }

    public void SetVoiceVolume(float newVolume)
    {
        voiceVolume = Mathf.Clamp01(newVolume);
        // Update volume for all currently playing voice lines
        foreach (var source in voicePool)
        {
            if (source.isPlaying && source.loop)
                source.volume = voiceVolume * voiceVolumeMultiplier;
        }
    }

    public void StopAllSFX()
    {
        foreach (var source in sfxPool) if (source.isPlaying) source.Stop();
        StopAllVoiceLines();
    }

    public bool HasVoiceLine(string key) => !string.IsNullOrEmpty(key) && voiceDict.ContainsKey(key.ToLowerInvariant());
    public List<string> GetAvailableVoiceKeys() => voiceDict.Keys.ToList();

    /// <summary>
    /// Get the number of currently playing SFX
    /// </summary>
    public int GetPlayingSFXCount() => sfxPool.Count(s => s.isPlaying);

    /// <summary>
    /// Get the number of currently playing voice lines
    /// </summary>
    public int GetPlayingVoiceCount() => voicePool.Count(v => v.isPlaying);

    // ==================== 🔊 SFX INTERNAL METHODS ====================

    private void PlayOneShotSFX(AudioClip clip, float volume)
    {
        if (clip == null) return;
        var source = GetAvailableSFXSource();
        if (source != null)
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void PlayOneShotVoice(AudioClip clip, float volume)
    {
        if (clip == null) return;
        var source = GetAvailableVoiceSource();
        if (source != null)
        {
            source.PlayOneShot(clip, volume * voiceVolumeMultiplier);
        }
    }

    private AudioSource PlayOneShotVoiceWithReference(AudioClip clip, float volume)
    {
        if (clip == null) return null;
        var source = GetAvailableVoiceSource();
        if (source != null)
        {
            source.PlayOneShot(clip, volume * voiceVolumeMultiplier);
            return source;
        }
        return null;
    }

    private AudioSource GetAvailableSFXSource()
    {
        // First try to find an inactive source
        foreach (var source in sfxPool)
            if (!source.isPlaying) return source;

        // If all are busy, find the oldest playing source (lowest time)
        AudioSource oldest = sfxPool.OrderBy(s => s.time).FirstOrDefault();
        if (oldest != null)
        {
            Debug.LogWarning("⚠️ SFX pool full - reusing oldest source (may cut off sound)");
            oldest.Stop();
            return oldest;
        }

        return sfxPool[0];
    }

    private AudioSource GetAvailableVoiceSource()
    {
        // First try to find an inactive source
        foreach (var source in voicePool)
            if (!source.isPlaying) return source;

        // If all are busy, find the oldest playing voice
        AudioSource oldest = voicePool.OrderBy(v => v.time).FirstOrDefault();
        if (oldest != null)
        {
            Debug.LogWarning("⚠️ Voice pool full - reusing oldest source (may cut off voice)");
            oldest.Stop();
            return oldest;
        }

        return voicePool[0];
    }

    private IEnumerator ClearClipAfterPlayback(AudioSource source)
    {
        if (source != null && source.clip != null)
        {
            yield return new WaitForSeconds(source.clip.length);
            if (source != null && !source.loop)
                source.clip = null;
        }
    }

    private IEnumerator PlayVoiceSequenceCoroutine(List<string> keys, float delayBetween)
    {
        foreach (string key in keys)
        {
            PlayVoiceLine(key);
            yield return new WaitForSeconds(delayBetween);
        }
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
        int playingSFX = 0;
        foreach (var s in sfxPool) if (s.isPlaying) playingSFX++;
        int playingVoice = 0;
        foreach (var v in voicePool) if (v.isPlaying) playingVoice++;
        Debug.Log($"🔊 SFX Pool: {playingSFX}/{sfxPool.Count} playing | Voice Pool: {playingVoice}/{voicePool.Count} playing");
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