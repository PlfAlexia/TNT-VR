using System;
using System.IO;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    private StreamWriter writer;
    private string filePath;

    void Awake()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"nback_{timestamp}.csv";
        filePath = Path.Combine(Application.persistentDataPath, fileName);
        writer = new StreamWriter(filePath, append: false);

        writer.WriteLine(
            "experiment_time_ms," +
            "block_index," +
            "trial_index," +
            "n_back," +
            "operation," +
            "is_target," +
            "response," +
            "rt_ms," +
            "correct," +
            "sound_type," +
            "sound_name," +
            "sound_direction," +
            "sound_time_ms"
        );
        writer.Flush();
        Debug.Log($"[DataManager] Fichier CSV créé : {filePath}");
    }

    public void SaveTrial(
        int blockIndex,
        int nBack,
        int trialIndex,
        string operation,
        bool isTarget,
        string response,
        float rt,
        int correct,
        string soundType,
        string soundName,
        string soundDirection,
        float soundTime,
        float experimentTime)
    {
        string rtStr = (response == "none" || rt < 0f)
            ? "NaN"
            : Mathf.RoundToInt(rt * 1000f).ToString();
        string soundTimeStr = soundTime < 0f
            ? "NaN"
            : Mathf.RoundToInt(soundTime * 1000f).ToString();
        string experimentTimeStr = Mathf.RoundToInt(experimentTime * 1000f).ToString();

        string line = string.Join(",",
            experimentTimeStr,
            blockIndex,
            trialIndex,
            nBack,
            $"\"{operation}\"",
            isTarget ? "1" : "0",
            response,
            rtStr,
            correct,
            soundType,
            soundName,
            soundDirection,
            soundTimeStr
        );
        writer.WriteLine(line);
        writer.Flush();
    }

    public void CloseFile()
    {
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
            Debug.Log($"[DataManager] Fichier CSV fermé : {filePath}");
        }
    }

    void OnDestroy()
    {
        CloseFile();
    }
}