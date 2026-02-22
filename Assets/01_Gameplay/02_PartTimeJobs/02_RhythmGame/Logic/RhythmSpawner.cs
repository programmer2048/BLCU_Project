using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 音符生成器 —— 定时在轨道顶部生成下落音符
/// </summary>
public class RhythmSpawner : MonoBehaviour
{
    [Header("设置")]
    public RectTransform spawnArea;
    public float noteSpeed = 300f;
    public float spawnInterval = 0.8f;
    public int trackCount = 4;

    [Header("音符颜色 (编钟/琵琶/笛子/古筝)")]
    public Color[] noteColors = new Color[]
    {
        new Color(0.9f, 0.3f, 0.3f),
        new Color(0.3f, 0.9f, 0.3f),
        new Color(0.3f, 0.3f, 0.9f),
        new Color(0.9f, 0.9f, 0.3f)
    };

    private bool _spawning;
    private List<RhythmNote> _activeNotes = new List<RhythmNote>();
    public List<RhythmNote> ActiveNotes => _activeNotes;

    /// <summary>开始生成音符</summary>
    public void StartSpawning()
    {
        _spawning = true;
        _activeNotes.Clear();
        StartCoroutine(SpawnLoop());
    }

    /// <summary>停止生成</summary>
    public void StopSpawning()
    {
        _spawning = false;
    }

    private IEnumerator SpawnLoop()
    {
        while (_spawning)
        {
            SpawnNote();
            yield return new WaitForSeconds(spawnInterval + Random.Range(-0.15f, 0.15f));
        }
    }

    private void SpawnNote()
    {
        int track = Random.Range(0, trackCount);
        float trackWidth = spawnArea != null ? spawnArea.rect.width / trackCount : 200f;

        GameObject noteObj = new GameObject($"Note_{track}_{Time.time:F1}");
        RectTransform rt = noteObj.AddComponent<RectTransform>();

        if (spawnArea != null)
            rt.SetParent(spawnArea, false);
        else
            rt.SetParent(transform, false);

        // 位置生成在顶部
        float startX = (track - trackCount / 2f + 0.5f) * trackWidth;
        float startY = spawnArea != null ? spawnArea.rect.height / 2f : 400f;
        rt.anchoredPosition = new Vector2(startX, startY);
        rt.sizeDelta = new Vector2(trackWidth * 0.8f, 40f);

        Image img = noteObj.AddComponent<Image>();
        img.color = noteColors[track % noteColors.Length];

        var note = noteObj.AddComponent<RhythmNote>();
        note.track = track;
        note.speed = noteSpeed;
        _activeNotes.Add(note);
    }

    /// <summary>移除音符</summary>
    public void RemoveNote(RhythmNote note)
    {
        _activeNotes.Remove(note);
        if (note != null && note.gameObject != null)
            Destroy(note.gameObject);
    }

    /// <summary>清空所有音符</summary>
    public void ClearAll()
    {
        foreach (var note in _activeNotes)
        {
            if (note != null && note.gameObject != null)
                Destroy(note.gameObject);
        }
        _activeNotes.Clear();
    }
}

/// <summary>
/// 单个音符组件
/// </summary>
public class RhythmNote : MonoBehaviour
{
    public int track;
    public float speed;
    public bool isHit;

    private RectTransform _rt;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (_rt != null)
        {
            _rt.anchoredPosition += Vector2.down * speed * Time.deltaTime;
        }
    }

    public float GetY()
    {
        return _rt != null ? _rt.anchoredPosition.y : 0f;
    }
}
