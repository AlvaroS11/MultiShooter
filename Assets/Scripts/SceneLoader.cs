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