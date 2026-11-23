# Ninou's Farm — port Godot 4.7 (C#)

Port du projet Unity du même dépôt vers **Godot 4.7 .NET**. Le projet Unity reste intact à la racine ;
le port vit uniquement dans ce dossier `Godot/`.

## Ouvrir le projet

1. Lancer `Godot_v4.7.2-stable_mono_win64.exe` → *Importer* → choisir `Godot/project.godot`.
2. Compiler une fois (bouton *Build* en haut à droite, ou `Alt+B`).
3. *F5* lance le jeu : `world.tscn`, le monde procédural (200x200, ~2 s de génération ; Espace pour la finir sans
   afficher la progression). Pour la scène de test des outils, ouvrir `scenes/test_tools.tscn` puis *F6*.

Contrôles : ZQSD/WASD (touches physiques) ou flèches. Clic gauche (ou Entrée) = utiliser l'objet en main,
molette, touches 1 à 9 ou clic sur une case = choisir le slot de la toolbar, I/Tab = afficher l'inventaire,
E = interagir (ouvrir / fermer le coffre ; il se referme aussi quand on s'éloigne).
Glisser-déposer une case vers une autre (inventaire, toolbar ou coffre) : déplace, complète une pile ou échange.

Scène de test : hache, houe, arrosoir, canne à pêche, 3 graines de tomate et 3 de salade au départ, un coffre de 20 cases
et trois arbres (3 coups de hache, ils lâchent du bois que l'aimant du joueur attire).
Pêche : canne en main, clic face à l'eau (bord de l'étang ou de l'île). Un clic pendant l'attente remonte la ligne ;
quand le poisson mord, une jauge apparaît à droite : cliquer quand le curseur est dans la zone verte. Une culture pousse d'un stade
par heure de jeu **arrosée** (1 heure = 30 s réelles) ; la houe la récolte (ou rend la graine si elle n'est pas mûre).

## Arborescence

| Dossier | Contenu |
|---|---|
| `scripts/` | Code C#, même découpage que `Assets/_Project/Assets/Scripts` |
| `scenes/` | `world.tscn` (scène principale, équivalent de `Test_1.unity`), `test_tools.tscn` (équivalent de `Test_Tools.unity`), `player.tscn`, `crop_instance.tscn`, `item_pickable.tscn` (les prefabs) |
| `scenes/ui/` | `hud.tscn` (toolbar, inventaire, coffre, heure), `ui_slot.tscn` (prefab UISlot) |
| `scenes/objects/` | `chest.tscn`, `tree.tscn`, `axe_hitbox.tscn` |
| `assets/` | Textures copiées depuis le pack Sprout Lands (**générées**, ne pas éditer) |
| `resources/tiles/` | `world_tileset.tres`, `rule_tiles.json` (**générés**) |
| `resources/characters/` | `player_frames.tres` : animations du joueur (**générées**) |
| `resources/items/` | Outils, récoltes, graines, cultures, poissons et table de pêche en `.tres` (**générés** depuis les ScriptableObjects) |
| `resources/ui/` | Sprites d'UI (`slot`, `panel`, coins du sélecteur, **générés**) et `ui_theme.tres` (police pixel) |
| `resources/world/` | `world_gen_config.tres` (SO_WorldGenConfig) et `world_items.json` (catalogue du décor) (**générés**) |
| `resources/objects/` | `chest_frames.tres`, `tree_frames.tres` : animations du coffre et de l'arbre (**générées**) |
| `tools/convert_unity_assets.py` | Convertisseur Unity → Godot (RuleTiles, collisions, tuiles labourées, clips `.anim`, objets) |

Pour régénérer les ressources après une modif côté Unity : `python Godot/tools/convert_unity_assets.py`
(depuis la racine du dépôt), puis relancer Godot.

## Correspondances Unity → Godot utilisées

| Unity | Godot |
|---|---|
| `MonoBehaviour` + `Awake` / `Start` / `Update` / `FixedUpdate` | `Node` + `_EnterTree` / `_Ready` / `_Process` / `_PhysicsProcess` |
| `[SerializeField] private` | `[Export] private` (la classe doit être `partial`) |
| `ScriptableObject` (`[CreateAssetMenu]`) | `Resource` + `[GlobalClass]`, assets en `.tres` |
| `Rigidbody2D.MovePosition` | `CharacterBody2D.MoveAndSlide` |
| `Tilemap` + `RuleTile` | `TileMapLayer` + `RuleTile.cs` (règles Unity converties en JSON) |
| `TilemapCollider2D` + Physics Shape des sprites | Polygones de collision du `TileSet` (repris des `.meta`) |
| `Animator` (blend tree, triggers) | `AnimatedSprite2D` piloté en code (`PlayerMovements`) |
| `StateMachineBehaviour` (`LockMovementBehaviour`) | `PlayAction()` + signal `AnimationFinished` |
| Prefab + `Instantiate` / `Resources.Load` | Scène `.tscn` + `PackedScene.Instantiate()` + `AddChild` / `GD.Load` |
| `OnTriggerEnter2D` | Signal `AreaEntered` d'une `Area2D` |
| `OnDestroy` | `_ExitTree` (désabonner les événements statiques ici) |
| Coroutine (`yield return new WaitForSeconds`) | Méthode `async` + `await ToSignal(GetTree().CreateTimer(t), "timeout")` |
| DOTween | `CreateTween()` (lié au nœud, s'arrête avec lui) |
| `UnityEvent` | `[Signal]` |
| `EventSystem` / `IsPointerOverUI` | `_UnhandledInput` : l'UI consomme ses clics avant le jeu |
| Trigger d'Animator (`HOE`, `WATER`) | Préfixe d'animation (`hoe`, `watering`) + direction (`_down`, `_left`...) |
| Canvas + `Image` + « Pixels Per Unit Multiplier » | `CanvasLayer` + `PixelUIRoot` (UI en pixels du sprite, agrandie ×2) |
| `Image` en mode *Sliced* (9-slice) | `NinePatchRect` (marges reprises des bordures du sprite) |
| `GridLayoutGroup` / `HorizontalLayoutGroup` | `GridContainer` / `HBoxContainer` |
| `Button.onClick` | Signal `Pressed` d'un `Button` (plat) |
| `RectMask2D` | `clip_contents` sur un `Control` |
| Pivot de sprite (en bas au centre...) | Marge transparente autour de l'image (`AtlasTexture.margin`) : le pivot tombe au centre |
| `LayerMask` + layers nommés | Calques de collision 2D (`world`, `player`, `pickups`, `interactables`, `damageable`) |
| `Collider2D` trigger + `OnTriggerEnter2D` sur une hitbox | `Area2D` + signal `AreaEntered`, masque = calques visés |
| `Image.color.a` / `SetSiblingIndex` | `Modulate.A` / `MoveChild` |
| États d'Animator enchaînés par triggers (pêche) | `PlayerMovements.BeginExternalControl` + signal de fin d'animation : le script enchaîne lui-même |
| Courbe `m_FlipX` d'un clip | `FlipH` du sprite, d'après la métadonnée `flip_h` de `player_frames.tres` |
| `List<struct Entry>` sérialisée | Tableau de petites `Resource` (`FishLootEntry`) |
| Coroutine de génération (`yield return null` par bloc) | Méthode `async` + `await` de la frame suivante |
| `Mathf.PerlinNoise` / `Random.InitState` | `FastNoiseLite` (Perlin, ramené à [0, 1]) / `RandomNumberGenerator` avec la graine |
| Tilemap de décor à cases de 1/3 d'unité | `DecorLayer` : un seul nœud qui dessine tout le décor dans `_Draw` |
| `SlopeConnectedRuleTile` (pentes amies) | `RuleTile.Matches` + liste `connects` dans `rule_tiles.json` |
| Références entre prefab joueur et UI dans l'Inspecteur | Groupes (`player_inventory`, `player_controller`, `inventory_view`) : deux scènes séparées se trouvent sans câblage |
| `Vector2Int`, Y vers le haut, 1 unité = 16 px | `Vector2I`, **Y vers le bas**, 1 unité = 1 px |

⚠️ **Axe Y inversé** : dans Godot, « haut » vaut `(0, -1)`. Les cartes et directions venues d'Unity sont retournées.

### Écarts volontaires avec le code Unity

- `CropData._seed` a disparu : graine → culture → graine est une référence circulaire que Godot ne sait pas charger.
  La graine se passe elle-même à la culture au moment de planter (`CropInstance.Init(cropData, cell, seed)`).
- Les tuiles labourées / arrosées reprennent les **mêmes coordonnées d'atlas** que la case de sol, dans la source
  « Tilled » / « Watered » de la même feuille (au lieu d'une correspondance sprite → sprite par rectangle).
- `PlayerController.Use()` est ignoré pendant une animation d'action (une seule action à la fois).
- La taille de l'inventaire et de la toolbar est réglée sur `PlayerInventory` (dans Unity elle venait de l'UI).
- `Storage` hérite de `Node2D` (et pas `Node`) : un coffre est un stockage posé dans le monde, il lui faut une position.
- L'interface du coffre est posée dans `hud.tscn` au lieu d'être instanciée depuis `Resources` à la première ouverture.
  Son « parchemin » n'a plus besoin du masque ni de la copie du cadre : le `NinePatchRect` garde ses bordures à toute taille.
- Pas de barre de défilement : les 36 cases de l'inventaire et les 20 du coffre tiennent à l'écran.
- Glisser-déposer entre les cases (nouveau, absent d'Unity) : `Storage.MoveTo` + le drag & drop intégré aux `Control`.
- `Tree` s'appelle `WorldTree` : Godot a déjà une classe `Tree` (un contrôle d'interface).
- La hitbox de la hache part du centre du corps ; dans Unity elle partait de la racine du prefab, décalée d'environ
  7 px vers la gauche par rapport au corps.
- `TimeUI` : ciel, flèche et portrait animé. L'heure en texte (`ClockLabel`) est un ajout.
- Portrait : l'Animator `UserCharacter` (36 clips, 7 humeurs) est transcrit en code dans `PortraitStateMachine`
  (logique pure : transitions, temps de sortie, Any State, déclencheurs) ; `UICharacterAnimator` joue le clip choisi.
  Écarts : pas de fondu entre clips ; le point de sortie d'un clip qui boucle est testé une fois par boucle ;
  une transition « Any State » vers l'état courant arrête l'évaluation (sinon « AngryYap » rebasculait sur « Yap »).
  Les bizarreries de l'Animator sont conservées : « Tired » et « Blink » n'ont aucune transition entrante (jamais joués),
  et depuis Sleep_Loop / VerySleep_Loop la transition vers « _End » passe avant celles d'intensité.
- Comme dans Unity, rien dans le jeu n'appelle `CharacterMoodController` (SetAmbient / React) : pour voir une humeur,
  régler `_debugEmotion` (et `_debugIntensity`) sur le nœud `CharacterMoodController` du HUD.
- Pêche : sans Animator, `PlayerFishing` enchaîne lui-même `fishing_start` → `fishing_idle` → `fishing_struggle`
  → `show` (pris) ou `fishing_quit` (raté) ; la session se termine à la fin de l'animation (sécurité : 8 s).
- `FishLootTable._entries` est un `Array<Resource>` (et non `FishLootEntry[]`) pour garder un `.tres` simple.
- Monde procédural : même algorithme et mêmes réglages (`SO_WorldGenConfig` : graine 1, taille 200), mais le bruit
  de Godot n'est pas celui d'Unity : à graine égale, la carte n'est pas la même que dans Unity.
- La grille du monde est en coordonnées Godot (sud = y + 1) : les pentes sont cherchées sur les bords SUD des plateaux,
  comme dans Unity ; le reste (biomes, lacs, rivières) est symétrique. L'empreinte des objets de décor s'étend vers le nord.
- Le monde calcule tout (relief, pentes) avant de peindre ; Unity peignait puis posait les pentes. Rendu identique.
- Ajout : un anneau d'océan bloquant de 30 cases autour de la grille (les îles touchent le bord de la carte).
- Le point d'apparition évite le pied des pentes ; l'écran de chargement affiche la progression (barre + étape).
- Le décor ne s'anime pas (les tuiles animées du décor Unity n'ont qu'une image).
- Marche sur 8 directions : les 4 clips diagonaux (feuille « teemo 8 directions », cases de 16x16) sont convertis avec une
  marge transparente pour tomber au centre d'une case de 48x48, comme le pivot Unity. `PlayerMovements.MovingAnimation`
  choisit la diagonale quand le plus petit axe dépasse ~41 % du plus grand ; à l'arrêt le personnage regarde en 4 directions.
  Les clips diagonaux durent 0,32 s contre 0,85 s pour les autres : `WalkCycleRatio` les ralentit pour que tous les pas
  bouclent en autant de temps (le blend tree d'Unity synchronisait les clips), sinon les jambes allaient 2,7 fois trop vite.
- Les animations du personnage sont lues directement dans les `.anim` Unity (la feuille est découpée en 48×48) ;
  le `model.json` de la skill `ninou-character-anims` ne sert plus au port.

## Avancement

- [x] **Étape 1 — socle** : projet C#, TileSet + RuleTiles convertis (sol, falaise, eau, collisions),
      grille logique (`TileGridData`, `ITileGrid`, `TileGridLocator`, `TestTileGrid`), `ItemData`, `Storage`,
      joueur (déplacement, animations idle/moving/chop/hoe, verrou pendant une action), caméra.
- [x] **Étape 2 — outils et culture** : `ToolData`/`IUsable`, `PlayerController` (cible, slot actif, molette, 1-0),
      `PlayerInventory`, houe + `FarmTilemapView` (tuiles labourées / arrosées), arrosoir + `SpriteOneShot` (eau),
      `SeedData`, `CropData` (15 cultures), `CropInstance`, `TimeManager`, `Dropable`, `ItemPickable`,
      `PlayerCollector`, `DebugHud` provisoire. Cycle labourer → arroser → planter → pousser → récolter → ramasser testé.
- [x] **Étape 3 — UI** : `UIStorage`, `UIInventory` (toolbar 9 + grille 36, touche I), `UIToolbar` + `MenuSelector`,
      `UISlot` (animations d'ajout / d'empilement / de clic), `UIChest` (ouverture en parchemin), `Chest`,
      `IInteractable`, `PlayerInteractor` (touche E, indication au-dessus du joueur), `ClockLabel`.
- [x] **Étape 4 — monde interactif** : `AxeData`, `PlayerActions`, hitbox `AAction`, `IDamageable`, arbres (`WorldTree` :
      coup, chute du côté opposé, bois), aimant (`Magnet`, `Magnetable`), `TimeUI` (ciel + flèche),
      glisser-déposer entre inventaire, toolbar et coffre.
- [x] **Étape 4 bis — portrait animé** : `PortraitStateMachine` (Animator `UserCharacter`), `UICharacterAnimator`,
      `CharacterMoodController` ; 36 animations de portrait converties.
- [x] **Étape 5 — pêche** : `FishingRodData`, `PlayerFishing`, `FishingGauge` (logique pure), `UIFishingGauge`,
      `FishData`, `FishLootTable` (3 poissons pondérés), animations de pêche et « show » (feuilles front / back / side).
- [x] **Étape 6 — monde procédural** : `GridWorld` (Perlin + falloff, 5 biomes, falaises lissées, lacs, rivières,
      pentes, génération étalée sur plusieurs frames), `WorldItemsGeneration` + `DecorLayer` (décor et arbres),
      `WorldGenConfig`, `GameManager` (apparition du joueur, écran de chargement), RuleTiles des 5 biomes.
