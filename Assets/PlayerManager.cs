using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using Unity.Services.Lobbies.Models;
using System;
using TMPro;
using UnityEngine.AI;
using Unity.Collections;

public class PlayerManager : NetworkBehaviour
{
    private float speed = 3f;
    private Camera _mainCamera;
    public LayerMask floor;

    //  public GameObject Bullet;

    public Weapon gun;

    public int MaxLife = 100;

    public NetworkVariable<int> life = new NetworkVariable<int>();

    public UIPlayer healthUI;

    [SerializeField] private Transform playerPrefab;

    // public NetworkVariable<int> team = new NetworkVariable<int>();
    // public int team;


   /* public string PlayerLobbyId;
    public string PlayerName;
    public int PlayerTeam;
   */
    public NetworkVariable<FixedString128Bytes> PlayerLobbyId = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> PlayerTeam = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
   
    public LobbyManager.PlayerCharacter playerCharacterr;

    public int PlayerInfoIndex;

    [SerializeField]
    private Joystick joystick;


    [SerializeField]
    private TextMeshProUGUI nameText;



    public NavMeshAgent playerNavMesh;

    public bool firing; //Server only

    public bool isHealthing;


    [SerializeField]
    private int healthDamageWait = 3;

    [SerializeField]
    private int healthInterval = 1;

    public NetworkVariable<int> healthBySecond = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);



    [SerializeField]
    private GameObject CanvasDeath;


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
//        life = new NetworkVariable<int>(MaxLife, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        //  team = new NetworkVariable<int>(MaxLife, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        CanvasDeath.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || !Application.isFocused) return;
        //Movement for pc

#if UNITY_STANDALONE_WIN

        //CAMBIAR PARA QUE NO ESTÉ SIEMPRE MOVIENDOSE, HACER COMO EN EL LOL

        /* if (Input.GetMouseButtonDown(1))
         {
             // MovePlayerServerRpc(ray);
                Debug.Log("ddd");
                Vector3 dest = Input.mousePosition;

                Ray ray = _mainCamera.ScreenPointToRay(dest);

                if (Physics.Raycast(ray, out RaycastHit hitData, 100, floor))
                {
                    moveDestination = hitData.point;
                    moveDestination.y = 0.5f;
                }
            }

            if (transform.position != moveDestination)
            {
                //  screenPosition = Input.mousePosition;


                    MovePlayerServerRpc(moveDestination);
                    MoveCamera();   
            }


             Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
             MovePlayerPcServerRpc(ray);
         }
        */


        //Meter todo esto en una función
        Vector3 receivedInput = Vector3.zero;
        if (Input.GetKey("d"))
        {
            receivedInput += Vector3.right;
        }
        if (Input.GetKey("a"))
        {
            receivedInput += Vector3.left;
        }
        if (Input.GetKey("w"))
        {
            receivedInput += Vector3.forward;
        }
        if (Input.GetKey("s"))
        {
            receivedInput += Vector3.back;
        }

        if (receivedInput != Vector3.zero)
        {
            receivedInput.Normalize();
            MovePlayerPcServerRpc(receivedInput);
        }
        MoveCamera();

        //Meter en una funcion
        if (Input.GetMouseButton(1))
        {
            Vector3 dest = Input.mousePosition;

            Ray ray = _mainCamera.ScreenPointToRay(dest);

            if (Physics.Raycast(ray, out RaycastHit hitData, 100, floor))
            {
                Vector3 moveDestination = hitData.point;
                moveDestination.y = 0.5f;

                Quaternion newRotation = Quaternion.LookRotation(moveDestination);
                transform.rotation = newRotation;
                gun.AimWeapon(moveDestination);

            }
        }
        else
            gun.StopAim();

        //Meter en una funcion
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 dest = Input.mousePosition;

            Ray ray = _mainCamera.ScreenPointToRay(dest);

            if (Physics.Raycast(ray, out RaycastHit hitData, 100, floor))
            {
                Vector3 moveDestination = hitData.point;
                moveDestination.y = 0.5f;
                gun.PlayerFireServerRpc(moveDestination);
            }
        }

        if(IsServer)
        {
            if (isHealthing)
            {
               // life.Value = 
            }
        }


#elif UNITY_STANDALONE_WIN  //ANDROID
    
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

    [ServerRpc]
    private void MovePlayerPcServerRpc(Vector3 input)
    {
        transform.position +=  input * Time.deltaTime * speed;
        // Vector3 targetDirection = input - transform.position;


        if (!firing)
        {
            Quaternion newRotation = Quaternion.LookRotation(input);
            transform.rotation = newRotation;
        }
        
      

    }


   /* [ServerRpc]
    private void MovePlayerPcServerRpc(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hitData, 100, floor))
        {
            playerNavMesh.destination = hitData.point;

           /* if (playerNavMesh.destination != transform.position)
            {
                Vector3 targetDirection = playerNavMesh.destination - transform.position;
                transform.forward = -targetDirection;
            }
           
           
        }


    }
   */

    public override void OnNetworkSpawn()
    {
        Player player = LobbyManager.Instance.GetPlayerOrCreate();
        Initialized();

        life = new NetworkVariable<int>(MaxLife, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


        nameText.text = PlayerName.Value.ToString();

        //        moveDestination = transform.position;


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
    private void MovePlayerServerRpc(Vector3 mouseWorldCoordinates)
    {
        /* if (Physics.Raycast(ray, out RaycastHit hitData, 100, floor))
         {
                mouseWorldCoordinates = hitData.point;
                mouseWorldCoordinates.y = 0.5f;


                transform.position = Vector3.MoveTowards(transform.position, mouseWorldCoordinates, Time.deltaTime * speed);

                if (mouseWorldCoordinates != transform.position)
                {
                    Vector3 targetDirection = mouseWorldCoordinates - transform.position;
                    transform.forward = -targetDirection;
                }
        */

        //            playerNavMesh.SetDestination(hitData.point);

        transform.position = Vector3.MoveTowards(transform.position, mouseWorldCoordinates, Time.deltaTime * speed);

        if (mouseWorldCoordinates != transform.position)
        {
            Vector3 targetDirection = mouseWorldCoordinates - transform.position;
            transform.forward = -targetDirection;
        }
    }


    [ServerRpc]
    private void MovePlayerPhoneServerRpc(Vector3 input)
    {
        //TO DO CHECKs
        transform.position = Vector3.MoveTowards(transform.position, transform.position + input, Time.deltaTime * speed);
    }

  /*  [ServerRpc]
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
//    }


    [ServerRpc(RequireOwnership = false)]
    public void DamageTakenServerRpc(int dmg, int shooterIndex, int playerHittedIndex)
    {
        life.Value -= dmg;
        if (life.Value <= 0)
        {
            Debug.Log("DAMAGE TO 0" + life.Value);
            //Manage player
           // OnlineManager.Instance.PlayerDeath(OnlineManager.Instance.playerList[playerHittedIndex].clientId);

            OnlineManager.Instance.ChangeScoreServerRpc(shooterIndex);
            life.Value = MaxLife;

        }
        else
        {
            //StopCoroutine(WaitToHealth());
          //  StopCoroutine(HealthByTime());
            StopAllCoroutines();            //Care with this, it stops all the Couroutines of this script!!


            StartCoroutine(WaitToHealth());
        }
        healthUI.TakeDamageClientRpc(life.Value);
    }



    private IEnumerator WaitToHealth()
    {
        yield return new WaitForSeconds(healthDamageWait);
        StartCoroutine(HealthByTime());
    }

    private IEnumerator HealthByTime()
    {
        Debug.Log("HEALTHING!!");
        yield return new WaitForSeconds(healthInterval);
        life.Value += healthBySecond.Value;

        if (life.Value >= MaxLife)
            life.Value = MaxLife;
        else if (life.Value < MaxLife)
            StartCoroutine(HealthByTime());

        healthUI.TakeDamageClientRpc(life.Value);
        //Add field so if it is positive (health) it shows an animation and if negative it shows other animation

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
