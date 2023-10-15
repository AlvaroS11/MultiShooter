using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class PlayerInfo
{
    public string name;
    public ulong clientId;
    public string lobbyPlayerId;
    public int team;
    public LobbyManager.PlayerCharacter playerCharacter;
}
