using UnityEngine;
using System;
using DG.Tweening;

public class DamagePopup : MonoBehaviour
{
    Action<DamagePopup> onReturn;
    NumberSpriteView numberView;
    Sequence activeSequence;

    public void Awake()
    {
        numberView = GetComponent<NumberSpriteView>();
    }

    public void Setup(int value, Vector3 startPos, Action<DamagePopup> action)
    {
        activeSequence?.Kill();
    
        transform.position = startPos;
        transform.localScale = Vector3.one * 0.5f; // 最初は少し小さめから
        numberView.SetNumber(value);
        onReturn = action;

        // --- パラメータ調整（ここを変えると手触りが変わる） ---
        float outTime = 0.3f;        // 飛び出す速さ（短いほど鋭い）
        float jumpHeight = 2.0f;     // 跳ねる高さ
        float spreadX = 0.6f;        // 左右の散らばり幅
        float randomX = UnityEngine.Random.Range(-spreadX, spreadX);

        activeSequence = DOTween.Sequence();

        activeSequence
            // 1. 噴水のように飛び出す (OutQuad = 勢いよく出て、頂点で止まる)
            .Append(transform.DOMoveY(startPos.y + jumpHeight, outTime).SetEase(Ease.OutQuad))
            .Join(transform.DOMoveX(startPos.x + randomX, outTime).SetEase(Ease.Linear)) // 横は等速で散らす
            .Join(transform.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuart)) // 飛び出す瞬間に強調

            // 2. 頂点で標準サイズへ（ほんの一瞬）
            .Append(transform.DOScale(1.0f, 0.05f))
        
            // 3. 自由落下しながら消える (InQuad = 重力で加速しながら落ちる)
            .Append(transform.DOMoveY(-0.5f, 0.3f).SetRelative().SetEase(Ease.InQuad))
            .Join(transform.DOScale(0.5f, 0.3f)) // 落ちながら小さくなる
            .Join(GetComponent<CanvasGroup>() != null ? GetComponent<CanvasGroup>().DOFade(0, 0.3f) : transform.DOScale(0, 0.3f)) 

            .OnComplete(() => onReturn?.Invoke(this));
    }

    private void OnDestroy() => activeSequence?.Kill();
}