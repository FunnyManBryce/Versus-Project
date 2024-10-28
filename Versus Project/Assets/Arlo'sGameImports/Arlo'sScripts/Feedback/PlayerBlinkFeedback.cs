using System.Collections;
using UnityEngine;

public class PlayerBlinkFeedback : MonoBehaviour
{
    [SerializeField] private Renderer characterRenderer;
    public float blinkDuration = 0.1f;
    [SerializeField] private Color blinkColor = new Color(1f, 1f, 1f, 0.25f);

    public int blinkCount = 5;
    private void Start()
    {
        if (characterRenderer == null)
        {
            characterRenderer = GetComponent<Renderer>();
        }
    }

    public void PlayBlinkFeedback()
    {
        StartCoroutine(Blink());
    }

    private IEnumerator Blink()
    {
        float blinkInterval = blinkDuration / blinkCount;

        for (int i = 0; i < blinkCount; i++)
        {
            characterRenderer.material.color = blinkColor;
            yield return new WaitForSeconds(blinkInterval);

            characterRenderer.material.color = Color.white;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}
