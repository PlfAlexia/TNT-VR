using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class HeadMotionTracker : MonoBehaviour
{
    [Header("R�f�rence cam�ra (t�te du participant)")]
    public Camera vrCamera;

    private const float SAMPLE_RATE = 60f; // Hz
    private const int SMOOTHING_WINDOW = 5; // taille de la fen�tre pour la moyenne glissante de l'acc�l�ration

    private float sampleInterval;
    private float timeSinceLastSample = 0f;

    private StreamWriter writer;
    private string filePath;

    // t0 commun avec ExperimentManager (Time.time au lancement de l'experience).
    // Tant que ExperimentManager n'a pas appele SetStartTime, on retombe sur Time.time
    // pour ne jamais planter, mais les tout premiers echantillons (avant le Start()
    // d'ExperimentManager) resteront non cales sur t0.
    private float startTime = -1f;

    private Vector3 lastPosition;
    private Vector3 lastVelocity;
    private bool hasLastPosition = false;
    private bool hasLastVelocity = false;

    // Permet � ExperimentManager d'indiquer le bloc et l'essai courants
    private int currentBlockIndex = -1;
    private int currentTrialIndex = -1;

    // Fen�tre glissante utilis�e pour lisser l'acc�l�ration brute (bruit de tracking � 60Hz)
    private Queue<float> accelerationWindow = new Queue<float>();

    void Awake()
    {
        sampleInterval = 1f / SAMPLE_RATE;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"headmotion_{timestamp}.csv";
        filePath = Path.Combine(Application.persistentDataPath, fileName);
        writer = new StreamWriter(filePath, append: false);

        writer.WriteLine("time_ms,block_index,trial_index,pos_x,pos_y,pos_z,acceleration_raw,acceleration_smoothed");
        writer.Flush();

        Debug.Log($"[HeadMotionTracker] Fichier CSV cr�� : {filePath}");
    }

    void Update()
    {
        timeSinceLastSample += Time.deltaTime;
        if (timeSinceLastSample < sampleInterval) return;

        float dt = timeSinceLastSample;
        timeSinceLastSample = 0f;

        Vector3 currentPosition = vrCamera.transform.position;
        float accelerationRaw = 0f;

        if (hasLastPosition)
        {
            Vector3 currentVelocity = (currentPosition - lastPosition) / dt;

            if (hasLastVelocity)
            {
                Vector3 currentAcceleration = (currentVelocity - lastVelocity) / dt;
                accelerationRaw = currentAcceleration.magnitude;
            }

            lastVelocity = currentVelocity;
            hasLastVelocity = true;
        }

        lastPosition = currentPosition;
        hasLastPosition = true;

        float accelerationSmoothed = ComputeSmoothedAcceleration(accelerationRaw);

        // t0 = lancement de l'experience (fixe par ExperimentManager via SetStartTime).
        // Si SetStartTime n'a jamais ete appele (ex. tracker utilise hors contexte
        // d'ExperimentManager), on se rabat sur Time.time comme avant, en secondes -> ms.
        float t0 = startTime >= 0f ? startTime : 0f;
        float timeMs = (Time.time - t0) * 1000f;

        string line = string.Join(",",
            timeMs.ToString("F1", CultureInfo.InvariantCulture),
            currentBlockIndex,
            currentTrialIndex,
            currentPosition.x.ToString("F4", CultureInfo.InvariantCulture),
            currentPosition.y.ToString("F4", CultureInfo.InvariantCulture),
            currentPosition.z.ToString("F4", CultureInfo.InvariantCulture),
            accelerationRaw.ToString("F4", CultureInfo.InvariantCulture),
            accelerationSmoothed.ToString("F4", CultureInfo.InvariantCulture)
        );
        writer.WriteLine(line);
    }

    /// <summary>
    /// Moyenne glissante sur les SMOOTHING_WINDOW derniers �chantillons d'acc�l�ration.
    /// La d�riv�e seconde brute (position -> vitesse -> acc�l�ration) amplifie fortement
    /// le bruit de tracking ; ce lissage donne un signal plus exploitable pour d�tecter
    /// un sursaut, tout en conservant la valeur brute dans le CSV pour ne rien perdre.
    /// </summary>
    private float ComputeSmoothedAcceleration(float newSample)
    {
        accelerationWindow.Enqueue(newSample);
        if (accelerationWindow.Count > SMOOTHING_WINDOW)
            accelerationWindow.Dequeue();

        float sum = 0f;
        foreach (float value in accelerationWindow)
            sum += value;

        return sum / accelerationWindow.Count;
    }

    /// <summary>
    /// Appel� par ExperimentManager (une seule fois, dans Start(), juste apr�s avoir
    /// fix� experimentStartTime) pour synchroniser t0 entre headmotion.csv et nback.csv.
    /// Les deux fichiers auront ainsi exactement la m�me origine temporelle, en ms.
    /// </summary>
    public void SetStartTime(float t0)
    {
        startTime = t0;
    }

    /// <summary>Appel� par ExperimentManager pour indiquer le bloc en cours.</summary>
    public void SetCurrentBlock(int blockIndex)
    {
        currentBlockIndex = blockIndex;
    }

    /// <summary>
    /// Appel� par ExperimentManager pour indiquer l'essai en cours (Option B).
    /// Permet de fusionner headmotion.csv et nback.csv en post-traitement sur
    /// (block_index, trial_index) et d'aligner pr�cis�ment les fen�tres temporelles
    /// autour de chaque stimulus/son.
    /// </summary>
    public void SetCurrentTrial(int trialIndex)
    {
        currentTrialIndex = trialIndex;
    }

    public void CloseFile()
    {
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
            Debug.Log($"[HeadMotionTracker] Fichier CSV ferm� : {filePath}");
        }
    }

    void OnDestroy()
    {
        CloseFile();
    }
}