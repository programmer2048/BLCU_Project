using UnityEngine;
using UnityEngine.SceneManagement;

public class AboutController:MonoBehaviour
{
    public void ExitToMap()
    {
        TransitionManager.Instance.SwitchScene("00_Boot");
        //SceneManager.LoadScene("00_Boot");
    }
}
