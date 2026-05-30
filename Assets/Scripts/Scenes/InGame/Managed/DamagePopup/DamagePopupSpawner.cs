using System.Collections.Generic;
using UnityEngine;

public class DamagePopupSpawner : MonoBehaviour
{
    public DamagePopup prefab;
    public int poolSize = 100;

    Queue<DamagePopup> pool = new();

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var obj = Instantiate(prefab, transform);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public void Spawn(int value, Vector3 pos)
    {
        var popup = pool.Dequeue();
        // Debug.Log($"Spawn: {pool.Count}");

        popup.gameObject.SetActive(true);
        popup.Setup(value, pos, Return);
    }

    void Return(DamagePopup popup)
    {
        popup.gameObject.SetActive(false);
        pool.Enqueue(popup);
        // Debug.Log($"Return:  {pool.Count}");
    }
}