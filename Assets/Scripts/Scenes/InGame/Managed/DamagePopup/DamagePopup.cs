using UnityEngine;
using TMPro;
using System;

public class DamagePopup : MonoBehaviour
{
    Action<DamagePopup> onReturn;

    float life = 1f;
    float t;

    Vector3 velocity = new Vector3(0, 1.5f, 0);
    
    NumberSpriteView　numberView;

    public void Awake()
    {
        numberView = GetComponent<NumberSpriteView>();
    }

    public void Setup(int value)
    {
        numberView.SetNumber(value);
    }

    void Update()
    {
        transform.position += velocity * Time.deltaTime;

        t += Time.deltaTime;
        if (t > life)
        {
            onReturn(this);
        }
    }
}