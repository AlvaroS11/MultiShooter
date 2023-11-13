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
   // public NetworkVariable<FixedString128Bytes> PlayerLobbyId = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> PlayerTeam = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
   
    public LobbyManager.PlayerCharacter playerCharacterr;

    public int PlayerInfoIndex;

    [SerializeField]
    private Joystick joystick;

    [SerializeField]
    private Joystick joystickShoot;
    

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


   // public Animation inmuneAnimation;
    public Animator animator;

    //[SerializeField]
    public NetworkVariable<bool> isInmune = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool isOwnPlayer = false;


    [SerializeField]
    private bool aiming;

    [SerializeField]
    private Vector3 lastAimedPos;



    private float previousMov;
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
        CanvasDeath.SetActive(false);
        isOwnPlayer = true;

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || !Application.isFocused) return;
        //Movement for pc

#if UNITY_STANDALONE_WIN


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
                gun.AimWeapon(moveDestination);

                if (Input.GetMouseButtonDown(0))
                {
                    gun.PlayerFireServerRpc(moveDestination, NetworkManager.Singleton.LocalClientId);
                }
            }
            else
                gun.StopAim();
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
                gun.PlayerFireServerRpc(moveDestination, NetworkManager.Singleton.LocalClientId);
            }
        }

        if(IsServer)
        {
            if (isHealthing)
            {
               // life.Value = 
            }
        }



#elif UNITY_ANDROID  //ANDROID

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
            movPos.z = 1;
        if (joystick.Vertical <= -.2f)
            movPos.z = -1;

        if (movPos != Vector3.zero)
        {
            MovePlayerPhoneServerRpc(movPos);
        }

        MoveCamera();


        //Aim And Shoot
        Vector3 shootPos = new Vector3();

        shootPos.x += joystickShoot.Horizontal;
        shootPos.z += joystickShoot.Vertical;

        //Debug.Log(shootPos);
        if (shootPos != Vector3.zero)
        {
      //      Debug.Log("Aiming!! " + shootPos);
            aiming = true;
            lastAimedPos = gun.AimWeaponMobile(shootPos);
            previousMov = shootPos.magnitude;
        }
        else
        {
            gun.StopAim();
            if(aiming == true)
            {
                Debug.Log(previousMov);
                if(previousMov >= 0.22)
                {
                    //Debug.Log(shootPos);
                    gun.PlayerFireServerMobileServerRpc(lastAimedPos, NetworkManager.Singleton.LocalClientId);
                    aiming = false;
                }
                
                    
            }
        }


        /*  if(joystickShoot.Horizontal<= .2f && joystickShoot.Horizontal >= .2f && joystickShoot.Vertical <= .2f && joystickShoot.Vertical >= .2f)
          {
              //Vibrate, cancel and set to 0
          }
        */

        //   if (joystickShoot.)

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

    [ServerRpc]
    private void MovePlayerPhoneServerRpc(Vector3 input)
    {
        //TO DO CHECKs
        transform.position = Vector3.MoveTowards(transform.position, transform.position + input, Time.deltaTime * speed);

        if (!firing)
        {
            Quaternion newRotation = Quaternion.LookRotation(input);
            transform.rotation = newRotation;
        }
    }

   /* [ClientRpc]
    public void setPlayerLifeBarsClientRpc(ulong clientId, int team)
    {
        if(team != PlayerTeam.Value)
        {
            PlayerManager enemy = OnlineManager.Instance.playerList.Find(x => x.clientId == clientId).playerObject.GetComponent<PlayerManager>();
            healthUI.healthBar.color = Color.red;
            Debug.Log("CHANGING COLORS!!");
        }
    }
   */

    public override void OnNetworkSpawn()
    {
        Player player = LobbyManager.Instance.GetPlayerOrCreate();
        Initialized();

        life = new NetworkVariable<int>(MaxLife, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


        nameText.text = PlayerName.Value.ToString();


        if (!IsServer)
        {
            PlayerInfo player1 = OnlineManager.Instance.playerList.Find(x => x.name == PlayerName.Value);
            player1.playerObject = gameObject;
            player1.clientId = NetworkManager.Singleton.LocalClientId;
            Debug.Log("ddd" + NetworkManager.Singleton.LocalClientId);
           // Debug.Log("ddd" + NetworkManager.Singleton.LocalClientId);

        }
        //        moveDestination = transform.position;

        if (IsOwner)
        {
           joystickShoot = Assets.Instance.joystickShoot;
           joystick = Assets.Instance.joystick;

        /*    foreach(PlayerInfo playerInfo in OnlineManager.Instance.playerList)
            {
                if(playerInfo.team != PlayerTeam.Value)
                {
                    Debug.Log("DIFERENT TEAMS!!");
                    playerInfo.playerObject.GetComponent<PlayerManager>().healthUI.healthBar.color = Color.red;
                    //Might not be spawned yet!!
                    Debug.Log(playerInfo.playerObject.GetComponent<PlayerManager>().healthUI.healthBar.color);
                }
            }
        */
        }



#if UNITY_ANDROID



#elif UNITY_STANDALONE_WIN
        if (IsOwner)
        {
            joystickShoot.gameObject.SetActive(false);
            joystick.gameObject.SetActive(false);
        }


#endif


    }


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


    [ServerRpc(RequireOwnership = false)]
    public void DamageTakenServerRpc(int dmg, int shooterIndex, int playerHittedIndex)
    {
        life.Value -= dmg;
        if (life.Value <= 0)
        {
            Debug.Log("DAMAGE TO 0" + life.Value);
            //Manage player
            OnlineManager.Instance.PlayerDeath(OnlineManager.Instance.playerList[playerHittedIndex].clientId);

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

}
