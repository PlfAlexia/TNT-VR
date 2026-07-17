using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentManager : MonoBehaviour
{
    private const int TRIALS_PAR_BLOC = 12;
    private const float DUREE_STIMULUS = 2f;
    private const float DUREE_FIXATION = 1f;
    private const float DUREE_REPOS = 18f;

    // Index du premier bloc réel (0 et 1 sont les blocs de test) : c'est ici que
    // la gamme de Shepard démarre, et elle tourne en continu jusqu'à la toute fin.
    private const int FIRST_REAL_BLOCK_INDEX = 2;

    // =====================================================================
    // MOMENTS D'APPARITION DES STARTLES — modifiables ici facilement
    // Format : (blockIndex, trialIndex)
    // Blocs : 0 = test 1-back, 1 = test 2-back, 2-9 = 1-back, 10-17 = 2-back
    // trialIndex : 0 à 11 (éviter 0 et 11 = premier et dernier du bloc)
    // =====================================================================
    private static readonly (int blockIdx, int trialIdx, string direction)[] STARTLE_SCHEDULE =
    {
        (3,  5, ""),   // (avant ou arrière, 1-back) — direction tirée aléatoirement
        (6,  7, ""),   // (avant ou arrière, 1-back)
        (11, 4, ""),   // (avant ou arrière, 2-back)
        (15, 8, ""),   // (avant ou arrière, 2-back)
    };
    // =====================================================================

    public StimulusManager stimulusManager;
    public ResponseManager responseManager;
    public DataManager dataManager;
    public SoundManager soundManager;
    public HeadMotionTracker headMotionTracker;

    private List<Block> allBlocks;
    private int currentBlockIdx = 0;
    private int currentTrialIdx = 0;
    private int currentN = 0;
    private float experimentStartTime = -1f;

    // Instructions aux blocs 0, 1 (tests), 2 (premier 1-back), 10 (premier 2-back)
    private HashSet<int> instructionBlocks = new HashSet<int> { 0, 1, 2, 10 };

    // Plan de sons : (blockIdx, trialIdx) → TrialSound
    private Dictionary<(int, int), TrialSound> soundSchedule;

    public class Block
    {
        public int N;
        public List<Stimulus> Sequence;

        public Block(int n, List<Stimulus> sequence)
        {
            N = n;
            Sequence = sequence;
        }
    }

    void Start()
    {
        experimentStartTime = Time.time; // ← t0 = lancement Unity
        headMotionTracker.SetStartTime(experimentStartTime); // même t0 pour headmotion.csv et nback.csv
        soundManager.SetStartTime(experimentStartTime); // même t0 pour sound_time_ms (nback.csv)
        allBlocks = GenerateAllBlocks();
        soundSchedule = GenerateSoundSchedule();
        StartCoroutine(RunExperiment());
    }

    // Structure : 1 test 1-back, 1 test 2-back, 8x 1-back, 8x 2-back
    private List<Block> GenerateAllBlocks()
    {
        List<Block> blocks = new List<Block>();

        // Bloc test 1-back
        List<Stimulus> testVariant1 = StimuliData.Variants1Back[Random.Range(0, StimuliData.Variants1Back.Count)];
        blocks.Add(new Block(1, RandomizeNBack(testVariant1, 1)));

        // Bloc test 2-back
        List<Stimulus> testVariant2 = StimuliData.Variants2Back[Random.Range(0, StimuliData.Variants2Back.Count)];
        blocks.Add(new Block(2, RandomizeNBack(testVariant2, 2)));

        // 8 blocs principaux 1-back
        for (int i = 0; i < 8; i++)
        {
            List<Stimulus> variant = StimuliData.Variants1Back[Random.Range(0, StimuliData.Variants1Back.Count)];
            blocks.Add(new Block(1, RandomizeNBack(variant, 1)));
        }

        // 8 blocs principaux 2-back
        for (int i = 0; i < 8; i++)
        {
            List<Stimulus> variant = StimuliData.Variants2Back[Random.Range(0, StimuliData.Variants2Back.Count)];
            blocks.Add(new Block(2, RandomizeNBack(variant, 2)));
        }

        return blocks;
    }

    // Génère le plan des 4 startles avec directions aléatoires avant/arrière
    // en respectant la contrainte : 2 startles sur des blocs 1-back (avant + arrière)
    // et 2 startles sur des blocs 2-back (avant + arrière), ordre aléatoire dans chaque paire
    private Dictionary<(int, int), TrialSound> GenerateSoundSchedule()
    {
        // Paire 1-back : avant/arrière dans un ordre aléatoire
        List<SoundManager.SoundDirection> dirs1Back = new List<SoundManager.SoundDirection>
            { SoundManager.SoundDirection.Front, SoundManager.SoundDirection.Back };
        ShuffleDirections(dirs1Back);

        // Paire 2-back : avant/arrière dans un ordre aléatoire
        List<SoundManager.SoundDirection> dirs2Back = new List<SoundManager.SoundDirection>
            { SoundManager.SoundDirection.Front, SoundManager.SoundDirection.Back };
        ShuffleDirections(dirs2Back);

        // On récupère les 4 entrées du STARTLE_SCHEDULE et on leur assigne les directions
        // Les 2 premières entrées sont les 1-back, les 2 dernières les 2-back
        Dictionary<(int, int), TrialSound> schedule = new Dictionary<(int, int), TrialSound>();

        schedule[(STARTLE_SCHEDULE[0].blockIdx, STARTLE_SCHEDULE[0].trialIdx)] =
            new TrialSound(STARTLE_SCHEDULE[0].trialIdx, "startle", dirs1Back[0]);

        schedule[(STARTLE_SCHEDULE[1].blockIdx, STARTLE_SCHEDULE[1].trialIdx)] =
            new TrialSound(STARTLE_SCHEDULE[1].trialIdx, "startle", dirs1Back[1]);

        schedule[(STARTLE_SCHEDULE[2].blockIdx, STARTLE_SCHEDULE[2].trialIdx)] =
            new TrialSound(STARTLE_SCHEDULE[2].trialIdx, "startle", dirs2Back[0]);

        schedule[(STARTLE_SCHEDULE[3].blockIdx, STARTLE_SCHEDULE[3].trialIdx)] =
            new TrialSound(STARTLE_SCHEDULE[3].trialIdx, "startle", dirs2Back[1]);

        return schedule;
    }

    private void ShuffleDirections(List<SoundManager.SoundDirection> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            SoundManager.SoundDirection tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private List<Stimulus> RandomizeNBack(List<Stimulus> stimuli, int n)
    {
        List<Stimulus> targets = new List<Stimulus>();
        List<Stimulus> distractors = new List<Stimulus>();
        List<bool> pattern = new List<bool>();

        foreach (Stimulus s in stimuli)
        {
            if (s.IsTarget) targets.Add(s);
            else distractors.Add(s);
            pattern.Add(s.IsTarget);
        }

        for (int attempt = 0; attempt < 1000; attempt++)
        {
            Shuffle(targets);
            Shuffle(distractors);

            List<Stimulus> tgtPool = new List<Stimulus>(targets);
            List<Stimulus> distPool = new List<Stimulus>(distractors);
            List<Stimulus> sequence = new List<Stimulus>();
            List<Stimulus> presented = new List<Stimulus>();
            bool valid = true;

            for (int i = 0; i < pattern.Count; i++)
            {
                Stimulus candidate;

                if (pattern[i])
                {
                    candidate = tgtPool[0];
                    tgtPool.RemoveAt(0);
                }
                else
                {
                    candidate = distPool[0];
                    distPool.RemoveAt(0);

                    if (presented.Count >= n &&
                        candidate.Result == presented[presented.Count - n].Result)
                    {
                        valid = false;
                        break;
                    }
                }

                sequence.Add(candidate);
                presented.Add(candidate);
            }

            if (valid) return sequence;
        }

        Debug.LogError("Impossible de générer une séquence valide après 1000 tentatives.");
        return stimuli;
    }

    private void Shuffle(List<Stimulus> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Stimulus temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private IEnumerator RunExperiment()
    {

        for (currentBlockIdx = 0; currentBlockIdx < allBlocks.Count; currentBlockIdx++)
        {
            Block block = allBlocks[currentBlockIdx];
            currentN = block.N;

            headMotionTracker.SetCurrentBlock(currentBlockIdx);

            // La gamme de Shepard démarre au premier bloc réel et tourne en continu
            // jusqu'à la fin de l'expérience (jamais pendant les blocs de test 0 et 1).
            if (currentBlockIdx == FIRST_REAL_BLOCK_INDEX)
                soundManager.StartShepardLoop();

            // Si un startle a été joué pendant le bloc précédent, la gamme de Shepard
            // est restée en pause jusqu'à la fin de ce bloc-là ; on la reprend ici, au
            // tout début du nouveau bloc (sans effet si elle n'était pas en pause, ou
            // si elle n'a pas encore démarré).
            soundManager.ResumeShepardLoop();

            // Récupère le son planifié pour ce bloc (s'il y en a un)
            List<TrialSound> soundPlan = new List<TrialSound>();
            for (int t = 0; t < TRIALS_PAR_BLOC; t++)
            {
                if (soundSchedule.ContainsKey((currentBlockIdx, t)))
                    soundPlan.Add(soundSchedule[(currentBlockIdx, t)]);
            }

            if (instructionBlocks.Contains(currentBlockIdx))
            {
                // Pendant les consignes (et la fixation qui suit), aucun trial n'est en cours :
                // on force trial_index = -1 dans headmotion.csv pour ne pas laisser traîner
                // l'index du dernier trial du bloc précédent, ce qui prêterait à confusion.
                headMotionTracker.SetCurrentTrial(-1);

                soundManager.PauseShepardLoop();

                responseManager.ClearConfirm();
                stimulusManager.ShowInstruction(GetInstruction(currentBlockIdx, currentN));
                yield return new WaitUntil(() => responseManager.SpacePressed());
                stimulusManager.HideInstruction();

                stimulusManager.ShowFixation();
                yield return new WaitForSeconds(DUREE_FIXATION);
                stimulusManager.HideFixation();

                soundManager.ResumeShepardLoop();
            }

            for (currentTrialIdx = 0; currentTrialIdx < TRIALS_PAR_BLOC; currentTrialIdx++)
            {
                // Si un startle est planifié pour cet essai, on l'arme : il ne sera
                // effectivement joué qu'à la fin du cycle de Shepard en cours (jamais
                // en plein milieu). L'appel n'est pas bloquant, les trials continuent
                // à s'enchaîner normalement pendant que le startle attend son tour.
                TrialSound scheduledSound = soundPlan.Find(s => s.TrialIndex == currentTrialIdx);
                if (scheduledSound != null)
                    soundManager.RequestStartle(scheduledSound);

                Stimulus stimulus = block.Sequence[currentTrialIdx];
                bool isTargetActual = IsRealNBackMatch(block.Sequence, currentTrialIdx, currentN);
                yield return StartCoroutine(RunTrial(stimulus, isTargetActual));

                if (currentTrialIdx < TRIALS_PAR_BLOC - 1)
                {
                    stimulusManager.ShowFixation();
                    yield return new WaitForSeconds(DUREE_FIXATION);
                    stimulusManager.HideFixation();
                }
            }

            // Repos après les blocs principaux (pas après les 2 blocs de test)
            if (currentBlockIdx < allBlocks.Count - 1 && currentBlockIdx != 0 && currentBlockIdx != 1)
            {
                soundManager.PauseShepardLoop();
                yield return StartCoroutine(RunRestCountdown());
                soundManager.ResumeShepardLoop();

                stimulusManager.ShowFixation();
                yield return new WaitForSeconds(DUREE_FIXATION);
                stimulusManager.HideFixation();
            }
        }

        soundManager.StopShepardLoop();
        stimulusManager.ShowEnd();
        headMotionTracker.CloseFile();
        dataManager.CloseFile();
    }

    private IEnumerator RunRestCountdown()
    {
        int secondsRemaining = Mathf.CeilToInt(DUREE_REPOS);
        while (secondsRemaining > 0)
        {
            stimulusManager.ShowRest(secondsRemaining);
            yield return new WaitForSeconds(1f);
            secondsRemaining--;
        }
        stimulusManager.HideRest();
    }

    private IEnumerator RunTrial(Stimulus stimulus, bool isTarget)
    {
        // Synchronise HeadMotionTracker sur l'essai courant (Option B : jointure via block_index + trial_index)
        headMotionTracker.SetCurrentTrial(currentTrialIdx);

        //if (experimentStartTime < 0f)
        //experimentStartTime = Time.time;

        responseManager.ResetResponse();

        stimulusManager.ShowStimulus(stimulus.Operation);
        float stimulusOnset = Time.time;

        yield return new WaitForSeconds(DUREE_STIMULUS);

        stimulusManager.HideStimulus();

        string response = responseManager.GetResponse();

        float rt = responseManager.HasResponded()
            ? responseManager.GetRT() - stimulusOnset
            : -1f;

        int correct = ScoreResponse(response, isTarget);
        float experimentTime = stimulusOnset - experimentStartTime;

        // Récupère (et consomme) le dernier son joué : chaque événement sonore n'est
        // ainsi reporté qu'une seule fois, sur le trial pendant lequel il a réellement été joué
        // (avant, un startle joué sur le trial 5 restait affiché sur tous les trials suivants).
        var soundInfo = soundManager.ConsumeLastSound();

        dataManager.SaveTrial(
            currentBlockIdx,
            currentN,
            currentTrialIdx,
            stimulus.Operation,
            isTarget,
            response,
            rt,
            correct,
            soundInfo.type,
            soundInfo.name,
            soundInfo.direction,
            soundInfo.time,
            experimentTime
        );
    }

    /// <summary>
    /// Détermine si l'essai courant est un véritable "match" n-back : le résultat de l'opération
    /// est identique à celui de l'opération n essais plus tôt DANS CE BLOC. On ne se fie plus au
    /// champ statique Stimulus.IsTarget (qui marque toute "valeur cible" comme target, y compris sa
    /// première apparition — alors que le participant n'a alors rien à quoi comparer). Avant la
    /// n-ième position du bloc, aucun match n'est possible.
    /// </summary>
    private bool IsRealNBackMatch(List<Stimulus> sequence, int trialIdx, int n)
    {
        if (trialIdx < n) return false;
        return sequence[trialIdx].Result == sequence[trialIdx - n].Result;
    }

    private int ScoreResponse(string response, bool isTarget)
    {
        if (response == "right" && isTarget) return 1;
        if (response == "left" && !isTarget) return 1;
        return 0;
    }

    private string GetInstruction(int blockIdx, int n)
    {
        if (blockIdx == 0)
        {
            return "Test 1/2 (1-back)\n\n" +
                   "Consigne : Appuyez le plus rapidement possible sur la gâchette droite si le résultat de l'opération est identique à celui de l'opération précédente.\n" +
                   "Dans le cas contraire, appuyez sur la gâchette gauche.\n\n" +
                   "Pour commencer, appuyez sur le bouton Y (bouton supérieur gauche).";
        }
        if (blockIdx == 1)
        {
            return "Changement de consigne\n\n" +
                   "Test 2/2 (2-back)\n\n" +
                   "Consigne : Appuyez le plus rapidement possible sur la gâchette droite si le résultat de l'opération est identique à celui de l'opération d'il y a 2 essais.\n" +
                   "Dans le cas contraire, appuyez sur la gâchette gauche.\n\n" +
                   "Pour commencer, appuyez sur le bouton Y (bouton supérieur gauche).";
        }
        if (blockIdx == 2)
        {
            return "Étape 1/2 (1-back)\n\n" +
                   "Consigne : Appuyez le plus rapidement possible sur la gâchette droite si le résultat de l'opération est identique à celui de l'opération précédente.\n" +
                   "Dans le cas contraire, appuyez sur la gâchette gauche.\n\n" +
                   "Pour commencer, appuyez sur le bouton Y (bouton supérieur gauche).";
        }
        // blockIdx == 10 (premier bloc 2-back principal)
        return "Changement de consigne\n\n" +
               "Étape 2/2 (2-back)\n\n" +
               "Consigne : Appuyez le plus rapidement possible sur la gâchette droite si le résultat de l'opération est identique à celui de l'opération d'il y a 2 essais.\n" +
               "Dans le cas contraire, appuyez sur la gâchette gauche.\n\n" +
               "Pour commencer, appuyez sur le bouton Y (bouton supérieur gauche).";
    }
}