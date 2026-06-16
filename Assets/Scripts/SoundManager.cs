using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip[] startleClips;
    public AudioClip neutralClip;

    [Header("Spatial Settings")]
    public float soundDistance = 2f; // distance en mètres autour de la tête

    private AudioSource audioSource;
    private Camera vrCamera;

    // Directions possibles
    public enum SoundDirection { Front, Back, Left, Right }

    // Résultat du dernier son joué (pour le logging)
    private string lastSoundType = "none";
    private string lastSoundDirection = "none";
    private float lastSoundTime = -1f;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 100% 3D
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 0.5f;
        audioSource.maxDistance = soundDistance * 2f;
        vrCamera = Camera.main;
    }

    /// <summary>
    /// Génère le plan de sons pour un bloc.
    /// Retourne une liste de (trialIndex, soundType, direction) ou null si pas de son.
    /// </summary>
    public List<TrialSound> GenerateBlockSoundPlan(List<bool> isTargetSequence)
    {
        // Identifier les indices target et non-target
        List<int> targetIndices = new List<int>();
        List<int> nonTargetIndices = new List<int>();

        for (int i = 0; i < isTargetSequence.Count; i++)
        {
            if (isTargetSequence[i]) targetIndices.Add(i);
            else nonTargetIndices.Add(i);
        }

        // Shuffler les deux listes
        Shuffle(targetIndices);
        Shuffle(nonTargetIndices);

        // Sélectionner 3 targets et 3 non-targets
        List<int> chosenTargets = targetIndices.GetRange(0, Mathf.Min(3, targetIndices.Count));
        List<int> chosenNonTargets = nonTargetIndices.GetRange(0, Mathf.Min(3, nonTargetIndices.Count));

        // Les 2 random : parmi les trials restants
        List<int> remaining = new List<int>();
        for (int i = 0; i < isTargetSequence.Count; i++)
        {
            if (!chosenTargets.Contains(i) && !chosenNonTargets.Contains(i))
                remaining.Add(i);
        }
        Shuffle(remaining);
        List<int> chosenRandom = remaining.GetRange(0, Mathf.Min(2, remaining.Count));

        // Assigner startle/neutre équitablement dans chaque catégorie
        // 3 avant target  : 2 startle + 1 neutre  (ou 1+2 — on alterne par bloc)
        // 3 avant nonTarget : idem inversé
        // 2 random : 1 startle + 1 neutre
        List<string> targetTypes = ShuffledTypes(new List<string> { "startle", "startle", "neutral" });
        List<string> nonTargetTypes = ShuffledTypes(new List<string> { "startle", "neutral", "neutral" });
        List<string> randomTypes = ShuffledTypes(new List<string> { "startle", "neutral" });

        // Générer 8 directions équilibrées (2 par direction)
        List<SoundDirection> directions = ShuffledDirections();

        // Assembler le plan
        List<TrialSound> plan = new List<TrialSound>();
        int dirIdx = 0;

        for (int i = 0; i < chosenTargets.Count; i++)
            plan.Add(new TrialSound(chosenTargets[i], targetTypes[i], directions[dirIdx++]));

        for (int i = 0; i < chosenNonTargets.Count; i++)
            plan.Add(new TrialSound(chosenNonTargets[i], nonTargetTypes[i], directions[dirIdx++]));

        for (int i = 0; i < chosenRandom.Count; i++)
            plan.Add(new TrialSound(chosenRandom[i], randomTypes[i], directions[dirIdx++]));

        return plan;
    }

    /// <summary>Joue le son associé à ce trial s'il en a un.</summary>
    public void PlayIfScheduled(int trialIndex, List<TrialSound> plan)
    {
        TrialSound scheduled = plan.Find(s => s.TrialIndex == trialIndex);
        if (scheduled == null)
        {
            lastSoundType = "none";
            lastSoundDirection = "none";
            lastSoundTime = -1f;
            return;
        }

        // Positionner l'AudioSource selon la direction relative à la tête
        Vector3 dir = GetDirectionVector(scheduled.Direction);
        audioSource.transform.position = vrCamera.transform.position + dir * soundDistance;

        // Jouer le bon clip
        AudioClip clip = scheduled.Type == "startle"
        ? startleClips[Random.Range(0, startleClips.Length)]
        : neutralClip;
        audioSource.PlayOneShot(clip);

        // Logguer
        lastSoundType = scheduled.Type;
        lastSoundDirection = scheduled.Direction.ToString();
        lastSoundTime = Time.time;
    }

    // Accesseurs pour DataManager
    public string GetLastSoundType() => lastSoundType;
    public string GetLastSoundDirection() => lastSoundDirection;
    public float GetLastSoundTime() => lastSoundTime;

    // Calcule le vecteur 3D relatif à la tête du joueur
    private Vector3 GetDirectionVector(SoundDirection dir)
    {
        Transform cam = vrCamera.transform;
        switch (dir)
        {
            case SoundDirection.Front: return cam.forward;
            case SoundDirection.Back: return -cam.forward;
            case SoundDirection.Left: return -cam.right;
            case SoundDirection.Right: return cam.right;
            default: return cam.forward;
        }
    }

    private List<SoundDirection> ShuffledDirections()
    {
        List<SoundDirection> dirs = new List<SoundDirection>
        {
            SoundDirection.Front, SoundDirection.Front,
            SoundDirection.Back,  SoundDirection.Back,
            SoundDirection.Left,  SoundDirection.Left,
            SoundDirection.Right, SoundDirection.Right
        };
        Shuffle(dirs);
        return dirs;
    }

    private List<string> ShuffledTypes(List<string> types)
    {
        List<string> copy = new List<string>(types);
        Shuffle(copy);
        return copy;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }
}

/// <summary>Représente un son planifié pour un trial donné.</summary>
public class TrialSound
{
    public int TrialIndex;
    public string Type;         // "startle" ou "neutral"
    public SoundManager.SoundDirection Direction;

    public TrialSound(int idx, string type, SoundManager.SoundDirection dir)
    {
        TrialIndex = idx;
        Type = type;
        Direction = dir;
    }
}