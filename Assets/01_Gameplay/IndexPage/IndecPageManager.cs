using UnityEngine;
using UnityEngine.SceneManagement;

public class IndexPageManager : MonoBehaviour
{
    public void onClick_about()
    {
        SceneManager.LoadScene("About");
    }
}