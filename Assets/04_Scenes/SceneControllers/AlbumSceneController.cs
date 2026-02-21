using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 相簿场景控制器
/// </summary>
public class AlbumSceneController : MonoBehaviour
{
    [Header("UI")]
    public Transform albumContainer;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI countText;
    public Button backButton;

    private AlbumController _albumController;

    void Start()
    {
        _albumController = GetComponentInChildren<AlbumController>();
        if (_albumController == null)
        {
            _albumController = gameObject.AddComponent<AlbumController>();
        }
        _albumController.itemContainer = albumContainer;
        _albumController.titleText = titleText;
        _albumController.descText = countText;
        _albumController.backButton = backButton;

        _albumController.RefreshAlbum();
    }
}
