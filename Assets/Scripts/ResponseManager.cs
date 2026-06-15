using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class ResponseManager : MonoBehaviour
{
    // État de la réponse courante
    private string currentResponse = "none";
    private float responseTime = 0f;
    private bool hasResponded = false;

    // Devices XR
    private InputDevice rightController;
    private InputDevice leftController;

    // Seuil de déclenchement des gâchettes (0–1)
    private const float TRIGGER_THRESHOLD = 0.5f;

    // États précédents pour détecter le front montant
    private float prevRightTrigger = 0f;
    private float prevLeftTrigger = 0f;

    // FIX : prevAnyButton déplacé ici (hors de ReadConfirmButton) pour qu'il
    // persiste correctement entre les frames sans risque de réinitialisation.
    private bool prevAnyButton = false;

    // FIX : confirmPressed déclaré au niveau de la classe (était déjà le cas)
    // mais on s'assure qu'il n'est écrit QUE dans ReadConfirmButton()
    // et lu/consommé QUE dans SpacePressed().
    private bool confirmPressed = false;

    // Initialisation
    void Start()
    {
        TryGetControllers();
    }

    void Update()
    {
        // Re-tenter si les controllers ne sont pas encore connectés
        if (!rightController.isValid || !leftController.isValid)
            TryGetControllers();

        // FIX : ReadConfirmButton() est appelé TOUJOURS, indépendamment de
        // hasResponded, et AVANT ReadInputs() pour éviter tout conflit d'ordre.
        ReadConfirmButton();

        if (!hasResponded)
            ReadInputs();
    }

    // Récupération des controllers
    private void TryGetControllers()
    {
        var rightDevices = new List<InputDevice>();
        var leftDevices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            rightDevices);

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            leftDevices);

        if (rightDevices.Count > 0) rightController = rightDevices[0];
        if (leftDevices.Count > 0) leftController = leftDevices[0];
    }

    // Lecture des gâchettes uniquement (réponses trial)
    private void ReadInputs()
    {
        // Gâchette droite
        float rightTrigger = 0f;
        rightController.TryGetFeatureValue(CommonUsages.trigger, out rightTrigger);

        if (rightTrigger >= TRIGGER_THRESHOLD && prevRightTrigger < TRIGGER_THRESHOLD)
            RegisterResponse("right");

        prevRightTrigger = rightTrigger;

        // Gâchette gauche
        float leftTrigger = 0f;
        leftController.TryGetFeatureValue(CommonUsages.trigger, out leftTrigger);

        if (leftTrigger >= TRIGGER_THRESHOLD && prevLeftTrigger < TRIGGER_THRESHOLD)
            RegisterResponse("left");

        prevLeftTrigger = leftTrigger;

        // Fallback clavier PC (tests en Editor)
        if (Input.GetKeyDown(KeyCode.RightArrow)) RegisterResponse("right");
        if (Input.GetKeyDown(KeyCode.LeftArrow)) RegisterResponse("left");
    }

    // FIX : ReadConfirmButton() ne touche QUE confirmPressed.
    // Il n'interagit plus avec hasResponded ni avec les variables de gâchettes.
    // La détection de front montant est correcte : prevAnyButton est un champ
    // de classe persistant, pas une variable locale recréée à chaque appel.
    private void ReadConfirmButton()
    {
        bool rightA = false;
        bool leftX = false;
        rightController.TryGetFeatureValue(CommonUsages.primaryButton, out rightA);
        leftController.TryGetFeatureValue(CommonUsages.primaryButton, out leftX);

        // FIX : Input.GetKey (et non GetKeyDown) pour la cohérence avec la
        // logique de front montant gérée manuellement via prevAnyButton.
        bool anyButton = rightA || leftX || Input.GetKey(KeyCode.Space);

        if (anyButton && !prevAnyButton)
        {
            // On pose le flag ; seul SpacePressed() est autorisé à le consommer.
            confirmPressed = true;
        }

        prevAnyButton = anyButton;
    }

    // API publique

    /// <summary>Enregistre la première réponse reçue pendant un trial.</summary>
    private void RegisterResponse(string side)
    {
        if (hasResponded) return;
        currentResponse = side;
        responseTime = Time.time;
        hasResponded = true;
    }

    /// <summary>Réinitialise avant chaque nouveau trial.</summary>
    public void ResetResponse()
    {
        currentResponse = "none";
        responseTime = 0f;
        hasResponded = false;
        // FIX : on ne remet PAS confirmPressed à false ici — une pression
        // intervenue juste avant ResetResponse() ne doit pas être perdue.
    }

    /// <summary>Retourne "right", "left" ou "none".</summary>
    public string GetResponse() => currentResponse;

    /// <summary>Retourne le timestamp absolu de la réponse (Time.time).</summary>
    public float GetRT() => responseTime;

    /// <summary>Vrai si une réponse a été enregistrée pendant ce trial.</summary>
    public bool HasResponded() => hasResponded;

    /// <summary>
    /// Retourne true une seule fois par pression du bouton de confirmation
    /// (bouton A/X ou Espace). Consomme le flag.
    /// </summary>
    public bool SpacePressed()
    {
        if (!confirmPressed) return false;
        confirmPressed = false;
        return true;
    }

    /// <summary>Vide le flag de confirmation (appeler avant ShowInstruction).</summary>
    public void ClearConfirm()
    {
        confirmPressed = false;
    }
}