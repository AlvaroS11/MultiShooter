using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

[System.Serializable]
public class PlayerInfo
{

    //network variables?
    public FixedString128Bytes name;
    public ulong clientId;
    public FixedString128Bytes lobbyPlayerId;
    public int team;
    public LobbyManager.PlayerCharacter playerCharacter;
    public GameObject playerObject;
    public bool isOwner;
    public int kills;
    public int deaths;

    public bool isDeleted;
    public PlayerSingleStat PlayerSingleStat;


    public void UpdateStats()
    {
        PlayerSingleStat.team.text = team.ToString();
        //PlayerSingleStat.kills = kills;

        PlayerSingleStat.killsDeaths.text = kills + "/" + deaths;
    }
}

