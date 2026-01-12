# LAST LOOP – The Forgotten Witness
Prototype Narratif Interactif | Unity 2022+ | C#

## CONCEPT

LAST LOOP est un jeu narratif en boucle temporelle. Le joueur se déplace sur un plateau circulaire de 6 tuiles via lancer de dé. Un système de 7 flags persistants modifie progressivement les dialogues d'un NPC central appelé Le Témoin. Objectif : découvrir la vérité et briser la boucle.

## GAMEPLAY

Appuyer sur Espace lance un dé qui déplace le joueur sur 6 types de tuiles : Témoin pour dialoguer, Ruines pour explorer, Combat pour gagner ou perdre des ressources, Autel à activer avec un objet, Vide sans effet, et Relique donnant l'objet clé. Les 7 flags narratifs persistent entre les boucles. Victoire en brisant la boucle après la bonne séquence d'actions. Défaite si ressources à zéro.

## ARCHITECTURE

NarrativeStateManager est un Singleton MonoBehaviour avec DontDestroyOnLoad gérant les flags via Dictionary et la sauvegarde via PlayerPrefs JSON. DialogueManager charge des ScriptableObjects DialogueNode depuis Resources et affiche le bon dialogue selon les flags actifs. LoopController instancie les tuiles en cercle via calcul trigonométrique et gère le mouvement par Coroutines. TileBase est une classe abstraite avec méthode OnPlayerEnter override par 6 classes héritées détectant le joueur via OnTriggerEnter.

## SYSTÈMES

15 scripts C# en 4 catégories : Core avec NarrativeStateManager singleton et GameManager, Systems avec DialogueManager et LoopController, Actors avec WitnessNPC, PlayerController et 7 tiles, UI avec HUDManager, DialogueBoxUI, ChoiceButtonUI, VictoryScreen et DefeatScreen. 7 DialogueNode ScriptableObjects configurables dans l'inspecteur. 2 enums TileType et NarrativeFlag. Sauvegarde automatique à chaque SetFlag.

## FLUX NARRATIF

Rencontrer Témoin active RENCONTRE_TEMOIN. Explorer Ruines active VISITE_RUINES et change le dialogue. Trouver Relique active TROUVE_RELIQUE. Activer Autel avec Relique active ACTIVE_AUTEL. Retour au Témoin propose un choix : briser la boucle active VERITE_TERMINEE et charge VictoryScene, continuer active CONSCIENT_BOUCLE et maintient la boucle.

## TECHNOLOGIES

Singleton pattern pour state management. ScriptableObjects pour données narratives. Coroutines pour animations. TextMeshPro pour UI. PlayerPrefs avec JsonUtility pour sauvegarde. LINQ pour vérification de flags. Events pour découplage UI-gameplay. Input.GetKeyDown pour compatibilité.

## STRUCTURE

Scripts/Core contient NarrativeStateManager avec 9 méthodes publiques et GameManager. Scripts/Systems contient DialogueManager chargeant depuis Resources et LoopController avec RollDice Coroutine. Scripts/Actors contient WitnessNPC, PlayerController et dossier Tiles avec TileBase abstraite et 6 héritiers. Scripts/UI contient 5 managers de Canvas. ScriptableObjects/Dialogues contient 7 DialogueNode assets. Prefabs contient 7 tuiles colorées. Scenes contient GameScene.

## TEST

Ouvrir GameScene et Play. Espace pour lancer le dé. Séquence victoire : Témoin, Ruines, Relique, Autel, Témoin puis choix briser. Durée 3-5 minutes. Défaite si ressources zéro. Fermer et relancer pour vérifier persistence PlayerPrefs.

## MÉTRIQUES

2000 lignes C# sur 15 scripts. 4-6 heures développement. 7 DialogueNode assets. 7 flags persistants. 2 fins plus défaite. Architecture SOLID modulaire réutilisable.

## CONCLUSION

Prototype production-ready démontrant state management persistant via Singleton, dialogue data-driven via ScriptableObjects, game loop avec Coroutines, architecture modulaire SOLID, et UI complète TextMeshPro. Code commenté en français avec Debug.Log. Base extensible pour jeux narratifs plus complet.
