using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using System;
using TMPro;
using Unity.Collections;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField]
    private float speed = 20f;
    private Camera _mainCamera;
    public LayerMask floor;

    public Weapon gun;

    public int MaxLife = 100;

    public NetworkVariable<int> life = new NetworkVariable<int>();

    public UIPlayer healthUI;

    [HideInInspector]
    public NetworkVariable<FixedString128Bytes> PlayerLobbyId = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public NetworkVariable<int> PlayerTeam = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [HideInInspector]
    public LobbyManager.PlayerCharacter playerCharacterr;

    [HideInInspector]
    public NetworkVariable<int> playerChar = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    //0: Marine
    //1: Ninja
    //2: Zombie

    public int PlayerInfoIndex;
    public ulong clientId;

    //  [SerializeField]
    [HideInInspector]
    private Joystick joystick;

    // [SerializeField]
    [HideInInspector]
    private Joystick joystickShoot;


    [SerializeField]
    private TextMeshProUGUI nameText;

    //[HideInInspector]
    public NetworkVariable<bool> firing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


  //  public bool firing; //Server only
 //   public bool localFiring; //Client prediction only


    //public bool isHealthing;


    [SerializeField]
    private int healthDamageWait = 3;

    [SerializeField]
    private int healthInterval = 1;

    [HideInInspector]
    public NetworkVariable<int> healthBySecond = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);



    [SerializeField]
    private GameObject CanvasDeath;


    public Animator animator;

    [HideInInspector]
    public NetworkVariable<bool> isInmune = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool isOwnPlayer = false;


    private bool aiming;

    private Vector3 lastAimedPos;

    public GameObject body;

    public Animator bodyAnimator;

    private float previousMov;

    //    [SerializeField]
    public AudioSource noAmmoSound;

    static float ping;

    // Network variables should be value objects
    public struct InputPayload : INetworkSerializable
    {
        public int tick;
        public DateTime timestamp;
        public Vector3 inputVector;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref timestamp);
            serializer.SerializeValue(ref inputVector);
        }
    }

    //The state of the player
    public struct StatePayload : INetworkSerializable
    {
        public int tick;
        public Vector3 position;
        public Vector3 inputVector; // needed to know the direction in case of extrapolation and reconcialiation
        public DateTime timestamp;



        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            //     serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref inputVector);
            serializer.SerializeValue(ref timestamp);
        }
    }

    // Netcode general
    NetworkTimer networkTimer;
    const float k_serverTickRate = 60f; // 60 FPS
    const int k_bufferSize = 1024;

    // Netcode client specific
    CircularBuffer<StatePayload> clientStateBuffer;
    CircularBuffer<InputPayload> clientInputBuffer;
    StatePayload lastServerState;
    StatePayload lastProcessedState;

    ClientNetworkTransform clientNetworkTransform;

    // Netcode server specific
    CircularBuffer<StatePayload> serverStateBuffer;
    Queue<InputPayload> serverInputQueue;
    
    [Header("Netcode")]
    [SerializeField] float reconciliationCooldownTime = 1f;
    [SerializeField] float extrapolationCooldownTime = 1f;
    [SerializeField] float reconciliationThreshold = 10f;
    [SerializeField] GameObject serverCube;
    [SerializeField] GameObject clientCube;
    [SerializeField] float extrapolationLimit = 0.5f;
    [SerializeField] float extrapolationMinimum = 0.1f;
    [SerializeField] float extrapolationMultiplier = 10f;
    CountdownTimer reconciliationTimer;
    CountdownTimer extrapolationTimer;
    StatePayload extrapolationState;


    [Header("Ping")]
    DateTime sentMsg;

    [SerializeField] static TextMeshProUGUI pingText;

    public static DateTime previousTimeStamp = DateTime.Now;

    private Vector3 moveDestination = Vector3.zero;

    private void Awake()
    {
        networkTimer = new NetworkTimer(k_serverTickRate);
        clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
        clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);
        serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);


        serverInputQueue = new Queue<InputPayload>();

        reconciliationTimer = new CountdownTimer(reconciliationCooldownTime);
        extrapolationTimer = new CountdownTimer(extrapolationCooldownTime);

        clientNetworkTransform = GetComponent<ClientNetworkTransform>();


        reconciliationTimer.OnTimerStart += () => {
            extrapolationTimer.Stop();
        };

        extrapolationTimer.OnTimerStart += () => {
            reconciliationTimer.Stop();
            SwitchAuthorityMode(AuthorityMode.Server);

        };
        extrapolationTimer.OnTimerStop += () => {
            extrapolationState = default;
            SwitchAuthorityMode(AuthorityMode.Client);
        };
    }

    void SwitchAuthorityMode(AuthorityMode mode)
    {
        clientNetworkTransform.authorityMode = mode;
        bool shouldSync = mode == AuthorityMode.Client;
        clientNetworkTransform.SyncPositionX = shouldSync;
        clientNetworkTransform.SyncPositionY = shouldSync;
        clientNetworkTransform.SyncPositionZ = shouldSync;
    }

    void Start()
    {
        Initialized();

        if(IsClient && IsOwner && !IsHost)
        {
            sentMsg = DateTime.Now;
            //SendPing();
        }
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
        pingText = GameAssets.Instance.pingText;

    }

    void Update()
    {

        networkTimer.Update(Time.deltaTime);
        reconciliationTimer.Tick(Time.deltaTime);
        extrapolationTimer.Tick(Time.deltaTime);
        Extrapolate();

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (IsOwner)
                transform.position += transform.forward * 20f;
        }

        HandleInput();
    }

    //Manage shooting in every frame ignoring client tick
    private void HandleInput()
    {
        if (!IsClient || !IsOwner) return;
#if UNITY_STANDALONE_WIN

        //Aiming
        if (Input.GetMouseButton(1))
        {
            Vector3 dest = Input.mousePosition;

            Ray ray = _mainCamera.ScreenPointToRay(dest);

            if (Physics.Raycast(ray, out RaycastHit hitData, 100, floor))
            {
                moveDestination = hitData.point;
                moveDestination.y = 0.5f;
                gun.AimWeapon(moveDestination);


                //Aiming and shooting
                if (Input.GetMouseButtonDown(0) && firing.Value)
                    //noAmmoSound.Play();
                    noAmmoSound.PlayOneShot(noAmmoSound.clip);
                else if (Input.GetMouseButtonDown(0) && !gun.reloading)
                {
                    Vector3 targetDirection = moveDestination - transform.position;
                    gun.PlayerFireServerRpc(moveDestination, NetworkManager.Singleton.LocalClientId);
                    //localFiring = true;
                    transform.forward = targetDirection;

                }

            }
            else
                gun.StopAim();
        }
        else
        {
            gun.StopAim();

            //Just shooting
            if (Input.GetMouseButtonDown(0) && firing.Value)
            {
                noAmmoSound.PlayOneShot(noAmmoSound.clip);
            }
            else if (Input.GetMouseButtonDown(0) && !gun.reloading)
            {
                Vector3 dest = Input.mousePosition;

                Ray ray = _mainCamera.ScreenPointToRay(dest);

                if (Physics.Raycast(ray, out RaycastHit hitData, 100, floor))
                {
                    moveDestination = hitData.point;
                    moveDestination.y = 0.5f;
                    Vector3 targetDirection = moveDestination - transform.position;
                    transform.forward = targetDirection;
                    gun.PlayerFireServerRpc(moveDestination, NetworkManager.Singleton.LocalClientId);
                    //localFiring = true;
                }
            }
        }

#endif
    }

    private void FixedUpdate()
    {
        while (networkTimer.ShouldTick())
        {
            HandleClientTick();
            HandleServerTick();
        }

    }

    void HandleServerTick()
    {
        if (!IsServer) return;
        var bufferIndex = -1;
        InputPayload inputPayload = default;
        while (serverInputQueue.Count > 0)
        {
            inputPayload = serverInputQueue.Dequeue();

            bufferIndex = inputPayload.tick % k_bufferSize;

            //ProcessMovement is normally called if there is no lag. Else also call Extrapolation

            StatePayload statePayload = PredictMovePlayer(inputPayload);
            statePayload.timestamp = DateTime.Now;
            serverStateBuffer.Add(statePayload, bufferIndex);
        }

        if (bufferIndex == -1) return;
        SendToClientRpc(serverStateBuffer.Get(bufferIndex));

       // HandleExtrapolation(serverStateBuffer.Get(bufferIndex), CalculateLatencyInMillis(inputPayload.timestamp));
    }


    private Vector3 previousPos = Vector3.zero;
    public void AntiCheat()
    {
        if(previousPos == Vector3.zero) 
            previousPos = transform.position;

        //CHEAT DETECTED
        if (Vector3.Distance(previousPos, transform.position) > 2)
        {
                SwitchAuthorityMode(AuthorityMode.Server);
                transform.position = previousPos;
                SwitchAuthorityMode(AuthorityMode.Client);
        }

        previousPos = transform.position;
    }


    static float CalculateLatencyInMillis(DateTime timeStamp)
    {
        ping = (DateTime.Now - timeStamp).Milliseconds;
        if (ping < -10f)
        {
            Debug.LogWarning(ping);
            Debug.LogWarning(timeStamp.Millisecond.ToString());
            Debug.LogWarning(DateTime.Now.Millisecond.ToString());

        }
        return ping;

    }

    private void DisplayPing()
    {
         if ((DateTime.Now - previousTimeStamp).Milliseconds >= 200)
         {
            float actualPing = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId);
             pingText.text = "Ping: " + ((int)actualPing).ToString();
             previousTimeStamp = DateTime.Now;
         };
    }


    void Extrapolate()
    {
        if (IsServer && extrapolationTimer.IsRunning)
        {

            if (extrapolationState.inputVector != Vector3.zero)
            {
                transform.position += extrapolationState.inputVector * Time.deltaTime * speed * ping / 10000 * extrapolationMultiplier;
            }

        }
    }


    StatePayload lastExtrapolationState;

    //Server only preprares extrapolationState to be executed in Extrapolate()
    void HandleExtrapolation(StatePayload latest, float latency)
    {

        if (ShouldExtrapolate(latency))
        {
            if (extrapolationState.position != default)
            {
                latest = extrapolationState;
            }

            extrapolationState.inputVector = latest.inputVector;

            //Allows Extrapolate method to start
            extrapolationTimer.Start();

        }
        else
        {
            extrapolationTimer.Stop();

            //Reconciliation if lag is strong
        }
    }


    //True if latency is less than the maximum latency and latency is more than the FPS/1
    bool ShouldExtrapolate(float latency) => latency > extrapolationMinimum && latency < extrapolationLimit && latency > Time.fixedDeltaTime;



    [ClientRpc]
    void SendToClientRpc(StatePayload statePayload)
    {
        if (!IsOwner) return;
        lastServerState = statePayload;
      //  serverCube.transform.position = statePayload.position;
        //DisplayPing(CalculateLatencyInMillis(lastServerState.timestamp));
    }

    private Vector3 GetInput()
    {
        if (!IsOwner || !Application.isFocused) return Vector3.zero;

#if UNITY_STANDALONE_WIN

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
          //  MovePlayerPc(receivedInput);
        }
        MoveCamera();

        //Aiming
     /*   if (Input.GetMouseButton(1))
        {
            Vector3 dest = Input.mousePosition;

            Ray ray = _mainCamera.ScreenPointToRay(dest);

            if (Physics.Raycast(ray, out RaycastHit hitData, 100, floor))
            {
                Vector3 moveDestination = hitData.point;
                moveDestination.y = 0.5f;
                gun.AimWeapon(moveDestination);


                //Aiming and shooting
                if (Input.GetMouseButtonDown(0) && firing.Value)
                    //noAmmoSound.Play();
                noAmmoSound.PlayOneShot(noAmmoSound.clip);
                else if (Input.GetMouseButtonDown(0)&& !gun.reloading)
                {
                    Vector3 targetDirection = moveDestination - transform.position;
                    gun.PlayerFireServerRpc(moveDestination, NetworkManager.Singleton.LocalClientId);
                    //localFiring = true;
                }

            }
            else
                gun.StopAim();
        }
        else
            gun.StopAim();

        //Just shooting
        if (Input.GetMouseButtonDown(0) && firing.Value)
            //noAmmoSound.Play();
            noAmmoSound.PlayOneShot(noAmmoSound.clip);

        else if (Input.GetMouseButtonDown(0) && !gun.reloading)
        {
            Vector3 dest = Input.mousePosition;

            Ray ray = _mainCamera.ScreenPointToRay(dest);

            if (Physics.Raycast(ray, out RaycastHit hitData, 100, floor))
            {
                Vector3 moveDestination = hitData.point;
                moveDestination.y = 0.5f;
                gun.PlayerFireServerRpc(moveDestination, NetworkManager.Singleton.LocalClientId);
              //  localFiring = true;
            }
        }

        */
        return receivedInput;



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
            //MovePlayerPhoneServerRpc(movPos);

        }

        MoveCamera();

        //Aim And Shoot
        Vector3 shootPos = new Vector3();

        shootPos.x += joystickShoot.Horizontal;
        shootPos.z += joystickShoot.Vertical;

        if (shootPos != Vector3.zero)
        {
            aiming = true;
            lastAimedPos = gun.AimWeaponMobile(shootPos);
            previousMov = shootPos.magnitude;
        }
        else
        {
            gun.StopAim();
            if (aiming == true)
            {
                if (previousMov >= 0.22)
                {
                    //  if (!localFiring)
                    //{
                    if (firing.Value)
                        noAmmoSound.Play();
                    //   Vector3 targetDirection = lastAimedPos - transform.position;
                    // transform.forward = targetDirection;
                    //  Debug.Log(targetDirection);
                    //Vector3 targetDirection = shootPos - transform.position;
                    //transform.forward = targetDirection;
                    moveDestination = lastAimedPos;
                    if (LobbyManager.Instance.IntToCharacter(playerChar.Value)!= LobbyManager.PlayerCharacter.Marine)
                    {
                        transform.forward = lastAimedPos - transform.position;
                    }
                    else
                    {
                        transform.forward = lastAimedPos;
                    }
                    gun.PlayerFireServerMobileServerRpc(lastAimedPos, NetworkManager.Singleton.LocalClientId);
                    aiming = false;
                }
                else
                {
                    //Handheld.Vibrate();
                }


            }
        }
        return movPos;

#endif
    }


    void HandleClientTick()
    {
        if (!IsClient || !IsOwner) return;


        var currentTick = networkTimer.CurrentTick;
        var bufferIndex = currentTick % k_bufferSize;


        
        Vector3 inputVector = GetInput();
        if(inputVector == Vector3.zero)
            return;
        InputPayload inputPayload = new InputPayload()
        {
            tick = currentTick,
            timestamp = DateTime.Now,
            inputVector = inputVector,
        };

        clientInputBuffer.Add(inputPayload, bufferIndex);


        SendToServerRpc(inputPayload);


        StatePayload statePayload = ProcessMovement(inputPayload);
        clientStateBuffer.Add(statePayload, bufferIndex);

        HandleServerReconciliation();
        DisplayPing();
    }

    bool ShouldReconcile()
    {
        bool isNewServerState = !lastServerState.Equals(default);
        bool isLastStateUndefinedOrDifferent = lastProcessedState.Equals(default)
                                               || !lastProcessedState.Equals(lastServerState);


        return isNewServerState && isLastStateUndefinedOrDifferent && !reconciliationTimer.IsRunning && !extrapolationTimer.IsRunning;
    }

    void HandleServerReconciliation()
    {

        if (!ShouldReconcile()) return;

        float positionError;
        int bufferIndex;

        bufferIndex = lastServerState.tick % k_bufferSize;
        if (bufferIndex - 1 < 0) return; // Not enough information to reconcile

        StatePayload rewindState = IsHost ? serverStateBuffer.Get(bufferIndex - 1) : lastServerState; // Host RPCs execute immediately, so we can use the last server state
        StatePayload clientState = IsHost ? clientStateBuffer.Get(bufferIndex - 1) : clientStateBuffer.Get(bufferIndex);
        positionError = Vector3.Distance(rewindState.position, clientState.position);

        if (positionError > reconciliationThreshold)
        {
            ReconcileState(rewindState);
            reconciliationTimer.Start();
        }

        lastProcessedState = rewindState;
    }

    void ReconcileState(StatePayload rewindState)
    {
        transform.position = rewindState.position;
        transform.rotation = Quaternion.Euler(rewindState.inputVector.x, rewindState.inputVector.y, rewindState.inputVector.z);

        if (!rewindState.Equals(lastServerState)) return;

        clientStateBuffer.Add(rewindState, rewindState.tick % k_bufferSize);

        // Replay all inputs from the rewind state to the current state
        int tickToReplay = lastServerState.tick;

        while (tickToReplay < networkTimer.CurrentTick)
        {
            int bufferIndex = tickToReplay % k_bufferSize;
            StatePayload statePayload = ProcessMovement(clientInputBuffer.Get(bufferIndex));
            clientStateBuffer.Add(statePayload, bufferIndex);
            tickToReplay++;
        }
    }

    // En el cliente
    void SendPing()
    {
        var pingTimestamp = DateTime.UtcNow;
        PingToServerRpc(pingTimestamp, NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc]
    void PingToServerRpc(DateTime clientTimestamp, ulong clientId)
    {
        // Asegúrate de que este RPC solo se envíe al cliente que inició el ping
        SendPongClientRpc(clientTimestamp, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });
    }

    [ClientRpc]
    void SendPongClientRpc(DateTime timeStamp, ClientRpcParams rpcParams = default)
    {
        ping = (DateTime.UtcNow - timeStamp).Milliseconds;
        
       DisplayPing(ping);
       Debug.Log(NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId));
        Debug.Log(ping);
       SendPing();
    }

   static void DisplayPing(double actualPing)
    {
        pingText.text = "Ping: " + ((int)actualPing).ToString();
    }


        [ServerRpc]
    void SendToServerRpc(InputPayload input)
    {
        // clientCube.transform.position = input.position;
        serverInputQueue.Enqueue(input);
    }

    StatePayload ProcessMovement(InputPayload input)
    {
        MovePlayerPc(input.inputVector);

        return new StatePayload()
        {
            tick = input.tick,

            //      networkObjectId = NetworkObjectId,
            position = transform.position,
            inputVector = input.inputVector,

        };
    }

    private StatePayload PredictMovePlayer(InputPayload input)
    {
        Vector3 newPos = transform.position + input.inputVector.normalized * Time.deltaTime * speed;


        if (!firing.Value && input.inputVector != Vector3.zero)
        {
            Quaternion newRotation = Quaternion.LookRotation(input.inputVector);
        }
        return new StatePayload()
        {
            tick = input.tick,
            position = newPos,
            inputVector = input.inputVector,
        };
    }


    bool freezeRotation;
    private void MovePlayerPc(Vector3 input)
    {
        if (input == Vector3.zero)
            return;
        transform.position += input.normalized * Time.deltaTime * speed;

        if (firing.Value && !freezeRotation)
        {
           // Quaternion newRotation = Quaternion.LookRotation(moveDestination);
            //transform.rotation = newRotation;
            transform.forward = moveDestination - transform.position;
            freezeRotation = true;
        }
        else if(!firing.Value)
            freezeRotation = false;

        if (!firing.Value && input != Vector3.zero)
        {
            Quaternion newRotation = Quaternion.LookRotation(input);
            transform.rotation = newRotation;
            //freezeRotation = false;

        }
    }

    [ServerRpc]
    private void MovePlayerPhoneServerRpc(Vector3 input)
    {
        //TO DO CHECKs
        transform.position = Vector3.MoveTowards(transform.position, transform.position + input, Time.deltaTime * speed);

        if (!firing.Value && input != Vector3.zero)
        {
            Quaternion newRotation = Quaternion.LookRotation(input);
            transform.rotation = newRotation;
        }
    }

    public override void OnNetworkSpawn()
    {
        Player player = LobbyManager.Instance.GetPlayerOrCreate();
        Initialized();

        life = new NetworkVariable<int>(MaxLife, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


        nameText.text = PlayerName.Value.ToString();


        if (!IsServer)
        {
            PlayerInfo player1 = OnlineManager.Instance.playerList.Find(x => x.lobbyPlayerId == PlayerLobbyId.Value);
            player1.playerObject = gameObject;
            player1.clientId = NetworkManager.Singleton.LocalClientId;
        }
        //        moveDestination = transform.position;

        if (IsOwner)
        {
            joystickShoot = Assets.Instance.joystickShoot;
            joystick = Assets.Instance.joystick;
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
        Initialized();
        _mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y + 15, transform.position.z - 5);

    }


    private bool stopHealth = false;

    [ServerRpc(RequireOwnership = false)]
    public void DamageTakenServerRpc(int dmg, int shooterIndex, int playerHittedIndex)
    {
        life.Value -= dmg;
        if (life.Value <= 0)
        {
            Debug.Log("DAMAGE TO 0" + life.Value);
            Debug.Log("Client ID" + OnlineManager.Instance.playerList[playerHittedIndex].clientId);
            //Manage player
            OnlineManager.Instance.PlayerDeath(OnlineManager.Instance.playerList[playerHittedIndex].clientId);

            OnlineManager.Instance.ChangeScoreServerRpc(shooterIndex, playerHittedIndex);
            life.Value = MaxLife;

        }
        else
        {
            //  StopAllCoroutines();            //Care with this, it stops all the Couroutines of this script!!
            stopHealth = true;
            // StopCoroutine(HealthByTime());
            StartCoroutine(WaitToHealth());
        }
        healthUI.TakeDamageClientRpc(life.Value);
    }



    private IEnumerator WaitToHealth()
    {
        yield return new WaitForSeconds(healthDamageWait);
        stopHealth = false;
        StartCoroutine(HealthByTime());
    }

    private IEnumerator HealthByTime()
    {
        if (stopHealth)
        {
            //  stopHealth = false;
            yield break;
        }

        yield return new WaitForSeconds(healthInterval);

        if (stopHealth)
        {
            //  stopHealth = false;
            yield break;
        }
        life.Value += healthBySecond.Value;

        if (life.Value >= MaxLife)
            life.Value = MaxLife;
        else if (life.Value < MaxLife && !stopHealth)
            StartCoroutine(HealthByTime());

        healthUI.TakeDamageClientRpc(life.Value);
        //Add field so if it is positive (health) it shows an animation and if negative it shows other animation

    }

}