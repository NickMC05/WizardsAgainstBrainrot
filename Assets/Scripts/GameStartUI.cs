using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameStartUI : MonoBehaviour
{
    [SerializeField] private Image uiImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Fades in the image over 3 seconds, then fades it back out
    /// </summary>
    public void FadeImageInAndOut()
    {
        StartCoroutine(FadeInAndOutRoutine());
    }

    private IEnumerator FadeInAndOutRoutine()
    {
        // Fade in over 3 seconds
        yield return StartCoroutine(FadeImage(0f, 1f, 0.5f));

        yield return StartCoroutine(FadeImage(1f, 1f, 1f));
        
        // Fade out over 3 seconds
        yield return StartCoroutine(FadeImage(1f, 0f, 0.5f));
    }

    private IEnumerator FadeImage(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = uiImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            uiImage.color = color;
            yield return null;
        }

        // Ensure final alpha is exact
        color.a = endAlpha;
        uiImage.color = color;
    }
}