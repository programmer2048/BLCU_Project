using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using DG.Tweening;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("UI & Video References")]
    public CanvasGroup loadingCanvasGroup;
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;

    [Header("Settings")]
    public float fadeDuration = 0.3f;
    // 删除了 waitForVideoToEnd 变量，不再强制等待视频播完

    private bool isTransitioning = false;
    private RenderTexture videoRenderTexture;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.blocksRaycasts = false;

            if (videoPlayer != null && videoDisplay != null)
            {
                videoRenderTexture = new RenderTexture(1920, 1080, 0);
                videoPlayer.targetTexture = videoRenderTexture;
                videoDisplay.texture = videoRenderTexture;
                videoPlayer.playOnAwake = false;
                videoPlayer.waitForFirstFrame = true;

                // 既然加载完就切断，推荐把视频设为循环，防止视频短但加载慢导致黑屏
                videoPlayer.isLooping = true;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // minDisplayTime 默认是 1.0f，如果你想加载完 0 延迟立刻切，可以在调用时传入 0 
    // 例如：TransitionManager.Instance.SwitchScene("NextScene", 0f);
    public void SwitchScene(string sceneName, float minDisplayTime = 1.0f)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(null, sceneName, -1, minDisplayTime));
    }

    public void SwitchScene(int sceneBuildIndex, float minDisplayTime = 1.0f)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(null, "", sceneBuildIndex, minDisplayTime));
    }

    private IEnumerator TransitionRoutine(Action middleAction, string targetSceneName, int targetSceneIndex, float minDisplayTime)
    {
        isTransitioning = true;
        loadingCanvasGroup.blocksRaycasts = true;

        if (videoPlayer != null)
        {
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared) yield return null;
            videoPlayer.Play();
        }

        yield return loadingCanvasGroup.DOFade(1f, fadeDuration).WaitForCompletion();

        float startTime = Time.time;

        AsyncOperation asyncLoad = null;
        if (!string.IsNullOrEmpty(targetSceneName))
            asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        else if (targetSceneIndex >= 0)
            asyncLoad = SceneManager.LoadSceneAsync(targetSceneIndex);

        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = false;

            // 等待场景加载完毕 (progress == 0.9f 说明 Unity 已经把场景加载进内存了)
            while (asyncLoad.progress < 0.9f) yield return null;

            // 仅判断最小展示时间（防闪烁）。如果你传入的 minDisplayTime 是 0，这里会直接跳过
            float timeSpent = Time.time - startTime;
            if (timeSpent < minDisplayTime) yield return new WaitForSeconds(minDisplayTime - timeSpent);

            // 立刻激活新场景
            asyncLoad.allowSceneActivation = true;
            yield return new WaitUntil(() => asyncLoad.isDone);
        }
        else if (middleAction != null)
        {
            middleAction.Invoke();
            yield return new WaitForSeconds(minDisplayTime);
        }

        // 场景加载完，立刻淡出 UI
        yield return loadingCanvasGroup.DOFade(0f, fadeDuration).WaitForCompletion();

        // 立刻停止视频播放
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        loadingCanvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    private void OnDestroy()
    {
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
        }
    }
}