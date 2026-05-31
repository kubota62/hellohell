using System;
using UnityEngine;

public class NumberSpriteView : MonoBehaviour
{
    [Header("0〜9のスプライト")]
    public Sprite[] digitSprites;

    [Header("桁の間隔")]
    public float spacing = 1f;

    [SerializeField]
    SpriteRenderer[] renderers;

    int lastLength;

    public void SetNumber(int value)
    {
        int digit = 0;
        int temp = Math.Abs(value);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].gameObject.SetActive(false);
        }
        
        if (temp == 0) digit = 1;
        while (temp > 0)
        {
            int digitNum = temp % 10;
            var sr = renderers[digit];

            sr.sprite = digitSprites[digitNum];
            sr.gameObject.SetActive(true);

            //sr.transform.localPosition = new Vector3(i * spacing, 0, 0);
            
            temp /= 10;
            digit++;
        }
    }
}