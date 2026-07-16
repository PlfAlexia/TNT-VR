using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip[] startleClips;
    public AudioClip neutralClip;

    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("Gamme de Shepard (jouée en boucle pendant les blocs réels uniquement)")]
    public AudioClip shepardClip; // clip représentant UN cycle complet de la gamme

    [Header("Spatial Settings")]
    public float soundDistance = 2f;

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float shepardVolume = 0.5f;

    private AudioSource sfxSource;
    private AudioSource musicSource;
    private AudioSource shepardSource;
    private Camera vrCamera;

    public enum SoundDirection { Front, Back, Left, Right }

    private string lastSoundType = "none";
    private string lastSoundDirection = "none";
    private float lastSoundTime = -1f;
    private string lastSoundName = "none";

    // --- État de la gamme de Shepard ---
    private bool shepardPlaying = false;
    private double shepardCycleStartDspTime = -1.0;
    private float shepardCycleDuration = 12f; // valeur par défaut, recalculée depuis shepardClip.length au démarrage
    private double shepardPauseStartDspTime = -1.0; // -1 = pas en pause

    void Awake()
    {
        // AudioSource pour les effets sonores (startles)
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode = AudioRolloffMode.Linear;
        sfxSource.minDistance = 0.5f;
        sfxSource.maxDistance = soundDistance * 2f;

        // AudioSource pour la musique de fond
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.spatialBlend = 0f; // Son 2D
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        // AudioSource dédiée à la gamme de Shepard (2D, en boucle, pilotée manuellement)
        shepardSource = gameObject.AddComponent<AudioSource>();
        shepardSource.spatialBlend = 0f;
        shepardSource.loop = true;
        shepardSource.playOnAwake = false;

        sfxSource.volume = sfxVolume;
        musicSource.volume = musicVolume;
        shepardSource.volume = shepardVolume;

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }

        vrCamera = Camera.main;
    }

    // =====================================================================
    // GAMME DE SHEPARD — boucle continue pendant les blocs réels
    // =====================================================================

    /// <summary>Démarre la gamme de Shepard en boucle continue (appelé une seule fois, au premier bloc réel).</summary>
    public void StartShepardLoop()
    {
        if (shepardClip == null)
        {
            Debug.LogError("[SoundManager] shepardClip non assigné dans l'inspecteur — impossible de démarrer la gamme de Shepard.");
            return;
        }

        shepardSource.clip = shepardClip;
        shepardSource.volume = shepardVolume;
        shepardSource.Play();

        shepardCycleDuration = shepardClip.length;
        shepardCycleStartDspTime = AudioSettings.dspTime;
        shepardPlaying = true;
    }

    /// <summary>Arrête la gamme de Shepard (appelé à la fin de l'expérience).</summary>
    public void StopShepardLoop()
    {
        if (!shepardPlaying) return;

        shepardSource.Stop();
        shepardPlaying = false;
        shepardCycleStartDspTime = -1.0;
        shepardPauseStartDspTime = -1.0;
    }

    /// <summary>
    /// Met en pause la gamme de Shepard (consignes, pauses/repos entre blocs) sans
    /// perdre la position de lecture ni désynchroniser le cycle utilisé pour les startles.
    /// Sans effet si la gamme n'est pas en cours de lecture.
    /// </summary>
    public void PauseShepardLoop()
    {
        if (!shepardPlaying) return;
        if (shepardPauseStartDspTime >= 0.0) return; // déjà en pause

        shepardSource.Pause();
        shepardPauseStartDspTime = AudioSettings.dspTime;
    }

    /// <summary>
    /// Reprend la gamme de Shepard après une pause, exactement où elle s'était arrêtée.
    /// Décale l'origine du cycle (shepardCycleStartDspTime) de la durée de la pause pour
    /// que GetTimeUntilNextCycleEnd() reste cohérent avec la position réelle de lecture.
    /// </summary>
    public void ResumeShepardLoop()
    {
        if (!shepardPlaying) return;
        if (shepardPauseStartDspTime < 0.0) return; // n'était pas en pause

        double pausedDuration = AudioSettings.dspTime - shepardPauseStartDspTime;
        shepardCycleStartDspTime += pausedDuration;
        shepardPauseStartDspTime = -1.0;

        shepardSource.UnPause();
    }

    /// <summary>Temps restant (en secondes) avant la fin du cycle de Shepard actuellement en cours.</summary>
    private float GetTimeUntilNextCycleEnd()
    {
        if (!shepardPlaying)
        {
            Debug.LogWarning("[SoundManager] Gamme de Shepard non active — le startle sera joué immédiatement.");
            return 0f;
        }

        double elapsed = AudioSettings.dspTime - shepardCycleStartDspTime;
        double elapsedInCycle = elapsed % shepardCycleDuration;
        double remaining = shepardCycleDuration - elapsedInCycle;

        // Évite un redémarrage quasi immédiat (arrondi flottant) qui reviendrait à jouer en plein cycle
        if (remaining < 0.01) remaining = shepardCycleDuration;

        return (float)remaining;
    }

    // =====================================================================
    // STARTLES — armés à un trial donné, mais joués uniquement en fin de cycle
    // =====================================================================

    /// <summary>
    /// "Arme" un startle planifié : il ne sera réellement joué qu'à la toute fin
    /// du cycle de Shepard en cours (jamais en plein milieu). Ne bloque pas
    /// l'expérience : le déroulement des trials continue normalement pendant l'attente.
    /// </summary>
    public void RequestStartle(TrialSound sound)
    {
        StartCoroutine(PlayStartleAtCycleEndRoutine(sound));
    }

    private IEnumerator PlayStartleAtCycleEndRoutine(TrialSound sound)
    {
        float waitTime = GetTimeUntilNextCycleEnd();
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        PlaySoundNow(sound);
    }

    private void PlaySoundNow(TrialSound sound)
    {
        Vector3 dir = GetDirectionVector(sound.Direction);
        sfxSource.transform.position = vrCamera.transform.position + dir * soundDistance;

        AudioClip clip;
        if (sound.Type == "startle")
        {
            if (startleClips == null || startleClips.Length == 0)
            {
                Debug.LogError("[SoundManager] startleClips est vide ou non assigné dans l'inspecteur — impossible de jouer le son startle.");
                return;
            }
            clip = startleClips[Random.Range(0, startleClips.Length)];
        }
        else
        {
            clip = neutralClip;
        }

        sfxSource.PlayOneShot(clip);

        lastSoundType = sound.Type;
        lastSoundDirection = sound.Direction.ToString();
        lastSoundTime = Time.time;
        lastSoundName = clip != null ? clip.name : "none";

        // Coupe la gamme de Shepard dès qu'un startle est joué ; elle reste en pause
        // jusqu'à la fin du bloc en cours et ne reprendra qu'au bloc suivant (cf.
        // ExperimentManager, qui appelle ResumeShepardLoop() au tout début de
        // chaque nouveau bloc).
        if (sound.Type == "startle")
        {
            PauseShepardLoop();
        }
    }

    /// <summary>
    /// Récupère les infos du dernier son joué ET les remet à "none" dans la foulée.
    /// Garantit qu'un événement sonore n'est reporté qu'une seule fois dans le CSV
    /// (sur le trial pendant lequel il a réellement été joué), au lieu de rester
    /// affiché indéfiniment sur tous les trials suivants.
    /// </summary>
    public (string type, string direction, float time, string name) ConsumeLastSound()
    {
        var result = (lastSoundType, lastSoundDirection, lastSoundTime, lastSoundName);
        lastSoundType = "none";
        lastSoundDirection = "none";
        lastSoundTime = -1f;
        lastSoundName = "none";
        return result;
    }

    private Vector3 GetDirectionVector(SoundDirection dir)
    {
        Transform cam = vrCamera.transform;

        switch (dir)
        {
            case SoundDirection.Front:
                return cam.forward;
            case SoundDirection.Back:
                return -cam.forward;
            case SoundDirection.Left:
                return -cam.right;
            case SoundDirection.Right:
                return cam.right;
            default:
                return cam.forward;
        }
    }
}

public class TrialSound
{
    public int TrialIndex;
    public string Type;
    public SoundManager.SoundDirection Direction;

    public TrialSound(int idx, string type, SoundManager.SoundDirection dir)
    {
        TrialIndex = idx;
        Type = type;
        Direction = dir;
    }
}