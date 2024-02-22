using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{

    public enum Scene
    {
        GameScene,
        LobbyScene,
        CharacterSelectScene,
    }


    private static Scene targetScene;



    public static void Load(Scene targetScene)
    {
        SceneLoader.targetScene = targetScene;
        SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }

    public static IEnumerator LoadAsync(Scene targetScene, string msg = null,  bool authenticate = false)
    {
        SceneLoader.targetScene = targetScene;
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(targetScene.ToString(), LoadSceneMode.Single);

        while (!asyncOperation.isDone)
        {
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            Debug.Log("Cargando escena... " + (progress * 100f).ToString("F2") + "%");
            yield return null;

        }
        asyncOperation.allowSceneActivation = true;

        if (targetScene != Scene.LobbyScene)
            yield break;

        if(!authenticate)
        {
            AuthenticateUI.Instance.Hide();
        }
        //Debug.Log("***123");
        if(!LobbyUI.Instance.gameObject.active) 
        {
            Debug.Log("activating lobby list");
            LobbyListUI.Instance.gameObject.SetActive(true);
        }

        if(msg != null)
            PopUp.Instance.ShowPopUp(msg, false, PopUp.PopUpType.Error);


    }

    public static void LoadNetwork(Scene targetScene)
    {
        Debug.Log("LOAD NETWORK!" + targetScene + " " + LoadSceneMode.Single);
        // NetworkSceneManager.Sing ActiveSceneSynchronizationEnabled;
        NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;
        var status =  NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
       // var status = NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Additive);

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Failed to load {targetScene} " +
                  $"with a {nameof(SceneEventProgressStatus)}: {status}");
        }
    }

    public static void LoaderCallback()
    {
        SceneManager.LoadScene(targetScene.ToString());
    }

}