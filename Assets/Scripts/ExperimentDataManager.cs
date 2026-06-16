using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentManager : MonoBehaviour
{
    private const int BLOCS_PAR_CONDITION = 10;
    private const int TRIALS_PAR_BLOC = 12;
    private const float DUREE_STIMULUS = 2f;   // secondes
    private const float DUREE_FIXATION = 1f;   // secondes (inter-stimulus)
    private const float DUREE_REPOS = 18f;  // secondes (entre les blocs)

    public StimulusManager stimulusManager;
    public ResponseManager responseManager;
    public DataManager dataManager;
    public SoundManager soundManager;

    private List<Block> allBlocks;
    private int currentBlockIdx = 0;
    private int currentTrialIdx = 0;
    private int currentN = 0;

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
        allBlocks = GenerateAllBlocks();
        StartCoroutine(RunExperiment());
    }

    private List<Block> GenerateAllBlocks()
    {
        List<Block> blocks = new List<Block>();

        for (int i = 0; i < BLOCS_PAR_CONDITION; i++)
        {
            List<Stimulus> variant = StimuliData.Variants0Back[Random.Range(0, StimuliData.Variants0Back.Count)];
            blocks.Add(new Block(0, Randomize0Back(variant)));
        }

        for (int i = 0; i < BLOCS_PAR_CONDITION; i++)
        {
            List<Stimulus> variant = StimuliData.Variants1Back[Random.Range(0, StimuliData.Variants1Back.Count)];
            blocks.Add(new Block(1, RandomizeNBack(variant, 1)));
        }

        for (int i = 0; i < BLOCS_PAR_CONDITION; i++)
        {
            List<Stimulus> variant = StimuliData.Variants2Back[Random.Range(0, StimuliData.Variants2Back.Count)];
            blocks.Add(new Block(2, RandomizeNBack(variant, 2)));
        }

        return blocks;
    }

    // Randomisation 0-back
    private List<Stimulus> Randomize0Back(List<Stimulus> stimuli)
    {
        List<Stimulus> shuffled = new List<Stimulus>(stimuli);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Stimulus temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }
        return shuffled;
    }

    // Randomisation n-back 
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

    // Shuffle générique 
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

            // Générer le plan de sons pour ce bloc
            List<bool> isTargetSeq = new List<bool>();
            foreach (Stimulus s in block.Sequence) isTargetSeq.Add(s.IsTarget);
            List<TrialSound> soundPlan = soundManager.GenerateBlockSoundPlan(isTargetSeq);

            // Instructions au début de chaque condition (blocs 0, 10, 20)
            if (currentBlockIdx == 0 || currentBlockIdx == 10 || currentBlockIdx == 20)
            {
                responseManager.ClearConfirm();
                stimulusManager.ShowInstruction(GetInstruction(currentN));
                yield return new WaitUntil(() => responseManager.SpacePressed());
                stimulusManager.HideInstruction();

                stimulusManager.ShowFixation();
                yield return new WaitForSeconds(DUREE_FIXATION);
                stimulusManager.HideFixation();
            }

            // Lancer les 12 trials
            for (currentTrialIdx = 0; currentTrialIdx < TRIALS_PAR_BLOC; currentTrialIdx++)
            {
                Stimulus stimulus = block.Sequence[currentTrialIdx];
                yield return StartCoroutine(RunTrial(stimulus, currentTrialIdx, soundPlan));

                if (currentTrialIdx < TRIALS_PAR_BLOC - 1)
                {
                    stimulusManager.ShowFixation();
                    yield return new WaitForSeconds(DUREE_FIXATION);
                    stimulusManager.HideFixation();
                }
            }

            // Écran de repos entre les blocs
            stimulusManager.ShowRest();
            yield return new WaitForSeconds(DUREE_REPOS);
            stimulusManager.HideRest();

            stimulusManager.ShowFixation();
            yield return new WaitForSeconds(DUREE_FIXATION);
            stimulusManager.HideFixation();
        }

        stimulusManager.ShowEnd();
        dataManager.CloseFile();
    }

    // Déroulement d'un trial 
    private IEnumerator RunTrial(Stimulus stimulus, int trialIndex, List<TrialSound> soundPlan)
    {
        responseManager.ResetResponse();

        stimulusManager.ShowStimulus(stimulus.Operation);
        float stimulusOnset = Time.time;

        // Son joué à l'onset du stimulus
        soundManager.PlayIfScheduled(trialIndex, soundPlan);

        yield return new WaitForSeconds(DUREE_STIMULUS);

        stimulusManager.HideStimulus();

        string response = responseManager.GetResponse();

        // RT en secondes relatif à l'apparition du stimulus
        float rt = responseManager.HasResponded()
            ? responseManager.GetRT() - stimulusOnset
            : -1f;

        int correct = ScoreResponse(response, stimulus.IsTarget);

        dataManager.SaveTrial(
            currentBlockIdx,
            currentN,
            currentTrialIdx,
            stimulus.Operation,
            stimulus.IsTarget,
            response,
            rt,
            correct,
            soundManager.GetLastSoundType(),
            soundManager.GetLastSoundDirection(),
            soundManager.GetLastSoundTime()
        );
    }

    // Scoring 
    private int ScoreResponse(string response, bool isTarget)
    {
        if (response == "right" && isTarget) return 1;
        if (response == "left" && !isTarget) return 1;
        return 0;
    }

    private string GetInstruction(int n)
    {
        switch (n)
        {
            case 0: return "Appuyez sur la gâchette droite si le résultat est 50,\nsinon sur la gâchette gauche.";
            case 1: return "Appuyez sur la gâchette droite si le résultat est identique au calcul précédent,\nsinon sur la gâchette gauche.";
            case 2: return "Appuyez sur la gâchette droite si le résultat est identique au calcul d'il y a 2 essais,\nsinon sur la gâchette gauche.";
            default: return "";
        }
    }
}