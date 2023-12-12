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
using System.Net.NetworkInformation;

public class PlayerManager : NetworkBehaviour
{
    private float speed = 3f;
    private Camera _mainCamera;
    public LayerMask floor;

    //  public GameObject Bullet;

    public Weapon gun;

    public int MaxLife = 100;

    //[HideInInspector]
    public NetworkVariable<int> life = new NetworkVariable<int>();

    public UIPlayer healthUI;

    private Transform playerPrefab;

    // public NetworkVariable<int> team = new NetworkVariable<int>();
    // public int team;


    /* public string PlayerLobbyId;
     public string PlayerName;
     public int PlayerTeam;
    */
    // public NetworkVariable<FixedString128Bytes> PlayerLobbyId = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public NetworkVariable<int> PlayerTeam = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [HideInInspector]
    public LobbyManager.PlayerCharacter playerCharacterr;

    public int PlayerInfoIndex;

    //  [SerializeField]
    [HideInInspector]
    private Joystick joystick;

    // [SerializeField]
    [HideInInspector]
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


    // [SerializeField]
    private bool aiming;

    //[SerializeField]
    private Vector3 lastAimedPos;

    public GameObject body;

    public Animator bodyAnimator;

    private float previousMov;


    static float ping;

    // Network variables should be value objects
    public struct InputPayload : INetworkSerializable
    {
        public int tick;
        public DateTime timestamp;
        public ulong networkObjectId;
        public Vector3 inputVector;
        public Vector3 position;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref timestamp);
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref inputVector);
            serializer.SerializeValue(ref position);
        }
    }

    //The state of the player
    public struct StatePayload : INetworkSerializable
    {
        public int tick;
        public ulong networkObjectId;
        public Vector3 position;
        //public Quaternion rotation;
        public Vector3 inputVector;
        public DateTime timestamp;

        //public Vector3 velocity;
        //public Vector3 angularVelocity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref inputVector);
            serializer.SerializeValue(ref timestamp);
            //serializer.SerializeValue(ref velocity);
            //serializer.SerializeValue(ref angularVelocity);
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
    [SerializeField] static TextMeshProUGUI pingText;





    public static DateTime previousTimeStamp = DateTime.Now;


    private void Awake()
    {
        networkTimer = new NetworkTimer(k_serverTickRate);
        clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
        clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);
        serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);


        serverInputQueue = new Queue<InputPayload>();

        reconciliationTimer = new CountdownTimer(reconciliationCooldownTime);
        extrapolationTimer = new CountdownTimer(extrapolationLimit);

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
        pingText = LobbyAssets.Instance.pingText;

    }

    private void FixedUpdate()
    {
        while (networkTimer.ShouldTick())
        {
            HandleClientTick();
            HandleServerTick();
        }

        //Extraplolate(); ?

    }

    void HandleServerTick()
    {
        var bufferIndex = -1;
        InputPayload inputPayload = default;
        while (serverInputQueue.Count > 0)
        {
            inputPayload = serverInputQueue.Dequeue();

            bufferIndex = inputPayload.tick % k_bufferSize;

            //ProcessMovement is normally called if there is no lag. Else also call Extrapolation

            /*  if (inputPayload.inputVector != Vector3.zero && !IsOwner)
                  Debug.Log("not zero");
              else if(!IsOwner)
                  Debug.Log("servertick is zero");*/
            StatePayload statePayload = ProcessMovement(inputPayload);
            statePayload.timestamp = DateTime.Now;
            serverStateBuffer.Add(statePayload, bufferIndex);
        }

        if (bufferIndex == -1) return;
        SendToClientRpc(serverStateBuffer.Get(bufferIndex));
        //if(!IsOwner)
        Debug.Log("...");
        Debug.Log(serverStateBuffer.Get(bufferIndex).position); // Está sacando lo mismo que en el cliente???
        HandleExtrapolation(serverStateBuffer.Get(bufferIndex), CalculateLatencyInMillis(inputPayload.timestamp));
    }


    static float CalculateLatencyInMillis(DateTime timeStamp) {
        ping = (DateTime.Now - timeStamp).Milliseconds;
        return ping;

        }

    static void DisplayPing(float actualPing)
    {
        if((DateTime.Now - previousTimeStamp).Milliseconds >= 200)
        {
            pingText.text = "Ping: " + ((int)actualPing).ToString();
            previousTimeStamp = DateTime.Now;
        }
        ;
    }


    void Extrapolate()
    {
        //Debug.Log(extrapolationTimer.is)
        if (IsServer && extrapolationTimer.IsRunning)
        {
            if (!IsOwner && extrapolationState.inputVector != Vector3.zero)
                Debug.Log("not zero");

           // Debug.Log(extrapolationState.position);
           // transform.position += extrapolationState.position;

            //a mas latencia más habrá que preveer
            if (extrapolationState.inputVector != Vector3.zero)
            {
                Debug.Log("extrapolating");
                transform.position += extrapolationState.inputVector * Time.deltaTime * speed * ping / 1000 * extrapolationMultiplier;

            }
            //MovePlayerPc(extrapolationState.inputVector);


            //MovePlayerPc(extrapolationState.inputVector);

        }
    }


    //Server only preprares extrapolationState to be executed in Extrapolate()
    void HandleExtrapolation(StatePayload latest, float latency)
    {
        /* Debug.Log("***");
         Debug.Log(latency);
         Debug.Log(extrapolationLimit);
         Debug.Log(Time.fixedDeltaTime);
         Debug.Log(latency < extrapolationLimit && latency > Time.fixedDeltaTime);  
        */

        if (ShouldExtrapolate(latency))
        {
            Debug.Log("shouldExtrapolate");
            // Calculate the arc the object would traverse in degrees
            /*   float axisLength = latency * latest.angularVelocity.magnitude * Mathf.Rad2Deg;
               Quaternion angularRotation = Quaternion.AngleAxis(axisLength, latest.angularVelocity);

               if (extrapolationState.position != default)
               {
                   latest = extrapolationState;
               }

               // Update position and rotation based on extrapolation
               var posAdjustment = latest.velocity * (1 + latency * extrapolationMultiplier);
               extrapolationState.position = posAdjustment;
               extrapolationState.rotation = angularRotation * transform.rotation;
               extrapolationState.velocity = latest.velocity;
               extrapolationState.angularVelocity = latest.angularVelocity;
              */

            // Debug.Log("EXTRAPOLATING!!");

            if (extrapolationState.position != default)
            {
                latest = extrapolationState;
            }

            // Update position based on extrapolation
            //var posAdjustment = latest.position + (Vector3.up * latency * extrapolationMultiplier); // Adjust as needed

            //var posAdjustment = latest.position + (speed * latest.inputVector * latency * extrapolationMultiplier);


            // NO ESTA HACIENDO NADA, EN EXTRAPOLATE LO HACEMOS CON EL IMPUT
            /* var posAdjustment = transform.position;
             if (latest.inputVector != Vector3.zero)
                 posAdjustment = speed * (latest.inputVector.normalized * latency * extrapolationMultiplier);
            */


            //Debug.Log("posAdjustment : " + posAdjustment);
            //var posAdjustment = speed * latest.inputVector;
            // extrapolationState.position = posAdjustment;

          //  var posAdjustment = speed * (latest.inputVector.normalized * latency * extrapolationMultiplier);
            //extrapolationState.position = posAdjustment;

            extrapolationState.inputVector = latest.inputVector;



            //Allows Extrapolate method to continue
            extrapolationTimer.Start();

        }
        else
        {
            extrapolationTimer.Stop();

            //Reconciliation if lag is so strong
        }
    }


    //True if latency is less than the maximum latency and latency is more than the FPS/1
    bool ShouldExtrapolate(float latency) => latency > extrapolationMinimum && latency < extrapolationLimit && latency > Time.fixedDeltaTime;



    [ClientRpc]
    void SendToClientRpc(StatePayload statePayload)
    {
        //    Debug.Log($"Received state from server Tick {statePayload.tick} Server POS: {statePayload.position}");
        //serverCube.transform.position = statePayload.position.With(y: 4);
       
        if (!IsOwner) return;
        lastServerState = statePayload;
        serverCube.transform.position = statePayload.position;
        Debug.Log(statePayload.position);
        DisplayPing(CalculateLatencyInMillis(lastServerState.timestamp));

       
    }

    private Vector3 GetInput()
    {
        if (!IsOwner || !Application.isFocused) return Vector3.zero;

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
            MovePlayerPc(receivedInput);
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
                //return moveDestination;
                gun.PlayerFireServerRpc(moveDestination, NetworkManager.Singleton.LocalClientId);
            }
        }
        // return Vector3.zero;

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
        return movPos;

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


                    gun.PlayerFireServerMobileServerRpc(lastAimedPos, NetworkManager.Singleton.LocalClientId);
                    aiming = false;
                    bodyAnimator.SetBool("firing", true);
                }


            }
        }
#endif
    }



    void HandleClientTick()
    {
        if (!IsClient || !IsOwner) return;

        var currentTick = networkTimer.CurrentTick;
        var bufferIndex = currentTick % k_bufferSize;

        InputPayload inputPayload = new InputPayload()
        {
            tick = currentTick,
            timestamp = DateTime.Now,
            networkObjectId = NetworkObjectId,
            //inputVector = input.Move,
            inputVector = GetInput(),
            position = transform.position
        };

        clientInputBuffer.Add(inputPayload, bufferIndex);


        SendToServerRpc(inputPayload);

        StatePayload statePayload = ProcessMovement(inputPayload);
        clientStateBuffer.Add(statePayload, bufferIndex);

        HandleServerReconciliation();
    }

    bool ShouldReconcile()
    {
        bool isNewServerState = !lastServerState.Equals(default);
        bool isLastStateUndefinedOrDifferent = lastProcessedState.Equals(default)
                                               || !lastProcessedState.Equals(lastServerState);

        // Debug.Log("RECONCILIATION" + (isNewServerState && isLastStateUndefinedOrDifferent && !reconciliationTimer.IsRunning && !extrapolationTimer.IsRunning));
        //Debug.Log(isNewServerState && isLastStateUndefinedOrDifferent && !reconciliationTimer.IsRunning && !extrapolationTimer.IsRunning);
        return isNewServerState && isLastStateUndefinedOrDifferent && !reconciliationTimer.IsRunning && !extrapolationTimer.IsRunning;
        //return false;
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

        //Debug.Log("position error " + positionError);

        /* if(rewindState.position != clientState.position)
             Debug.Log("position differs! " + positionError);

        // if(IsHost )
          //   Debug.Log(" rewind and client positions "  + rewindState.position + " " + clientState.position);

         if (Input.GetKeyDown(KeyCode.Q))
         {
             Debug.Log("distance: " + rewindState.position + "  " + clientState.position);
         }*/
        Debug.Log(IsHost);
        Debug.Log(lastServerState.position);
        Debug.Log(serverStateBuffer.Get(bufferIndex - 1).position);
        Debug.Log("distance: " + rewindState.position + "  " + clientState.position);
        Debug.Log(positionError);
        if (positionError > reconciliationThreshold)
        {
            ReconcileState(rewindState);
            reconciliationTimer.Start();
        }

        lastProcessedState = rewindState;
    }

    void ReconcileState(StatePayload rewindState)
    {
        Debug.Log("Reconciling!! ");
        transform.position = rewindState.position;
        // transform.rotation = rewindState.rotation;
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



    [ServerRpc]
    void SendToServerRpc(InputPayload input)
    {
        //    Debug.Log($"Received input from client Tick: {input.tick} Client POS: {input.position}");
        //clientCube.transform.position = input.position.With(y: 4);
        clientCube.transform.position = input.position;
        serverInputQueue.Enqueue(input);
    }

    StatePayload ProcessMovement(InputPayload input)
    {
        MovePlayerPc(input.inputVector);

        return new StatePayload()
        {
            tick = input.tick,

            networkObjectId = NetworkObjectId,
           // position = input.position,
            position = transform.position,
            //rotation = transform.rotation,
            inputVector = input.inputVector,

        };

        /* return new StatePayload()
         {
             tick = input.tick,

             networkObjectId = NetworkObjectId,
             position = input.position,
             //position = transform.position,
             //rotation = transform.rotation,
             inputVector = input.inputVector,

         };
        */
    }



    // Update is called once per frame
    void Update()
    {

        networkTimer.Update(Time.deltaTime);
        reconciliationTimer.Tick(Time.deltaTime);
        extrapolationTimer.Tick(Time.deltaTime);
        Extrapolate();

        //Debug.Log($"Owner: {IsOwner} NetworkObjectId: {NetworkObjectId} Velocity: {transform.position:F1}");
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if(IsOwner)
            transform.position += transform.forward * 20f;
        }

    }

    void Move(Vector3 inputVector)
    {
        /*float verticalInput = AdjustInput(input.Move.y);
        float horizontalInput = AdjustInput(input.Move.x);

        float motor = maxMotorTorque * verticalInput;
        float steering = maxSteeringAngle * horizontalInput;

        UpdateAxles(motor, steering);
        UpdateBanking(horizontalInput);

        kartVelocity = transform.InverseTransformDirection(rb.velocity);

        if (IsGrounded)
        {
            HandleGroundedMovement(verticalInput, horizontalInput);
        }
        else
        {
            HandleAirborneMovement(verticalInput, horizontalInput);
        }*/

#if UNITY_ANDROID
        MovePlayerPhoneServerRpc(inputVector);

#elif UNITY_EDITOR_WIN
        MovePlayerPc(inputVector);

#endif

    }



    /*
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
        if (aiming == true)
        {
            //Debug.Log(previousMov);
            if (previousMov >= 0.22)
            {
                //Debug.Log(shootPos);
                //bodyAnimator.SetBool("firing", false);

                gun.PlayerFireServerMobileServerRpc(lastAimedPos, NetworkManager.Singleton.LocalClientId);
                aiming = false;

                //animator.SetBool("firing", true);

                bodyAnimator.SetBool("firing", true);
            }


        }
    }


    /*  if(joystickShoot.Horizontal<= .2f && joystickShoot.Horizontal >= .2f && joystickShoot.Vertical <= .2f && joystickShoot.Vertical >= .2f)
      {
          //Vibrate, cancel and set to 0
      }
    */

    //   if (joystickShoot.)

    //#endif


    //  gun.StopAim();

    // }*/

    /*[ServerRpc]
    private void MovePlayerPcServerRpc(Vector3 input)
    {
        transform.position += input * Time.deltaTime * speed;
        // Vector3 targetDirection = input - transform.position;


        if (!firing && input != Vector3.zero)
        {
            Quaternion newRotation = Quaternion.LookRotation(input);
            transform.rotation = newRotation;
        }
    }*/


    // [ServerRpc]
    private void MovePlayerPc(Vector3 input)
    {
        if (input == Vector3.zero)
            return;
        transform.position += input.normalized * Time.deltaTime * speed;


        if (!firing && input != Vector3.zero)
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

        if (!firing && input != Vector3.zero)
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
