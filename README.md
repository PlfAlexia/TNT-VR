# Éditeur Unity

Installer l'éditeur Unity Hub (https://unity.com/download) avec la version `600.3.17f1` et cocher **Android Build Support**, **Android SDK & NDK Tools** et **OpenJDK** pendant l'installation.

## Récupération du projet

Ouvrir un terminal à l'emplacement souhaité pour le projet et entrer :

```bash
git clone https://github.com/PlfAlexia/TNT-VR
```

## Ouverture du projet

1. Ouvrir Unity Hub
2. **Project > Add > Add project from disk** → sélectionner le dossier racine `TNT-VR`
3. Ouvrir la scène principale dans le dossier asset :
   `Assets > Scenes > SampleScene.unity`

## Configuration Android / Meta Quest

Dans Unity : **Edit > Project Settings**

**Player > Other Settings**
- Scripting Backend : `IL2CPP`
- Target Architectures : `ARM64` coché
- Package Name : vérifier qu'il correspond à celui enregistré sur le casque

**XR Plug-in Management**
- Onglet Android : cocher `Oculus` (ou `OpenXR` selon la config d'origine)

## Build and Run

1. **File > Build Settings**
2. Platform : `Android` (si ce n'est pas déjà sélectionné : *Switch Platform*, puis attendre le ré-import)
3. Vérifier que la scène principale est bien dans la liste **Scenes In Build**
4. Cliquer **Build And Run**
5. Choisir un dossier de sortie pour l'APK (ex : `Builds/`)

> L'application s'installera automatiquement sur le casque, penser à **Build And Run** à chaque modification.

## Récupération des données

Ouvrir un terminal :

```bash
adb pull /sdcard/Android/data/<package.name>/files/ ./donnees/
```

Remplacer `<package.name>` par le package défini dans Player Settings.

## À chaque changement, ne pas oublier

```bash
git add .
git commit -m "Description modification"
git push
```

---

# Documentation du code — Expérience VR N-back

Ce document décrit chaque script C# du projet, avec la même structure partout : **rôle général**, puis **champs** (les variables du script, avec leur type), puis **fonctions** (avec leur signature complète : paramètres typés et type de retour).

**Ordre du pipeline** : `ExperimentManager` est le chef d'orchestre. Il pilote `StimulusManager` (affichage), `ResponseManager` (lecture des réponses), `SoundManager` (audio), `DataManager` (CSV principal) et `HeadMotionTracker` (CSV de mouvement de tête), et pioche ses stimuli dans `StimuliData`.

Les 7 fichiers sont documentés ci-dessous dans cet ordre :

1. [StimuliData](#stimulidatacs)
2. [StimulusManager](#stimulusmanagercs)
3. [SoundManager](#soundmanagercs)
4. [DataManager](#datamanagercs)
5. [HeadMotionTracker](#headmotiontrackercs)
6. [ResponseManager](#responsemanagercs)
7. [ExperimentManager](#experimentmanagercs)

---

## `StimuliData.cs`

**Rôle général** : fichier de données pures, sans logique. C'est le stock des opérations arithmétiques (les stimuli) dans lequel `ExperimentManager` pioche pour construire les blocs de l'expérience.

**Classes définies ici** :
- `Stimulus` : représente un stimulus affiché pendant un essai (trial).
- `StimuliData` (classe statique) : contient toutes les listes de stimuli écrites à la main.

### Champs

**Classe `Stimulus`**

| Nom | Type | Description |
|---|---|---|
| `Operation` | `string` | Texte affiché à l'écran, ex. `"30 + 20"`. |
| `Result` | `int` | Résultat numérique de l'opération. |
| `IsTarget` | `bool` | Marque une "valeur cible" dans la liste écrite à la main (voir la nuance dans `ExperimentManager.IsRealNBackMatch`). |

**Classe statique `StimuliData`**

| Nom | Type | Description |
|---|---|---|
| `List0Back`, `List1BackA/B/C`, `List2BackA/B/C` | `List<Stimulus>` | 12 stimuli chacune, écrites à la main. Une valeur cible fixe par condition : `50` en 0-back, `35` en 1-back, `40` en 2-back. Plusieurs variantes (A/B/C) pour 1-back et 2-back, une seule pour 0-back — ça permet de varier l'ordre des essais quand une condition revient sur plusieurs blocs, sans changer la difficulté. |
| `Variants0Back`, `Variants1Back`, `Variants2Back` | `List<List<Stimulus>>` | Regroupent les variantes par condition. `ExperimentManager` pioche au hasard dedans pour choisir quelle variante utiliser sur un bloc donné. |

### Fonctions

- **`Stimulus(operation: string, result: int, isTarget: bool)`** : constructeur. Remplit les 3 champs, rien de plus.

---

## `StimulusManager.cs`

**Rôle général** : gère tout ce qui s'affiche à l'écran côté texte. Aucune logique d'expérience ici, uniquement des méthodes `Show`/`Hide` appelées par `ExperimentManager` selon l'état du trial en cours.

### Champs

| Nom | Type | Description |
|---|---|---|
| `textOperation` | `TextMeshProUGUI` (public) | Affiche l'opération arithmétique du stimulus (ou `"X"` pour la fixation, ou `"00+00"` pendant le repos). |
| `textInstruction` | `TextMeshProUGUI` (public) | Affiche les consignes et le message de fin. |
| `textTimer` | `TextMeshProUGUI` (public) | Affiche uniquement le décompte en secondes pendant le repos, séparé de `textOperation` pour pouvoir le centrer indépendamment à l'écran. |

### Fonctions

- **`Awake() : void`** — méthode Unity, appelée automatiquement à l'initialisation. Appelle `ClearAll()` pour être sûr que rien ne s'affiche avant que l'expérience commence.
- **`ShowStimulus(operation: string) : void`** — affiche l'opération arithmétique passée en paramètre. Coupe `textInstruction` au passage (au cas où il était encore actif).
- **`HideStimulus() : void`** — cache `textOperation`.
- **`ShowFixation() : void`** — affiche la croix de fixation `"X"` entre deux présentations de stimuli. Réutilise `textOperation` plutôt qu'un objet dédié, vu que c'est aussi une "opération" dans le principe (même position, même style).
- **`HideFixation() : void`** — cache `textOperation`.
- **`ShowRest(secondsRemaining: int) : void`** — pendant le repos entre les blocs : `"00+00"` reste affiché via `textOperation` (comme un stimulus neutre), et le décompte (`secondsRemaining`) s'affiche séparément via `textTimer`.
- **`HideRest() : void`** — coupe `textOperation` et `textTimer` d'un coup.
- **`ShowInstruction(message: string) : void`** — affiche le texte de consigne (utilisé en début de bloc pour expliquer la tâche). Coupe `textOperation` au passage.
- **`HideInstruction() : void`** — cache `textInstruction`.
- **`ShowEnd() : void`** — appelée à la toute fin de l'expérience. Repasse par `ClearAll()` pour tout nettoyer, puis affiche le message de remerciement dans `textInstruction`.
- **`ClearAll() : void`** *(privée)* — désactive les 3 objets texte (`textOperation`, `textInstruction`, `textTimer`). Vérifie que chaque référence n'est pas `null` avant de la toucher, pour éviter un crash si un champ a été oublié dans l'inspecteur Unity.

---

## `SoundManager.cs`

**Rôle général** : gère tout l'audio de l'expérience — musique de fond, gamme de Shepard en boucle, et sons startle/neutre spatialisés. Aucune logique de trial ici, uniquement des méthodes appelées par `ExperimentManager` selon l'état du bloc en cours.

**Classes définies ici** :
- `SoundManager` (avec l'énumération imbriquée `SoundDirection`)
- `TrialSound` : structure simple qui transporte les infos d'un son planifié entre `ExperimentManager` et `RequestStartle`.

### Champs

| Nom | Type | Description |
|---|---|---|
| `startleClips` | `AudioClip[]` (public) | Banque de sons startle, un clip est tiré au hasard à chaque déclenchement. |
| `neutralClip` | `AudioClip` (public) | Clip joué pour un son "neutre" (non-startle). |
| `backgroundMusic` | `AudioClip` (public) | Musique de fond, jouée en boucle dès `Awake()`. |
| `shepardClip` | `AudioClip` (public) | Un cycle complet de la gamme de Shepard (l'illusion sonore qui semble monter indéfiniment). |
| `soundDistance` | `float` (public) | Distance à laquelle les sons spatialisés sont positionnés autour de la caméra. |
| `musicVolume`, `sfxVolume`, `shepardVolume` | `float` (public, `[Range(0,1)]`) | Curseurs de volume réglables dans l'inspecteur, sans toucher au code. |
| `sfxSource` | `AudioSource` (privé) | Source audio 3D dédiée aux startles et sons neutres, repositionnée dynamiquement autour de la caméra selon la direction demandée. |
| `musicSource` | `AudioSource` (privé) | Source audio 2D en boucle pour la musique de fond. Lancée une seule fois dans `Awake()`, jamais retouchée ensuite. |
| `shepardSource` | `AudioSource` (privé) | Source audio 2D en boucle dédiée à la gamme de Shepard, pilotée manuellement (démarrée, mise en pause, reprise) par le reste du script. |
| `vrCamera` | `Camera` (privé) | Référence à `Camera.main`, récupérée une seule fois dans `Awake()` pour les calculs de position spatiale. |
| `SoundDirection` | `enum { Front, Back, Left, Right }` | Direction logique d'un son, convertie en vecteur 3D par `GetDirectionVector`. |
| `lastSoundType`, `lastSoundDirection`, `lastSoundName` | `string` (privés) | Infos du dernier son joué, repris par `ConsumeLastSound()`. |
| `lastSoundTime` | `float` (privé) | Temps auquel le dernier son a été joué (même référentiel que `experiment_time_ms`). |
| `shepardPlaying` | `bool` (privé) | Garde-fou indiquant si la gamme de Shepard est censée être active. Toutes les méthodes liées à Shepard la vérifient avant d'agir, pour éviter de manipuler un `AudioSource` qui ne joue pas. |
| `startTime` | `float` (privé) | t0 commun avec `ExperimentManager`/`HeadMotionTracker`, fixé une seule fois via `SetStartTime()`. |

### Fonctions

- **`Awake() : void`** — méthode Unity, appelée automatiquement à l'initialisation. Crée et configure les 3 `AudioSource` via `AddComponent` (plutôt qu'à la main dans l'éditeur, ce qui évite une configuration manuelle pour chaque scène) :
  - `spatialBlend` (`float`, 0 à 1) détermine si un son est 2D (`0f`, entendu pareil des deux oreilles) ou 3D/spatialisé (`1f`, semble venir d'une direction précise). `sfxSource` est en 3D pur, `musicSource` et `shepardSource` restent en 2D.
  - `rolloffMode` définit comment le volume d'un son 3D diminue avec la distance, `AudioRolloffMode.Linear` fait baisser le volume de façon régulière entre `minDistance` et `maxDistance`, plus prévisible que le mode par défaut d'Unity.
  - `loop` est coché pour `musicSource` et `shepardSource`. Une fois lancé, le clip redémarre automatiquement au lieu de s'arrêter.
  - `playOnAwake` est décoché pour ces deux mêmes sources, pour empêcher Unity de les lancer toutes seules dès que la scène démarre. C'est le script qui décide explicitement quand les lancer.

  Applique ensuite les volumes définis dans l'inspecteur, lance la musique de fond si un clip est assigné (`musicSource.Play()`), et récupère `Camera.main`.

- **`SetStartTime(t0: float) : void`** — appelée par `ExperimentManager` (même t0 que `HeadMotionTracker.SetStartTime`) pour que `sound_time_ms` soit exprimé dans le même référentiel que `experiment_time_ms`.
- **`StartShepardLoop() : void`** — démarre la gamme de Shepard, appelée une seule fois au premier bloc réel. Assigne le clip à `shepardSource`, appelle `shepardSource.Play()`, puis passe `shepardPlaying` à `true`.
- **`StopShepardLoop() : void`** — arrête définitivement la gamme via `shepardSource.Stop()`, appelée en fin d'expérience.
- **`PauseShepardLoop() : void`** / **`ResumeShepardLoop() : void`** — pause et reprise pendant les consignes et les repos entre blocs. S'appuient directement sur `AudioSource.Pause()` et `AudioSource.UnPause()` (méthodes natives d'Unity qui gèrent seules la position de lecture exacte du clip).
- **`GetTimeUntilNextCycleEnd() : float`** *(privée)* — calcule combien de secondes il reste avant la fin du cycle de Shepard en cours, à partir de `shepardSource.time` (position de lecture actuelle) et `shepardClip.length` (durée totale). Sert uniquement en interne à savoir quand jouer un startle. Retourne `0f` avec un avertissement si la gamme n'est pas active (le startle sera alors joué immédiatement).
- **`RequestStartle(sound: TrialSound) : void`** — point d'entrée appelé par `ExperimentManager` pour armer un startle sur un trial donné. Lance la coroutine `PlayStartleAtCycleEndRoutine`.
- **`PlayStartleAtCycleEndRoutine(sound: TrialSound) : IEnumerator`** *(privée, coroutine)* — une coroutine est une fonction Unity qui peut s'étaler sur plusieurs frames au lieu de s'exécuter d'un coup, utile ici pour attendre sans bloquer le reste du protocole. Attend la fin du cycle de Shepard en cours (durée calculée via `GetTimeUntilNextCycleEnd`) avant de déclencher le son. Les trials continuent de s'enchaîner normalement pendant l'attente, seul le son est différé.
- **`PlaySoundNow(sound: TrialSound) : void`** *(privée)* — joue le son (startle ou neutre) au moment voulu. Positionne `sfxSource` dans l'espace 3D autour de la caméra selon la direction du son (via `GetDirectionVector`). Tire un clip aléatoire parmi `startleClips` si c'est un startle, sinon utilise `neutralClip`. Enregistre le son joué (type, direction, heure, nom) dans les champs internes, pour que `ConsumeLastSound()` puisse ensuite les renvoyer. Met en pause la gamme de Shepard si c'était un startle, elle ne reprendra qu'au bloc suivant quand `ExperimentManager` appellera `ResumeShepardLoop()`.
- **`ConsumeLastSound() : (string type, string direction, float time, string name)`** — renvoie un tuple avec les infos du dernier son joué, puis les remet immédiatement à `"none"` / `-1f`. Garantit qu'un événement sonore n'apparaît qu'une seule fois dans le CSV de données, sur le trial où il a vraiment été joué.
- **`GetDirectionVector(dir: SoundDirection) : Vector3`** *(privée)* — convertit une direction logique (Front/Back/Left/Right) en un vecteur 3D relatif à l'orientation actuelle de la caméra. Utilisé juste avant de jouer un son pour savoir où positionner `sfxSource` par rapport au participant.

**Classe `TrialSound`**

| Nom | Type | Description |
|---|---|---|
| `TrialIndex` | `int` | Index du trial concerné. |
| `Type` | `string` | `"startle"` ou autre type de son. |
| `Direction` | `SoundManager.SoundDirection` | Direction du son. |

- **`TrialSound(idx: int, type: string, dir: SoundManager.SoundDirection)`** — constructeur, remplit les 3 champs.

---

## `DataManager.cs`

**Rôle général** : gère la sortie CSV des données récoltées pendant l'expérience (le fichier principal `nback_*.csv`), une ligne par trial.

### Champs

| Nom | Type | Description |
|---|---|---|
| `writer` | `StreamWriter` (privé) | Objet .NET qui écrit le texte dans le fichier CSV, ligne par ligne. |
| `filePath` | `string` (privé) | Chemin complet du fichier CSV. |

### Fonctions

- **`Awake() : void`** — méthode Unity, appelée automatiquement à l'initialisation. Construit le nom du fichier `nback_{timestamp}.csv` (timestamp au format `yyyy-MM-dd_HH-mm-ss`), le place dans `Application.persistentDataPath` (dossier de sauvegarde géré par Unity, propre à chaque plateforme), crée le `StreamWriter`, puis écrit la ligne d'en-tête qui liste les colonnes du CSV :

  | Colonne | Description |
  |---|---|
  | `experiment_time_ms` | Durée écoulée depuis le début de l'expérience, en ms. |
  | `block_index` | Bloc en cours (démarre à 0). |
  | `trial_index` | Trial en cours (démarre à 0). |
  | `n_back` | Condition n-back en cours. |
  | `operation` | Opération affichée. |
  | `is_target` | 1 si l'opération est une cible, 0 sinon. |
  | `response` | Gâchette pressée par le participant. |
  | `rt_ms` | Temps de réponse du participant, si réponse il y a eu. |
  | `correct` | 1 si la réponse est correcte, 0 sinon. |
  | `sound_type` | Type de son joué. |
  | `sound_name` | Nom du son joué. |
  | `sound_direction` | Direction d'où vient le son. |
  | `sound_time_ms` | Instant (en ms) où le son a été joué. |

- **`SaveTrial(blockIndex: int, nBack: int, trialIndex: int, operation: string, isTarget: bool, response: string, rt: float, correct: int, soundType: string, soundName: string, soundDirection: string, soundTime: float, experimentTime: float) : void`** — prend en paramètres toutes les données nécessaires pour compléter une ligne du CSV.
  - `rtStr` : temps de réponse converti en millisecondes s'il y a eu une réponse, `"NaN"` sinon (pas de réponse ou temps invalide).
  - `soundTimeStr` : instant du son (en ms) s'il y en a eu un, `"NaN"` sinon.
  - `experimentTimeStr` : temps d'expérience (en secondes) converti en millisecondes arrondies.
  - `line` : assemble toutes les valeurs séparées par des virgules et l'écrit dans le fichier, puis force immédiatement l'écriture sur le disque via `writer.Flush()` (pour ne rien perdre en cas de plantage juste après).

- **`CloseFile() : void`** — force l'écriture de tout ce qui reste en mémoire (`Flush()`) puis ferme proprement le fichier (`Close()`), et remet `writer` à `null` pour éviter une fermeture en double.
- **`OnDestroy() : void`** — méthode Unity, appelée automatiquement quand l'objet est détruit (fin de scène, arrêt de l'application). Appelle `CloseFile()`, ce qui permet de conserver un CSV exploitable même si l'expérience est interrompue avant la fin.

---

## `HeadMotionTracker.cs`

**Rôle général** : enregistre en continu la position et l'accélération de la tête du participant (via la caméra VR) dans un fichier CSV séparé (`headmotion_*.csv`). Échantillonne à fréquence fixe, indépendamment du CSV principal de `DataManager`.

### Champs

| Nom | Type | Description |
|---|---|---|
| `vrCamera` | `Camera` (public) | Référence caméra (tête du participant). |
| `SAMPLE_RATE` | `const float = 60f` | Fréquence cible d'échantillonnage, en Hz. |
| `SMOOTHING_WINDOW` | `const int = 5` | Taille de la fenêtre de moyenne glissante appliquée à l'accélération. |
| `sampleInterval` | `float` (privé) | Temps minimal entre deux échantillons écrits, calculé comme `1 / SAMPLE_RATE`. |
| `timeSinceLastSample` | `float` (privé) | Accumule le temps écoulé depuis le dernier échantillon écrit. |
| `writer` | `StreamWriter` (privé) | Écrit le CSV, même logique que `DataManager`. |
| `filePath` | `string` (privé) | Chemin complet du fichier CSV. |
| `startTime` | `float` (privé) | t0 commun avec `ExperimentManager`, initialisé à `-1f` (valeur sentinelle "pas encore reçu"), fixé une seule fois via `SetStartTime()`. |
| `lastPosition` | `Vector3` (privé) | Position de la caméra à l'échantillon précédent. |
| `lastVelocity` | `Vector3` (privé) | Vitesse calculée à l'échantillon précédent. |
| `hasLastPosition`, `hasLastVelocity` | `bool` (privés) | Garde-fous évitant des calculs faux sur les tout premiers frames, où il n'existe pas encore de position/vitesse précédente. |
| `currentBlockIndex`, `currentTrialIndex` | `int` (privés) | Bloc et trial courants, fixés par `ExperimentManager` via `SetCurrentBlock`/`SetCurrentTrial`. |
| `accelerationWindow` | `Queue<float>` (privé) | Fenêtre glissante des derniers échantillons d'accélération brute, utilisée pour le lissage. |

### Fonctions

- **`Awake() : void`** — méthode Unity, appelée automatiquement à l'initialisation. Calcule `sampleInterval`, crée le fichier CSV (même logique que `DataManager` : nom horodaté, stocké dans `Application.persistentDataPath`, écrase tout fichier existant du même nom) et écrit la ligne d'en-tête : `time_ms, block_index, trial_index, pos_x, pos_y, pos_z, acceleration_raw, acceleration_smoothed`.

- **`Update() : void`** — méthode Unity, appelée à chaque frame.
  - `timeSinceLastSample` accumule `Time.deltaTime` (temps écoulé depuis la frame précédente). Si le cumul est inférieur à `sampleInterval`, la fonction s'arrête immédiatement sans rien écrire — c'est ce qui permet de garder un échantillonnage régulier à 60 Hz plutôt que d'écrire à chaque frame (qui tourne à une fréquence variable).
  - Vitesse : `(position actuelle − position précédente) / dt`, calculée seulement à partir du 2ᵉ échantillon (`hasLastPosition`).
  - Accélération : `(vitesse actuelle − vitesse précédente) / dt` ; seule la norme du vecteur (`.magnitude`) est gardée, pour obtenir une intensité plutôt qu'une direction.
  - Horodatage (point clé de la synchronisation avec `DataManager`) : si `SetStartTime()` a bien été appelée, `t0 = startTime` ; sinon (repli de sécurité), `t0 = 0f`, ce qui revient à logger le `Time.time` brut d'Unity. `timeMs = (Time.time − t0) * 1000f` — exactement la même logique de conversion que `experiment_time_ms` dans `DataManager`, ce qui rend les deux fichiers directement comparables/joignables sur l'axe temporel.
  - Écrit la ligne dans le CSV avec `CultureInfo.InvariantCulture`, pour garantir un point décimal (`.`) et non une virgule, quels que soient les paramètres régionaux de la machine.

- **`ComputeSmoothedAcceleration(newSample: float) : float`** *(privée)* — maintient une `Queue<float>` des `SMOOTHING_WINDOW` (5) derniers échantillons d'accélération brute : ajoute le nouvel échantillon (`Enqueue`) et retire le plus ancien si la file dépasse 5 éléments (`Dequeue`), puis retourne la moyenne. La dérivée seconde brute (position → vitesse → accélération) amplifie fortement le bruit de tracking ; ce lissage donne un signal plus exploitable pour détecter un sursaut, tout en conservant la valeur brute dans le CSV pour ne rien perdre.
- **`SetStartTime(t0: float) : void`** — appelée par `ExperimentManager` (une seule fois, dans `Start()`, juste après avoir fixé `experimentStartTime`) pour synchroniser le t0 entre `headmotion.csv` et `nback.csv`.
- **`SetCurrentBlock(blockIndex: int) : void`** / **`SetCurrentTrial(trialIndex: int) : void`** — appelées par `ExperimentManager` pour indiquer respectivement le bloc et le trial en cours. Permettent de fusionner `headmotion.csv` et `nback.csv` en post-traitement sur `(block_index, trial_index)`, et d'aligner précisément les fenêtres temporelles autour de chaque stimulus/son.
- **`CloseFile() : void`** / **`OnDestroy() : void`** — même logique que `DataManager` : `Flush()` puis `Close()` du fichier, appelé automatiquement via `OnDestroy()` pour garantir un CSV exploitable même si l'expérience est interrompue avant la fin.

---

## `ResponseManager.cs`

**Rôle général** : capture les réponses du participant pendant chaque trial (via les gâchettes des manettes VR ou, en secours, le clavier), ainsi que l'appui de confirmation utilisé pour passer les écrans de consigne. Ne connaît rien du déroulement de l'expérience, `ExperimentManager` vient lire son état à chaque trial.

### Champs

| Nom | Type | Description |
|---|---|---|
| `currentResponse` | `string` (privé) | Réponse actuelle : `"none"`, `"right"` ou `"left"`. |
| `responseTime` | `float` (privé) | `Time.time` (horloge Unity brute) au moment où la réponse a été enregistrée. Pas encore un vrai temps de réaction, c'est `ExperimentManager` qui soustrait l'instant d'apparition du stimulus pour l'obtenir. |
| `hasResponded` | `bool` (privé) | Empêche d'enregistrer plus d'une réponse par trial. |
| `rightController`, `leftController` | `XRInputDevice` (privés, alias de `UnityEngine.XR.InputDevice`) | Références aux manettes VR droite/gauche. |
| `TRIGGER_THRESHOLD` | `const float = 0.5f` | Seuil à partir duquel une pression sur la gâchette est considérée comme un appui. |
| `prevRightTrigger`, `prevLeftTrigger` | `float` (privés) | Valeur de la gâchette à la frame précédente, utilisée pour détecter le passage sous le seuil vers au-dessus (front montant), afin de ne compter chaque appui qu'une seule fois. |
| `prevConfirmButton` | `bool` (privé) | État du bouton de confirmation à la frame précédente, même logique de front montant que ci-dessus. |
| `confirmPressed` | `bool` (privé) | Indique qu'un appui sur le bouton de confirmation est en attente d'être consommé par `SpacePressed()`. |

### Fonctions

- **`Start() : void`** — méthode Unity, appelée automatiquement au premier frame. Appelle `TryGetControllers()`.
- **`Update() : void`** — méthode Unity, appelée à chaque frame. Si l'une des deux manettes n'est pas valide, retente `TryGetControllers()`. Lit ensuite le bouton de confirmation (`ReadConfirmButton()`), puis lit les inputs de réponse (`ReadInputs()`) uniquement si le participant n'a pas encore répondu sur ce trial.
- **`TryGetControllers() : void`** *(privée)* — récupère les manettes droite/gauche via `InputDevices.GetDevicesWithCharacteristics`, en filtrant par caractéristiques Right/Left + Controller.
- **`ReadInputs() : void`** *(privée)* — lit la valeur de la gâchette droite et de la gâchette gauche (`TryGetFeatureValue(XRCommonUsages.trigger, ...)`), détecte un front montant par rapport à `TRIGGER_THRESHOLD` (valeur actuelle au-dessus du seuil, valeur précédente en dessous) et appelle `RegisterResponse("right")` ou `RegisterResponse("left")` en conséquence. Lit aussi les flèches gauche/droite du clavier comme solution de secours (`Keyboard.current`).
- **`ReadConfirmButton() : void`** *(privée)* — lit le bouton secondaire des deux manettes (`XRCommonUsages.secondaryButton`, boutons B/Y) ainsi que la touche Espace du clavier. Détecte un front montant sur la combinaison des trois sources et passe `confirmPressed` à `true` dans ce cas.
- **`RegisterResponse(side: string) : void`** *(privée)* — enregistre la réponse (`currentResponse = side`, `responseTime = Time.time`, `hasResponded = true`) si aucune réponse n'a encore été donnée sur ce trial ; ne fait rien sinon.
- **`ResetResponse() : void`** — remet `currentResponse`, `responseTime` et `hasResponded` à leur état initial. Appelée par `ExperimentManager` au début de chaque nouveau trial.
- **`GetResponse() : string`** — retourne `currentResponse`.
- **`GetRT() : float`** — retourne `responseTime` (temps Unity absolu, pas encore un temps de réaction relatif au stimulus).
- **`HasResponded() : bool`** — retourne `hasResponded`.
- **`SpacePressed() : bool`** — consomme `confirmPressed` : si un appui est en attente, le remet à `false` et retourne `true` (une seule fois par appui) ; sinon retourne `false`. Utilisée dans une boucle d'attente (`WaitUntil`) pour savoir quand passer à l'écran suivant.
- **`ClearConfirm() : void`** — force `confirmPressed` à `false` sans le "consommer" au sens d'un vrai appui traité. Utilisée pour éviter qu'un appui donné pendant l'écran précédent ne "fuite" vers l'écran de consigne suivant.

---

## `ExperimentManager.cs`

**Rôle général** : le chef d'orchestre de toute l'expérience. C'est le seul script qui connaît le déroulement complet (ordre des blocs, timing, quand jouer un son, quand écrire une ligne de données) et qui pilote tous les autres scripts en leur disant quoi faire et quand.

**Classes définies ici** :
- `Block` : regroupe une condition n-back et la séquence de stimuli associée pour un bloc.

### Champs

**Constantes de timing et de structure**

| Nom | Type | Description |
|---|---|---|
| `TRIALS_PAR_BLOC` | `const int = 12` | Nombre d'essais par bloc. |
| `DUREE_STIMULUS` | `const float = 2f` | Durée d'affichage d'un stimulus, en secondes. |
| `DUREE_FIXATION` | `const float = 1f` | Durée d'affichage de la croix de fixation entre deux stimuli, en secondes. |
| `DUREE_REPOS` | `const float = 18f` | Durée du repos entre deux blocs principaux, en secondes. |
| `FIRST_REAL_BLOCK_INDEX` | `const int = 2` | Index du premier bloc réel (les blocs 0 et 1 sont des blocs de test) ; c'est à ce bloc que la gamme de Shepard démarre, et elle tourne en continu jusqu'à la toute fin. |
| `STARTLE_SCHEDULE` | `static readonly (int blockIdx, int trialIdx, string direction)[]` | Planning des 4 startles, modifiable directement dans le code. Chaque entrée est un tuple `(bloc, trial, direction)` et la direction vide `""` signifie qu'elle sera tirée aléatoirement (avant/arrière) par `GenerateSoundSchedule`. |
| `instructionBlocks` | `HashSet<int> = {0, 1, 2, 10}` | Index des blocs précédés d'un écran de consigne : les 2 blocs de test, le premier bloc 1-back principal, le premier bloc 2-back principal. |

**Références vers les autres scripts** (assignées dans l'inspecteur Unity)

| Nom | Type |
|---|---|
| `stimulusManager` | `StimulusManager` (public) |
| `responseManager` | `ResponseManager` (public) |
| `dataManager` | `DataManager` (public) |
| `soundManager` | `SoundManager` (public) |
| `headMotionTracker` | `HeadMotionTracker` (public) |

**État de l'expérience en cours**

| Nom | Type | Description |
|---|---|---|
| `allBlocks` | `List<Block>` (privé) | Les 18 blocs de l'expérience (2 blocs de test + 8 blocs 1-back + 8 blocs 2-back), générés une fois au lancement. |
| `currentBlockIdx` | `int` (privé) | Index du bloc en cours. |
| `currentTrialIdx` | `int` (privé) | Index du trial en cours dans le bloc. |
| `currentN` | `int` (privé) | Valeur de n (1 ou 2) pour le bloc en cours. |
| `experimentStartTime` | `float` (privé) | t0 de l'expérience (`Time.time` au lancement), partagé avec `HeadMotionTracker` et `SoundManager` via `SetStartTime()`. |
| `soundSchedule` | `Dictionary<(int, int), TrialSound>` (privé) | Planning des sons, indexé par `(blockIdx, trialIdx)`, généré une fois au lancement. |

**Classe `Block`**

| Nom | Type | Description |
|---|---|---|
| `N` | `int` | Condition n-back du bloc (1 ou 2). |
| `Sequence` | `List<Stimulus>` | Séquence de stimuli du bloc, déjà randomisée. |

### Fonctions

- **`Start() : void`** — méthode Unity, appelée automatiquement au premier frame. Fixe `experimentStartTime = Time.time` (le t0 de toute l'expérience), le propage à `headMotionTracker` et `soundManager` via `SetStartTime()` (pour que les 3 fichiers de sortie partagent la même origine temporelle), génère les blocs (`GenerateAllBlocks()`) et le planning des sons (`GenerateSoundSchedule()`), puis lance la coroutine principale `RunExperiment()`.

- **`GenerateAllBlocks() : List<Block>`** *(privée)* — construit la liste des 18 blocs dans l'ordre : 1 bloc test 1-back, 1 bloc test 2-back, 8 blocs principaux 1-back, 8 blocs principaux 2-back. Pour chaque bloc, tire une variante au hasard dans `StimuliData.Variants1Back`/`Variants2Back` et la fait passer par `RandomizeNBack` pour randomiser l'ordre des stimuli.

- **`GenerateSoundSchedule() : Dictionary<(int, int), TrialSound>`** *(privée)* — construit le planning des 4 startles à partir de `STARTLE_SCHEDULE`. Tire aléatoirement l'ordre avant/arrière pour la paire de startles en blocs 1-back, puis séparément pour la paire en blocs 2-back (via `ShuffleDirections`), en respectant la contrainte : 2 startles sur des blocs 1-back (un avant, un arrière) et 2 startles sur des blocs 2-back (un avant, un arrière).

- **`ShuffleDirections(list: List<SoundManager.SoundDirection>) : void`** *(privée)* — mélange une liste de directions en place (algorithme de Fisher-Yates).

- **`RandomizeNBack(stimuli: List<Stimulus>, n: int) : List<Stimulus>`** *(privée)* — randomise l'ordre d'une liste de stimuli tout en respectant le motif cible/non-cible d'origine (le pattern `IsTarget` de chaque position est conservé) et en évitant qu'un distracteur ne crée accidentellement un vrai match n-back (même résultat que le stimulus n positions plus tôt). Sépare les stimuli en targets et distractors, puis essaie jusqu'à 1000 fois de construire une séquence valide en mélangeant chaque groupe (`Shuffle`) et en vérifiant la contrainte à chaque insertion. Si aucune séquence valide n'est trouvée après 1000 tentatives, logue une erreur et retourne la liste d'origine, non randomisée.

- **`Shuffle(list: List<Stimulus>) : void`** *(privée)* — mélange une liste de stimuli en place, utilisée par `RandomizeNBack`.

- **`RunExperiment() : IEnumerator`** *(privée, coroutine)* — la boucle principale de l'expérience, qui s'étale sur toute sa durée. Pour chaque bloc (`currentBlockIdx` de `0` à `allBlocks.Count - 1`) :
  - Indique le bloc courant à `headMotionTracker` (`SetCurrentBlock`).
  - Démarre la gamme de Shepard au premier bloc réel (`FIRST_REAL_BLOCK_INDEX`), et la reprend (`ResumeShepardLoop`) si elle était en pause depuis un startle du bloc précédent.
  - Récupère les sons planifiés pour ce bloc dans `soundSchedule`.
  - Si le bloc fait partie de `instructionBlocks` : force `trial_index = -1` dans `headMotionTracker` (pour ne pas laisser traîner l'index du dernier trial du bloc précédent), met la gamme de Shepard en pause, affiche la consigne (`ShowInstruction`) et attend un appui de confirmation (`WaitUntil` sur `responseManager.SpacePressed()`), puis affiche une croix de fixation avant de reprendre la gamme de Shepard.
  - Pour chaque trial du bloc, arme un startle si un son est planifié sur ce trial (`soundManager.RequestStartle`, non bloquant, le son attend la fin du cycle de Shepard en cours), détermine si c'est un vrai match n-back (`IsRealNBackMatch`), puis lance la coroutine `RunTrial`. Affiche une croix de fixation entre deux trials (sauf après le dernier du bloc).
  - Après un bloc principal (pas après les 2 blocs de test), met la gamme de Shepard en pause, lance le décompte de repos (`RunRestCountdown`), la reprend, puis affiche une croix de fixation.

  À la toute fin, arrête définitivement la gamme de Shepard, affiche l'écran de fin, et ferme les deux fichiers CSV (`headMotionTracker.CloseFile()`, `dataManager.CloseFile()`).

- **`RunRestCountdown() : IEnumerator`** *(privée, coroutine)* — affiche le décompte de repos seconde par seconde (`stimulusManager.ShowRest`) pendant `DUREE_REPOS` (18) secondes, puis le cache (`HideRest`).

- **`RunTrial(stimulus: Stimulus, isTarget: bool) : IEnumerator`** *(privée, coroutine)* — exécute un seul essai :
  - Synchronise `headMotionTracker` sur le trial courant (`SetCurrentTrial`).
  - Réinitialise `responseManager` (`ResetResponse`), affiche le stimulus et note l'instant d'apparition (`stimulusOnset = Time.time`).
  - Attend `DUREE_STIMULUS` secondes, puis cache le stimulus.
  - Récupère la réponse donnée (`response`). Calcule le temps de réaction `rt = responseManager.GetRT() - stimulusOnset` s'il y a eu une réponse, sinon `-1f`.
  - Calcule `correct` via `ScoreResponse`, et `experimentTime = stimulusOnset - experimentStartTime` (temps relatif au début de l'expérience).
  - Récupère et consomme le dernier son joué (`soundManager.ConsumeLastSound()`), ce qui garantit qu'un son n'est reporté que sur le trial pendant lequel il a réellement été joué.
  - Enregistre toutes ces informations dans le CSV via `dataManager.SaveTrial(...)`.

- **`IsRealNBackMatch(sequence: List<Stimulus>, trialIdx: int, n: int) : bool`** *(privée)* — détermine si le trial courant est un véritable "match" n-back : le résultat de l'opération est identique à celui de l'opération n essais plus tôt dans ce bloc. Ne se fie pas au champ statique `Stimulus.IsTarget` (qui marque toute "valeur cible" comme cible, y compris sa première apparition — alors que le participant n'a alors rien à quoi comparer). Avant la n-ième position du bloc, aucun match n'est possible (`trialIdx < n` → `false`).

- **`ScoreResponse(response: string, isTarget: bool) : int`** *(privée)* — retourne `1` si la réponse est correcte (`"right"` + cible, ou `"left"` + non-cible), `0` sinon.

- **`GetInstruction(blockIdx: int, n: int) : string`** *(privée)* — retourne le texte de consigne à afficher, différent selon qu'il s'agit du bloc test 1-back (0), du bloc test 2-back (1), du premier bloc 1-back principal (2), ou du premier bloc 2-back principal (10, changement de consigne).
