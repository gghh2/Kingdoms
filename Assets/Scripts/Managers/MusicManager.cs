using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Kingdoms.Managers
{
    [System.Serializable]
    public class MusicTrack
    {
        public string trackName = "New Track";
        public AudioClip audioClip;
        [Range(0f, 1f)]
        public float volume = 0.7f;
        public bool loop = true;
    }

    /// <summary>
    /// Manages background music with crossfading
    /// Simplified version for Kingdoms
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        #region Singleton
        
        private static MusicManager _instance;
        
        public static MusicManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("MusicManager not found in scene!");
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Music Tracks")]
        [Tooltip("List of all music tracks in the game")]
        public List<MusicTrack> musicTracks = new List<MusicTrack>();
        
        [Header("Settings")]
        [Tooltip("Master music volume")]
        [Range(0f, 1f)]
        public float masterVolume = 0.7f;
        
        [Tooltip("Fade duration when changing tracks")]
        public float fadeDuration = 2f;
        
        [Tooltip("Default track to play on start")]
        public string defaultTrackName = "";
        
        [Header("Random Play Settings")]
        [Tooltip("Enable random track selection")]
        public bool enableRandomPlay = false;
        
        [Tooltip("Avoid repeating the same track")]
        public bool avoidRepeats = true;
        
        [Tooltip("Auto play next random track when current ends")]
        public bool autoPlayNextRandom = true;
        
        [Header("Current State (Read-only)")]
        [SerializeField] private MusicTrack currentTrack;
        [SerializeField] private bool isPlaying = false;
        
        #endregion
        
        #region Private Fields
        
        private AudioSource primarySource;
        private AudioSource secondarySource;
        private Coroutine fadeCoroutine;
        private AudioSource activeSource;
        private AudioSource inactiveSource;
        private bool isFading = false;
        private List<MusicTrack> recentlyPlayedTracks = new List<MusicTrack>();
        private int maxRecentTracks = 3;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            SetupAudioSources();
        }
        
        private void Start()
        {
            // Load saved volume
            if (PlayerPrefs.HasKey("MusicVolume"))
            {
                masterVolume = PlayerPrefs.GetFloat("MusicVolume");
            }
            
            // Play default track or random if enabled
            if (enableRandomPlay)
            {
                PlayRandomTrack();
            }
            else if (!string.IsNullOrEmpty(defaultTrackName))
            {
                PlayTrackByName(defaultTrackName);
            }
            else if (musicTracks.Count > 0)
            {
                PlayTrack(musicTracks[0]);
            }
            
            Debug.Log($"MusicManager: Initialized with {musicTracks.Count} tracks");
        }
        
        private void Update()
        {
            // Auto-play next random track when current ends
            if (autoPlayNextRandom && enableRandomPlay && activeSource != null && currentTrack != null)
            {
                if (!currentTrack.loop && !activeSource.isPlaying && !isFading)
                {
                    PlayRandomTrack();
                }
            }
        }
        
        #endregion
        
        #region Audio Source Setup
        
        private void SetupAudioSources()
        {
            // Create primary audio source
            primarySource = gameObject.AddComponent<AudioSource>();
            primarySource.playOnAwake = false;
            primarySource.loop = true;
            primarySource.volume = 0f;
            
            // Create secondary audio source for crossfading
            secondarySource = gameObject.AddComponent<AudioSource>();
            secondarySource.playOnAwake = false;
            secondarySource.loop = true;
            secondarySource.volume = 0f;
            
            activeSource = primarySource;
            inactiveSource = secondarySource;
            
            Debug.Log("MusicManager: Audio sources created");
        }
        
        #endregion
        
        #region Playback Control
        
        /// <summary>
        /// Play a track by name
        /// </summary>
        public void PlayTrackByName(string trackName)
        {
            MusicTrack track = musicTracks.Find(t => t.trackName == trackName);
            if (track != null)
            {
                PlayTrack(track);
            }
            else
            {
                Debug.LogWarning($"MusicManager: Track '{trackName}' not found");
            }
        }
        
        /// <summary>
        /// Play a specific track
        /// </summary>
        public void PlayTrack(MusicTrack track)
        {
            if (track == null || track.audioClip == null)
            {
                Debug.LogWarning("MusicManager: Cannot play null track or track without audio clip");
                return;
            }
            
            // Don't restart if already playing
            if (currentTrack == track && activeSource.isPlaying)
            {
                Debug.Log($"MusicManager: Track '{track.trackName}' already playing");
                return;
            }
            
            // Stop current fade
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            fadeCoroutine = StartCoroutine(CrossfadeToTrack(track));
        }
        
        /// <summary>
        /// Play a random track from the list
        /// </summary>
        public void PlayRandomTrack()
        {
            if (musicTracks == null || musicTracks.Count == 0)
            {
                Debug.LogWarning("MusicManager: No tracks available for random play");
                return;
            }
            
            List<MusicTrack> availableTracks = new List<MusicTrack>(musicTracks);
            
            // Remove recently played tracks if avoiding repeats
            if (avoidRepeats && availableTracks.Count > maxRecentTracks)
            {
                foreach (var recentTrack in recentlyPlayedTracks)
                {
                    availableTracks.Remove(recentTrack);
                }
            }
            
            // Remove current track
            if (currentTrack != null)
            {
                availableTracks.Remove(currentTrack);
            }
            
            // Reset if no tracks available
            if (availableTracks.Count == 0)
            {
                availableTracks = new List<MusicTrack>(musicTracks);
                if (currentTrack != null)
                    availableTracks.Remove(currentTrack);
            }
            
            // Select random track
            if (availableTracks.Count > 0)
            {
                MusicTrack randomTrack = availableTracks[Random.Range(0, availableTracks.Count)];
                PlayTrack(randomTrack);
                
                // Update recently played list
                if (avoidRepeats)
                {
                    recentlyPlayedTracks.Add(randomTrack);
                    if (recentlyPlayedTracks.Count > maxRecentTracks)
                    {
                        recentlyPlayedTracks.RemoveAt(0);
                    }
                }
            }
        }
        
        /// <summary>
        /// Play next track in list
        /// </summary>
        public void PlayNextTrack()
        {
            if (enableRandomPlay)
            {
                PlayRandomTrack();
            }
            else if (musicTracks.Count > 0 && currentTrack != null)
            {
                int currentIndex = musicTracks.IndexOf(currentTrack);
                int nextIndex = (currentIndex + 1) % musicTracks.Count;
                PlayTrack(musicTracks[nextIndex]);
            }
        }
        
        /// <summary>
        /// Play previous track in list
        /// </summary>
        public void PlayPreviousTrack()
        {
            if (musicTracks.Count > 0 && currentTrack != null)
            {
                int currentIndex = musicTracks.IndexOf(currentTrack);
                int prevIndex = currentIndex - 1;
                if (prevIndex < 0) prevIndex = musicTracks.Count - 1;
                PlayTrack(musicTracks[prevIndex]);
            }
        }
        
        /// <summary>
        /// Stop music playback
        /// </summary>
        public void StopMusic(bool fade = true)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            if (fade)
            {
                StartCoroutine(FadeOut());
            }
            else
            {
                activeSource.Stop();
                activeSource.volume = 0f;
                currentTrack = null;
                isPlaying = false;
            }
        }
        
        /// <summary>
        /// Pause music
        /// </summary>
        public void PauseMusic()
        {
            if (activeSource.isPlaying)
            {
                activeSource.Pause();
                isPlaying = false;
                Debug.Log("MusicManager: Music paused");
            }
        }
        
        /// <summary>
        /// Resume music
        /// </summary>
        public void ResumeMusic()
        {
            if (!activeSource.isPlaying && currentTrack != null)
            {
                activeSource.UnPause();
                isPlaying = true;
                Debug.Log("MusicManager: Music resumed");
            }
        }
        
        #endregion
        
        #region Crossfade Logic
        
        private IEnumerator CrossfadeToTrack(MusicTrack newTrack)
        {
            isFading = true;
            
            Debug.Log($"MusicManager: Crossfading to '{newTrack.trackName}'");
            
            // Setup the inactive source with new track
            inactiveSource.clip = newTrack.audioClip;
            inactiveSource.volume = 0f;
            inactiveSource.loop = newTrack.loop;
            inactiveSource.Play();
            
            // Crossfade
            float elapsed = 0f;
            float startVolumeActive = activeSource.volume;
            float targetVolumeInactive = newTrack.volume * masterVolume;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                
                // Fade out active source
                activeSource.volume = Mathf.Lerp(startVolumeActive, 0f, t);
                
                // Fade in inactive source
                inactiveSource.volume = Mathf.Lerp(0f, targetVolumeInactive, t);
                
                yield return null;
            }
            
            // Stop the old source
            activeSource.Stop();
            activeSource.volume = 0f;
            
            // Swap sources
            AudioSource temp = activeSource;
            activeSource = inactiveSource;
            inactiveSource = temp;
            
            currentTrack = newTrack;
            isPlaying = true;
            isFading = false;
            
            Debug.Log($"MusicManager: Now playing '{newTrack.trackName}'");
        }
        
        private IEnumerator FadeOut()
        {
            Debug.Log("MusicManager: Fading out music");
            
            float elapsed = 0f;
            float startVolume = activeSource.volume;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                activeSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }
            
            activeSource.Stop();
            activeSource.volume = 0f;
            currentTrack = null;
            isPlaying = false;
        }
        
        #endregion
        
        #region Volume Control
        
        /// <summary>
        /// Set master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            
            if (currentTrack != null && !isFading)
            {
                activeSource.volume = currentTrack.volume * masterVolume;
            }
            
            // Save to PlayerPrefs
            PlayerPrefs.SetFloat("MusicVolume", masterVolume);
            PlayerPrefs.Save();
            
            Debug.Log($"MusicManager: Master volume set to {masterVolume}");
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Toggle random play mode
        /// </summary>
        public void ToggleRandomPlay()
        {
            enableRandomPlay = !enableRandomPlay;
            Debug.Log($"MusicManager: Random play {(enableRandomPlay ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Check if music is currently playing
        /// </summary>
        public bool IsPlaying()
        {
            return isPlaying && activeSource != null && activeSource.isPlaying;
        }
        
        /// <summary>
        /// Get current track name
        /// </summary>
        public string GetCurrentTrackName()
        {
            return currentTrack != null ? currentTrack.trackName : "None";
        }
        
        #endregion
    }
}
