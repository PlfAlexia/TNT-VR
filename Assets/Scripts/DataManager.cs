using System;
using System.IO;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    // ?? Fichier CSV ????????????????????????????????????????????????????????????
    private StreamWriter writer;
    private string filePath;

    // ?? Initialisation ?????????????????????????????????????????????????????????
    void Awake()
    {
        // Nom de fichier horodaté pour éviter les écrasements
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"nback_{timestamp}.csv";
        filePath = Path.Combine(Application.persistentDataPath, fileName);

        writer = new StreamWriter(filePath, append: false);

        // En-tête CSV
        writer.WriteLine(
            "block_index," +
            "n_back," +
            "trial_index," +
            "operation," +
            "is_target," +
            "response," +
            "rt_ms," +
            "correct"
        );
        writer.Flush();

        Debug.Log($"[DataManager] Fichier CSV créé : {filePath}");
    }

    // ?? Enregistrement d'un trial ?????????????????????????????????????????????
    /// <summary>
    /// Sauvegarde les données d'un trial dans le CSV.
    /// rt est un temps absolu (Time.time) — on le stocke en ms relatif au début du stimulus.
    /// </summary>
    public void SaveTrial(
        int blockIndex,
        int nBack,
        int trialIndex,
        string operation,
        bool isTarget,
        string response,
        float rt,
        int correct)
    {
        // rt < 0 ou "none" ? pas de réponse dans la fenêtre
        string rtStr = (response == "none" || rt < 0f)
            ? "NaN"
            : Mathf.RoundToInt(rt * 1000f).ToString();

        string line = string.Join(",",
            blockIndex,
            nBack,
            trialIndex,
            $"\"{operation}\"",   // guillemets pour les espaces dans l'opération
            isTarget ? "1" : "0",
            response,
            rtStr,
            correct
        );

        writer.WriteLine(line);
        writer.Flush();
    }

    // ?? Fermeture propre ???????????????????????????????????????????????????????
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

    // ?? Sécurité si l'objet est détruit sans CloseFile() ??????????????????????
    void OnDestroy()
    {
        CloseFile();
    }
}