using UnityEngine;
using UnityEngine.Pool;

public class DamagePool : MonoBehaviour
{
    public DamagePopup prefab;

    ObjectPool<DamagePopup> pool;

    void Awake()
    {
        pool = new ObjectPool<DamagePopup>(
            Create,
            OnGet,
            OnRelease,
            OnDestroyItem,
            collectionCheck: false,
            defaultCapacity: 50,
            maxSize: 200
        );
    }

    DamagePopup Create()
    {
        return Instantiate(prefab, transform);
    }

    void OnGet(DamagePopup obj)
    {
        obj.gameObject.SetActive(true);
    }

    void OnRelease(DamagePopup obj)
    {
        obj.gameObject.SetActive(false);
    }

    void OnDestroyItem(DamagePopup obj)
    {
        Destroy(obj.gameObject);
    }

    public DamagePopup Get() => pool.Get();
    public void Release(DamagePopup obj) => pool.Release(obj);
}