using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    [SerializeField] private Image image;

    private void Start()
    {
        if (image != null)
        {
            Color c = image.color;
            c.a = 0f;
            image.color = c;
        }
    }


    public void FadeOut(float duration, Action onComplete = null)
    {
        StartCoroutine(FadeOutRoutine(duration, onComplete));
    }

    public void FadeIn(float duration, Action onComplete = null)
    {
        StartCoroutine(FadeInRoutine(duration, onComplete));
    }

    // CHANGED: Now returns IEnumerator so you can yield it
    public IEnumerator FadeOutIn(float fadeOutDur, float waitTime, float fadeInDur, Action duringBlack = null)
    {
        yield return StartCoroutine(FadeOutRoutine(fadeOutDur, null));
        duringBlack?.Invoke();
        if (waitTime > 0)
            yield return new WaitForSecondsRealtime(waitTime);
        yield return StartCoroutine(FadeInRoutine(fadeInDur, null));
    }

    public void FadeAndLoad(string sceneName, float duration)
    {
        StartCoroutine(FadeAndLoadRoutine(sceneName, duration));
    }

    // --- Private routines (unchanged) ---

    private IEnumerator FadeOutRoutine(float duration, Action onComplete)
    {
        float t = 0;
        Color c = image.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / duration);
            image.color = c;
            yield return null;
        }
        c.a = 1f;
        image.color = c;
        onComplete?.Invoke();
    }

    private IEnumerator FadeInRoutine(float duration, Action onComplete)
    {
        float t = 0;
        Color c = image.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = 1f - Mathf.Clamp01(t / duration);
            image.color = c;
            yield return null;
        }
        c.a = 0f;
        image.color = c;
        onComplete?.Invoke();
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName, float duration)
    {
        yield return StartCoroutine(FadeOutRoutine(duration, null));
        SceneManager.LoadScene(sceneName);
    }
}