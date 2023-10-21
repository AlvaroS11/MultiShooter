using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using Unity.Services.Lobbies.Models;
using System;
using TMPro;

public class PlayerManager : NetworkBehaviour
{
    private float speed = 3f;
    private float bulletSpeed = 10f;
    private Camera _mainCamera;
    private Vector3 _mouseInput;
    Vector3 mouseWorldCoordinates;
    Vector3 screenPosition;

    public LayerMask floor;


    //  public GameObject Bullet;

    public Weapon gun;

    public int MaxLife = 100;

    public NetworkVariable<int> life = new NetworkVariable<int>();

    public UIPlayer healthUI;

    [SerializeField] private Transform playerPrefab;

    // public NetworkVariable<int> team = new NetworkVariable<int>();
    // public int team;


    public string PlayerLobbyId;
    public string PlayerName;
    public int PlayerTeam;
    public LobbyManager.PlayerCharacter playerCharacterr;

    public int PlayerInfoIndex;

    [SerializeField]
    private Joystick joystick;


    [SerializeField]
    private TextMeshProUGUI nameText;


    void Start()
    {
        Initialized();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        //Initialized();
    }

    private void Initialized()
    {
        if (!IsOwner) return;
        _mainCamera = Camera.main;
        life = new NetworkVariable<int>(MaxLife, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        //  team = new NetworkVariable<int>(MaxLife, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || !Application.isFocused) return;
        //Movement for pc
        screenPosition = Input.mousePosition;

#if UNITY_STANDALONE_WIN

        //CAMBIAR PARA QUE NO ESTÉ SIEMPRE MOVIENDOSE, HACER COMO EN EL LOL
        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

        if (Input.GetMouseButton(1))
        {
            gun.AimWeapon();
        }
        else
            gun.StopAim();

        MovePlayerServerRpc(ray);

        MoveCamera();

        if (Input.GetMouseButtonDown(0))
        {
            PlayerFireServerRpc();
        }


#elif UNITY_STANDALONE_WIN
    
       Vector3 movPos = new Vector3();
        if (joystick.Horizontal >= .2f)
        {
            movPos.x = 1;
        }
        if (joystick.Horizontal <= -.2f)
        {
            movPos.x = -1;
        }
        if (joystick.Vertical >= .2f)
            movPos.y = 1;
        if (joystick.Vertical <= -.2f)
            movPos.y = -1;

        if (movPos != Vector3.zero)
        {
            MovePlayerPhoneServerRpc(movPos);
        }
    

#endif


        //  gun.StopAim();

    }

    public override void OnNetworkSpawn()
    {
        Player player = LobbyManager.Instance.GetPlayerOrCreate();
        Initialized();


    }


    /*  public override void OnNetworkSpawn()
      {
          if (IsServer)
          {
              NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
              NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
          }
      }

      private void OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
      {
          foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
          {
              Transform playerTransform = Instantiate(playerPrefab);
              playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
          }
      }

      private void OnClientDisconnectCallback(ulong clientId)
      {
          //autoTestGamePausedState = true;
      }

      */

    private void MoveCamera()
    {

        //PREDICCIÓN SE PODRÍA HACER AQUÍ??
        // if (Physics.Raycast(ray, out RaycastHit hitData, 100, floor))

        Initialized();


        _mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y + 15, transform.position.z - 5);

    }



    [ServerRpc]
    private void MovePlayerServerRpc(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hitData, 100, floor))
        {
            mouseWorldCoordinates = hitData.point;
            mouseWorldCoordinates.y = 0.5f;
            transform.position = Vector3.MoveTowards(transform.position, mouseWorldCoordinates, Time.deltaTime * speed);

            if (mouseWorldCoordinates != transform.position)
            {
                Vector3 targetDirection = mouseWorldCoordinates - transform.position;
                transform.forward = -targetDirection;
            }
        }
    }


    [ServerRpc]
    private void MovePlayerPhoneServerRpc(Vector3 input)
    {
        //TO DO CHECKs
        transform.position = Vector3.MoveTowards(transform.position, transform.position + input, Time.deltaTime * speed);
    }

    [ServerRpc]
    private void PlayerFireServerRpc()
    {
        // Vector3 spawnPoint = transform.position + new Vector3(0.0f, 0.0f, -0.5f);
        gun.PlayerFireServerRpc();


        /*   GameObject bulletGameObject = Instantiate(Bullet, transform.position, transform.rotation);
           bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
           bulletGameObject.transform.Rotate(90, 0, 0);
           bulletGameObject.GetComponent<NetworkObject>().Spawn();
        */
        //NetworkManager.Singleton.AddNetworkPrefab(bulletGameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DamageTakenServerRpc(int dmg, int shooterIndex)
    {
        life.Value -= dmg;
        if (life.Value <= 0)
        {
            OnlineManager.Instance.ChangeScoreServerRpc(shooterIndex);

            life.Value = MaxLife;
        }
        healthUI.TakeDamageClientRpc(life.Value);

    }


    //ClientRpc, set the names to all players!
    public void SetUIName()
    {
        nameText.text = PlayerName;
        Debug.LogFormat("ppp" + PlayerName, nameText.text);
    }


    private void OnMouseEnter()
    {
        // print(mouseWorldCoordinates);
    }



    //Receive player data from other players
    /* [ClientRpc]
      private void RequestDataFromClientsClientRpc()
      {
          // Buscar el nuevo jugador en la escena utilizando el NetworkObjectId
          NetworkObject newPlayer = NetworkObject.Find(newPlayerNetworkObjectId);

          if (newPlayer != null)
          {
              // Enviar los datos al nuevo cliente
              int maxLife = GetComponent<PlayerController>().MaxLife;
              SendMaxLifeDataToClientServerRpc(newPlayer.GetComponent<NetworkObject>().OwnerClientId, maxLife);
          }
      }
    */

}
