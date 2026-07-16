using UnityEngine;
using TMPro;

public class StimulusManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI textOperation;
    public TextMeshProUGUI textInstruction;
    public TextMeshProUGUI textTimer;

    void Awake()
    {
        ClearAll();
    }

    public void ShowStimulus(string operation)
    {
        textInstruction.gameObject.SetActive(false);
        textOperation.gameObject.SetActive(true);
        textOperation.text = operation;
    }

    public void HideStimulus()
    {
        textOperation.gameObject.SetActive(false);
    }

    public void ShowFixation()
    {
        textOperation.gameObject.SetActive(true);
        textOperation.text = "X";
    }

    public void HideFixation()
    {
        textOperation.gameObject.SetActive(false);
    }

    public void ShowRest(int secondsRemaining)
    {
        textOperation.gameObject.SetActive(true);
        textOperation.text = "00+00";

        textTimer.gameObject.SetActive(true);
        textTimer.text = secondsRemaining.ToString();
    }

    public void HideRest()
    {
        textOperation.gameObject.SetActive(false);
        textTimer.gameObject.SetActive(false);
    }

    public void ShowInstruction(string message)
    {
        textOperation.gameObject.SetActive(false);
        textInstruction.gameObject.SetActive(true);
        textInstruction.text = message;
    }

    public void HideInstruction()
    {
        textInstruction.gameObject.SetActive(false);
    }

    public void ShowEnd()
    {
        ClearAll();
        textInstruction.gameObject.SetActive(true);
        textInstruction.text = "Expérience terminée.\nMerci pour votre participation.";
    }

    private void ClearAll()
    {
        if (textOperation != null) textOperation.gameObject.SetActive(false);
        if (textInstruction != null) textInstruction.gameObject.SetActive(false);
        if (textTimer != null) textTimer.gameObject.SetActive(false);
    }
}