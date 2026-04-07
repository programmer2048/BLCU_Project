using UnityEngine;
using UnityEngine.SceneManagement;

public class AboutController:MonoBehaviour
{
    public void ExitToMap()
    {
        SceneManager.LoadScene("00_Boot");
    }
}
