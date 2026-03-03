using UnityEngine;
using System.Collections;
using TMPro;

public class EarthquakeManager : MonoBehaviour
{
    public Rigidbody2D groundRb;
    public Transform ballTransform;
    public TextMeshProUGUI uiText;

    private bool isQuaking = false;

    // 【修改点1】降低像素高度判定，适应 500 像素的操作空间
    private float initialRequiredHeight = 250f; // 初始需要的高度
    private float winHeight = 200f;             // 胜利需要保持的高度
    private float maxHorizontalOffset = 150f;   // 小球允许的最大水平偏移

    // 震动幅度配置 (像素)
    public float shakeAmplitudeX = 15f; // 左右平移最大 15 像素
    public float shakeAmplitudeY = 5f;  // 上下平移最大 5 像素

    public void StartTest()
    {
        float currentHeight = ballTransform.position.y - groundRb.transform.position.y;
        if (currentHeight < initialRequiredHeight)
        {
            uiText.text = $"高度不足！当前:{currentHeight:F0}，需要:{initialRequiredHeight}";
            return;
        }

        uiText.text = "地震发生中(横波+纵波)...";

        // 创建防弹跳、高摩擦的物理材质
        PhysicsMaterial2D frictionMat = new PhysicsMaterial2D();
        frictionMat.friction = 0.9f;   // 极高摩擦力
        frictionMat.bounciness = 0.0f; // 完全不反弹

        BlockInteract[] allBlocks = FindObjectsOfType<BlockInteract>();
        foreach (var block in allBlocks)
        {
            block.isPhysicsActive = true;
            Rigidbody2D rb = block.GetComponent<Rigidbody2D>();
            Collider2D col = block.GetComponent<Collider2D>();

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 30f; // 稍微调低一点重力，30 足够稳了
                rb.mass = 5f;

                // 【修改点2】增加阻尼，防止方块像冰块一样无限滑动或旋转
                rb.linearDamping = 1f;         // 线性阻力（抑制平移）
                rb.angularDamping = 3f;  // 角阻力（抑制乱转）
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 防止高速穿模
            }
            if (col != null)
            {
                col.sharedMaterial = frictionMat;
            }
        }

        // 小球也要设置同样的属性
        Rigidbody2D ballRb = ballTransform.GetComponent<Rigidbody2D>();
        if (ballRb != null)
        {
            ballRb.bodyType = RigidbodyType2D.Dynamic;
            ballRb.gravityScale = 30f;
            ballRb.linearDamping = 1f;
            ballRb.angularDamping = 2f;
            ballRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            ballTransform.GetComponent<Collider2D>().sharedMaterial = frictionMat;
        }

        StartCoroutine(EarthquakeRoutine());
    }

    private IEnumerator EarthquakeRoutine()
    {
        isQuaking = true;
        float timer = 0f;
        float duration = 5f;

        // 记录地面的初始位置，所有的震动都围绕这个中心点进行
        Vector2 startPos = groundRb.position;
        float randomOffsetX = Random.Range(0f, 100f);
        float randomOffsetY = Random.Range(0f, 100f);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // 【修改点3】使用平移震动替代旋转震动
            // 柏林噪声产生 -1 到 1 的平滑随机值，乘以 25f 是为了让震动频率变快
            float noiseX = (Mathf.PerlinNoise(timer * 25f, randomOffsetX) - 0.5f) * 2f;
            float noiseY = (Mathf.PerlinNoise(timer * 25f, randomOffsetY) - 0.5f) * 2f;

            // 计算新的目标位置
            Vector2 targetPos = startPos + new Vector2(noiseX * shakeAmplitudeX, noiseY * shakeAmplitudeY);

            // 使用 MovePosition 进行物理安全的平移
            groundRb.MovePosition(targetPos);

            yield return new WaitForFixedUpdate(); // 物理相关的移动最好等待物理帧
        }

        // 震动结束，精准归位
        groundRb.MovePosition(startPos);
        isQuaking = false;
        uiText.text = "等待物理静止...";

        yield return new WaitForSeconds(3f);
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        float finalHeight = ballTransform.position.y - groundRb.transform.position.y;
        float offsetX = Mathf.Abs(ballTransform.position.x - groundRb.transform.position.x);

        Rigidbody2D ballRb = ballTransform.GetComponent<Rigidbody2D>();
        // 速度小于 15f 认为已经停稳（因为放大了像素尺度，速度阈值也要相应放大）
        bool isStable = ballRb != null && ballRb.linearVelocity.magnitude < 15f;

        if (finalHeight >= winHeight && offsetX <= maxHorizontalOffset && isStable)
        {
            uiText.text = $"胜利！建筑扛住了！最终高度: {finalHeight:F0}";
            uiText.color = Color.green;
        }
        else
        {
            if (finalHeight < winHeight)
                uiText.text = $"失败！高度过低: {finalHeight:F0} (需 {winHeight})";
            else if (offsetX > maxHorizontalOffset)
                uiText.text = $"失败！小球偏离建筑中心！偏移: {offsetX:F0}";
            else
                uiText.text = "失败！结构依然在崩塌中！";

            uiText.color = Color.red;
        }
    }
}