using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    public void LoadMainGame()
    {
        SceneManager.LoadScene("MainGame");
    }
}