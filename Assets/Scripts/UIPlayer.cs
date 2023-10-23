using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayer : NetworkBehaviour
{

    public float healthAmount;

    public float maxLife;
    public Image healthBar;

    public PlayerManager playerController;
    void Start()
    {

        // if (!IsOwner) return;

        setPlayersData();
    }



 /*   [ServerRpc]
    public void InitializeClientsHUDServerRpc()
    {
        setPlayerDataClientRpc();
    }
 */
    //Sends the player data to the other players
   
    public void setPlayersData()
    {
        maxLife = playerController.MaxLife;
        healthAmount = maxLife;
        //CUANDO UN CLIENTE SE CONECTA, NO ESTÁ COGIENDO LOS MAX LIFES DE LOS CLIENTES YA CONECTADOS,
        //DADO QUE NO SE HA EJECUTADO ESTE MÉTODO DESDE DICHOS CLIENTES, SE EJECUTÓ ANTERIORMENTE
    }
  



    // Update is called once per frame
    void Update()
    {
        
    }

    [ClientRpc]
    public void TakeDamageClientRpc(float life)
    {
        //Add field so if it is positive (health) it shows an animation and if negative it shows other animation

        healthAmount = (float) life;
        healthBar.fillAmount = healthAmount / maxLife;
    }

  /*  [ClientRpc]
    public void HealClientRpc(float healingAmount)
    {
        healthAmount = playerController.life.Value;
        healthAmount = Mathf.Clamp(healthAmount, 0, maxLife);
        healthBar.fillAmount = healthAmount / maxLife;

    }
  */
}
