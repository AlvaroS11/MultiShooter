using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameAssets : MonoBehaviour {



    public static GameAssets Instance { get; private set; }


    [SerializeField] private Sprite marineSprite;
    [SerializeField] private Sprite ninjaSprite;
    [SerializeField] private Sprite zombieSprite;
    [SerializeField] private Sprite noPredSprite;

    [SerializeField] private GameObject marineGameObject;
    [SerializeField] private GameObject ninjaGameObject;
    [SerializeField] private GameObject zombieGameObject;
    [SerializeField] private GameObject noPredGameObject;


    [SerializeField] public TextMeshProUGUI pingText;

    [SerializeField] public GameObject stats;


    [SerializeField] public EndGame endGame;
    [SerializeField] public GameObject messageGameObject;
    [SerializeField] public TextMeshProUGUI messageText;

    [SerializeField] public GameObject teamsText;


    private void Awake() {
        Instance = this;
    }

    public Sprite GetSprite(LobbyManager.PlayerCharacter playerCharacter) {
        switch (playerCharacter) {
            default:
            case LobbyManager.PlayerCharacter.Marine:   return marineSprite;
            case LobbyManager.PlayerCharacter.Ninja:    return ninjaSprite;
            case LobbyManager.PlayerCharacter.Zombie:   return zombieSprite;
            case LobbyManager.PlayerCharacter.NoPred: return noPredSprite;

        }
    }

    public GameObject GetPrefab(LobbyManager.PlayerCharacter playerCharacter)
    {
        switch (playerCharacter)
        {
            default:
            case LobbyManager.PlayerCharacter.Marine: return marineGameObject;
            case LobbyManager.PlayerCharacter.Ninja: return ninjaGameObject;
            case LobbyManager.PlayerCharacter.Zombie: return zombieGameObject;
            case LobbyManager.PlayerCharacter.NoPred: return noPredGameObject;
        }
    }

}