using System;
using System.Text;

/// <summary>
/// 简单加密工具（Base64 + 字符偏移，展示安全性概念）
/// </summary>
public static class EncryptHelper
{
    private const int SHIFT = 3;

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";
        byte[] bytes = Encoding.UTF8.GetBytes(plainText);
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)(bytes[i] + SHIFT);
        return Convert.ToBase64String(bytes);
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return "";
        try
        {
            byte[] bytes = Convert.FromBase64String(cipherText);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(bytes[i] - SHIFT);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "";
        }
    }
}
