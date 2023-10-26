using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;


[System.Serializable]
public class PlayerInfo
{
    public FixedString128Bytes name;
    public ulong clientId;
    public FixedString128Bytes lobbyPlayerId;
    public int team;
    public LobbyManager.PlayerCharacter playerCharacter;
    public GameObject playerObject;
}
