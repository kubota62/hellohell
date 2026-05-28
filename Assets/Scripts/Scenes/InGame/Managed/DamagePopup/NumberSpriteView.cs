using UnityEngine;

public class NumberSpriteView : MonoBehaviour
{
    [Header("0〜9のスプライト")]
    public Sprite[] digitSprites;

    [Header("桁の間隔")]
    public float spacing = 0.6f;

    SpriteRenderer[] renderers;

    int lastLength;

    void Awake()
    {
        renderers = new SpriteRenderer[10]; // 最大10桁想定（必要なら増やす）
    }

    public void SetNumber(int value)
    {
        string text = value.ToString();

        EnsureCapacity(text.Length);

        for (int i = 0; i < text.Length; i++)
        {
            int digit = text[i] - '0';

            var sr = renderers[i];

            sr.sprite = digitSprites[digit];
            sr.gameObject.SetActive(true);

            sr.transform.localPosition = new Vector3(i * spacing, 0, 0);
        }

        // 余った桁は非表示
        for (int i = text.Length; i < lastLength; i++)
        {
            renderers[i].gameObject.SetActive(false);
        }

        lastLength = text.Length;
    }

    void EnsureCapacity(int length)
    {
        for (int i = 0; i < length; i++)
        {
            if (renderers[i] != null) continue;

            var go = new GameObject("digit_" + i);
            go.transform.SetParent(transform);
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();

            renderers[i] = sr;
        }
    }
}