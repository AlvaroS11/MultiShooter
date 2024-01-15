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
   
    public void setPlayersData()
    {
        try
        {
            maxLife = playerController.MaxLife;
            healthAmount = maxLife;
        }
        catch
        {
            maxLife = 100;
            healthAmount = maxLife;
        }
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
}
