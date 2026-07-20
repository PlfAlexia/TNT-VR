using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class HeadMotionTracker : MonoBehaviour
{
    [Header("R�f�rence cam�ra (t�te du participant)")]
    public Camera vrCamera;

    private const float SAMPLE_RATE = 60f;
    private const int SMOOTHING_WINDOW = 5;

    private float sampleInterval;
    private float timeSinceLastSample = 0f;

    private StreamWriter writer;
    private string filePath;

    private float startTime = -1f;

    private Vector3 lastPosition;
    private Vector3 lastVelocity;
    private bool hasLastPosition = false;
    private bool hasLastVelocity = false;

    private int currentBlockIndex = -1;
    private int currentTrialIndex = -1;

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

        Debug.Log($"[HeadMotionTracker] Fichier CSV crée : {filePath}");
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

    public void SetStartTime(float t0)
    {
        startTime = t0;
    }

    public void SetCurrentBlock(int blockIndex)
    {
        currentBlockIndex = blockIndex;
    }

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