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

    private bool shepardPlaying = false;

    // t0 commun avec ExperimentManager / HeadMotionTracker (Time.time au lancement de
    // l'expérience). Tant que ExperimentManager n'a pas appelé SetStartTime, on retombe
    // sur Time.time brut (comportement précédent) pour ne jamais planter.
    private float startTime = -1f;

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

    /// Appelé par ExperimentManager (même t0 que HeadMotionTracker.SetStartTime) pour que
    /// sound_time_ms soit exprimé dans le même référentiel que experiment_time_ms.
    public void SetStartTime(float t0)
    {
        startTime = t0;
    }

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
        shepardPlaying = true;
    }

    public void StopShepardLoop()
    {
        if (!shepardPlaying) return;

        shepardSource.Stop();
        shepardPlaying = false;
    }

    public void PauseShepardLoop()
    {
        if (!shepardPlaying) return;
        shepardSource.Pause();
    }

    public void ResumeShepardLoop()
    {
        if (!shepardPlaying) return;
        shepardSource.UnPause();
    }

    private float GetTimeUntilNextCycleEnd()
    {
        if (!shepardPlaying)
        {
            Debug.LogWarning("[SoundManager] Gamme de Shepard non active — le startle sera joué immédiatement.");
            return 0f;
        }

        float remaining = shepardClip.length - shepardSource.time;

        if (remaining < 0.01f) remaining = shepardClip.length;

        return remaining;
    }


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
                Debug.LogError("[SoundManager] startleClips est vide ou non assigné dans l'inspecteur, impossible de jouer le son startle.");
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
        lastSoundTime = startTime >= 0f ? (Time.time - startTime) : Time.time;
        lastSoundName = clip != null ? clip.name : "none";

        if (sound.Type == "startle")
        {
            PauseShepardLoop();
        }
    }

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