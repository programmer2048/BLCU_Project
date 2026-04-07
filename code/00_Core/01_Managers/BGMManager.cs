using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BGMManager:MonoBehaviour
{
    static BGMManager _instance;
    public AudioSource bgm;
    public static BGMManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<BGMManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this);
        }
        else if (this != _instance)
        {
            Destroy(this);
        }
    }
}
