using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TeamUI : MonoBehaviour
{
    // Start is called before the first frame update

    public Image imageBackground;
    public Image imageBar;
    public TextMeshProUGUI text;

    public int team;
    public int maxKills;

    public int kills;

    RectTransform rt;
    RectTransform rtB;

    public float fullWidth;


    public List<Color> imageBars;
    public List<Color> imageBackgrounds;
    public void SetUp(int actualTeam, int maxKills)
    {
        actualTeam--;
        try
        {
            //  imageBackground.color = imageBackgrounds[actualTeam--];
            imageBar.color = imageBars[actualTeam];
        }
        catch
        {
            //       imageBackground.color = imageBackgrounds[0];
            imageBar.color = imageBars[0];
        }
        // imageBackground = imageBckg;
        //imageBar = imageItem;
        this.maxKills = maxKills;
        rt = imageBar.GetComponent(typeof(RectTransform)) as RectTransform;
        rtB = imageBackground.GetComponent(typeof(RectTransform)) as RectTransform;
        fullWidth = rtB.rect.width;

        float widthSize = 0.1f * fullWidth;
        rt.sizeDelta = new Vector2(widthSize, rt.sizeDelta.y);
    }

    public void Kill()
    {
        kills++;
        text.text = kills.ToString();

        float widthSize = ((float)kills / maxKills) * fullWidth;
        rt.sizeDelta = new Vector2(widthSize, rt.sizeDelta.y);
        //rt.
    }
}
