using UnityEngine;
using System;

public static class JsonHelper
{
    public static T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);
    public static string ToJson<T>(T obj) => JsonUtility.ToJson(obj);
}
