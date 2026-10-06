# Système de tuiles labourées par biome

Guide de mise en place du rendu des cases labourées (et plus tard arrosées) dans Ninou's Farm.
Ce document explique **quoi** faire, **dans quel ordre** et **pourquoi**. Le code est à écrire toi-même.

---

## 1. Le besoin

Quand la houe laboure une case, la case doit afficher la terre labourée **du biome** de cette case :

| Biome | Tuile labourée |
|---|---|
| Plaine (HILLS) | `HoedGrass` |
| Désert (DESERT) | `HoedSand` |
| … | … |

La **donnée** existe déjà : `TileData` contient `Biome`, `State` (`NONE` / `HOED`) et `IsWatered`.
Il manque uniquement la partie **visuelle**, synchronisée avec cette donnée.

---

## 2. Les fausses bonnes idées

| Idée | Pourquoi l'écarter |
|---|---|
| Changer le `Sprite` d'un asset `Tile` au runtime | Un asset `Tile` est **partagé** par toutes les cases qui l'utilisent : on change toute la carte d'un coup, et en éditeur la modification reste enregistrée dans le projet. |
| Écrire un `TileBase` personnalisé qui lit `TileData` pour choisir son sprite | On couple un asset de rendu à la logique du jeu (grille, singleton). C'est difficile à tester, et ça ne marche pas tel quel dans la scène de test. |
| Remplacer la tuile du sol (`_groundTilemap`) par une tuile labourée | On perd la tuile d'origine (biome, variante). Dé-labourer oblige à la recalculer, et on casse les raccords des `RuleTile` du sol. |

**Principe retenu** : on ne modifie jamais une tuile. On choisit **quelle tuile poser** dans une cellule,
sur une **couche dédiée** posée au-dessus du sol.

---

## 3. Vue d'ensemble

```
            ┌──────────────────────────┐
 Houe ────► │ ITileGrid.SetTileState   │  (GridWorld ou TestTileGrid)
            └────────────┬─────────────┘
                         ▼
            ┌──────────────────────────┐
            │ TileGridData             │  écrit la donnée
            │  → OnTileChanged(cell)   │  puis prévient
            └────────────┬─────────────┘
                         ▼
            ┌──────────────────────────┐
            │ FarmTilemapView          │  relit TileData(cell)
            │  → SO_FarmTileSet        │  choisit la tuile du biome
            │  → farmTilemap.SetTile   │  pose ou efface
            └──────────────────────────┘
```

Quatre briques, chacune avec **une seule responsabilité** :

| Brique | Responsabilité | Existe ? |
|---|---|---|
| `TileData` | Stocker l'état d'une case | ✅ |
| `SO_FarmTileSet` | Associer (biome, état) → tuile | à créer |
| `TileGridData.OnTileChanged` | Signaler qu'une case a changé | à créer |
| `FarmTilemapView` | Afficher l'état des cases sur la Tilemap de ferme | à créer |

---

## 4. Étape A — Les assets visuels

**Objectif** : disposer des tuiles labourées de chaque biome.

1. Découpe les sprites de terre labourée par biome (Sprite Editor).
2. Pour chaque biome, crée une **RuleTile** labourée (ex. `RuleTile_Hoed_Grass`, `RuleTile_Hoed_Sand`).
   - Pourquoi une RuleTile et pas une simple `Tile` : deux cases labourées côte à côte doivent se raccorder proprement, comme le sol. Le projet a déjà `RuleTileGenerator.cs` dans `Tools/Editor` pour t'aider.
3. Prévois une tuile **par défaut** (fallback), utilisée si un biome n'a pas encore sa propre tuile.

**Checkpoint** : peins quelques cases à la main avec la palette, et vérifie que les bords se raccordent bien.

---

## 5. Étape B — Le catalogue `SO_FarmTileSet`

**Objectif** : une source de vérité unique, réglable dans l'inspecteur, pour savoir quelle tuile afficher.

- Un `ScriptableObject` (à ranger par exemple dans `Scripts/World/Farming/`).
- Contenu : une liste d'entrées `{ Biome, HoedTile, HoedWateredTile }`, plus un `DefaultHoedTile`.
- Une seule méthode publique, du type :
  `TileBase GetTile(BiomeType biome, TileState state, bool isWatered)`
  - `state == NONE` → renvoie `null` (rien à afficher).
  - Biome absent de la liste → renvoie le fallback.
  - `isWatered` sans tuile humide renseignée → renvoie la tuile sèche.

C'est le même modèle que `_tileMappings` / `GetRuleTile` dans `GridWorld` : piloté par les données, sans `switch` sur les biomes dans le code.

**Questions à trancher**
- Une liste parcourue à chaque appel, ou un dictionnaire construit une fois (`OnEnable`) ? Combien d'appels par seconde y aura-t-il vraiment ?
- Que doit faire le catalogue si une entrée a un `HoedTile` vide : renvoyer le fallback, ou signaler l'erreur ?

**Checkpoint** : crée l'asset, remplis plaine et désert, et appelle `GetTile` depuis un `[ContextMenu]` pour vérifier les retours.

---

## 6. Étape C — La notification `OnTileChanged`

**Objectif** : qu'aucune écriture d'état ne puisse être oubliée par l'affichage.

- Dans `TileGridData` : un `event Action<Vector2Int> OnTileChanged`.
- `SetTileState` et `SetTileWatered` le déclenchent **après** avoir écrit, et **seulement si** l'écriture a réussi.
- Dans `ITileGrid` : exposer cet événement, pour que la vue s'abonne sans connaître l'implémentation.

**Pourquoi ici et pas dans la houe ?** Le gotcha n°1 du `CLAUDE.md` dit qu'il faut mettre à jour la donnée **et** le visuel.
Si c'est l'outil qui s'en charge, chaque futur outil (arrosoir, graines, sort…) devra y penser.
Si c'est `TileGridData` qui notifie, la synchronisation est **garantie par construction**, pour `GridWorld` **et** pour `TestTileGrid` à la fois.

**Questions à trancher**
- Faut-il notifier si la nouvelle valeur est identique à l'ancienne (labourer une case déjà labourée) ?
- Lors d'une régénération du monde, `GridWorld` recrée `TileGridData` : que deviennent les abonnés de l'ancienne instance ?

**Checkpoint** : abonne un simple `Debug.Log` à l'événement, puis laboure une case : le log doit apparaître une fois, avec la bonne coordonnée.

---

## 7. Étape D — La vue `FarmTilemapView`

**Objectif** : traduire l'état des cases en tuiles sur la Tilemap de ferme.

**Dans la scène**
- Nouvelle Tilemap `Tilemap_FARM`, enfant du même `Grid` que les autres.
- Ordre de rendu : **au-dessus** du sol (`Tilemap_GROUND`), **en dessous** des objets et du joueur.
- Pas de `TilemapCollider2D` : une case labourée ne bloque pas le déplacement.

**Le composant**
- Références sérialisées : la Tilemap de ferme et le `SO_FarmTileSet`.
- S'abonne à `OnTileChanged` de `TileGridLocator.Current`, et se désabonne dans `OnDisable`.
- À chaque notification : relire `GetTileAt(cell)` → `GetTile(biome, state, watered)` → `SetTile(cell, tuile)`
  (`null` efface la cellule).
- Conversion de coordonnées : `new Vector3Int(cell.x, cell.y, 0)`, la même convention que le reste du projet.

**Questions à trancher**
- **L'ordre d'initialisation** : la grille s'enregistre dans son `Awake`. Si la vue s'abonne dans son propre `Awake`, `Current` peut encore être `null`. Faut-il s'abonner dans `Start`, ou que le locator expose un événement « grille enregistrée » ?
- La vue doit-elle **redessiner toute la grille** à l'abonnement (utile après un chargement de sauvegarde), ou seulement réagir aux changements ?

**Checkpoint** : dans `Test_Tools`, un `[ContextMenu]` qui laboure la case (8,6) doit faire apparaître la terre labourée de plaine à l'écran.

---

## 8. Étape E — Intégrations

| Où | Quoi |
|---|---|
| `ToolsTestSceneBuilder` | Créer `Tilemap_FARM`, ajouter `FarmTilemapView`, et peindre une zone d'un **second biome** (désert) pour tester la correspondance par biome. Rappel : la scène de test est générée, ne la modifie pas à la main. |
| `TestTileGrid` | Rester une simple délégation : aucune logique de rendu dedans. Prévoir de lire le biome par zone, si la carte de test en contient plusieurs. |
| `GridWorld` | Ajouter `Tilemap_FARM` dans `ResetWorld()` (sinon les cases labourées survivent à un « Regenerate World »), et la brancher dans `Test_1`. |
| `HoeData` | Leçon 3 : `CanBeFarmed(cell)` puis `SetTileState(cell, HOED)`. L'outil ne touche **jamais** à une Tilemap. |

---

## 9. Évolutions prévues

- **Arrosage** : rien de nouveau à construire. `SetTileWatered` notifie déjà, et le catalogue sait renvoyer la tuile humide.
- **Assèchement quotidien** (leçon 5) : remettre `IsWatered` à `false` notifie la vue automatiquement.
- **Cultures** (leçons 4-6) : une **seconde** couche (`Tilemap_CROPS`) avec sa propre vue, sur le même principe. On ne mélange pas sol labouré et plantes.

---

## 10. Récapitulatif de validation

- [ ] Les RuleTiles labourées existent pour au moins deux biomes, plus un fallback.
- [ ] `SO_FarmTileSet.GetTile` renvoie la bonne tuile pour chaque combinaison (biome, état, humidité).
- [ ] `OnTileChanged` se déclenche une fois par écriture réussie, et jamais en cas d'échec (hors carte).
- [ ] Labourer une case de plaine affiche `HoedGrass` ; une case de désert affiche `HoedSand`.
- [ ] Dé-labourer (`NONE`) efface la cellule, et le sol d'origine réapparaît intact.
- [ ] « Regenerate World » ne laisse aucune case labourée fantôme.
- [ ] La houe ne contient aucune référence à une Tilemap.
