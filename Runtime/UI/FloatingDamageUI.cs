// using DG.Tweening;
using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Weapons;
using TMPro;
using UnityEngine;

public class FloatingDamageUI : MonoBehaviour,IPoolObject
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float floatingDistance;
    [SerializeField] private float floatingDuration;
    [SerializeField] private float randomOffsetValue;
    [SerializeField] private float scalePunch;

    public void Show(Damage damage,Vector2 position)
    {
        Vector2 offset = new(Random.Range(-randomOffsetValue, randomOffsetValue), 0);
        text.text = damage.Value.ToString();
        transform.position = position + offset;
        
        Animate();
    }

    private void Animate()
    {
        Color c = text.color;
        c.a = 1f;
        text.color = c;

        // Animate upward
        // transform.DOMoveY(transform.position.y + floatingDistance, floatingDuration).SetEase(Ease.OutCubic);
        // transform.DOPunchScale(Vector3.one * scalePunch, 0.3f, 5, 0.5f);
        // text.DOFade(0f, floatingDuration).SetEase(Ease.InCubic).OnComplete(() => ParentPool.ReturnToPool(this));
    }
    
    #region Pool Implementation
    
    public IPool ParentPool { get; set; }
    public bool IsDisposed { get; set; }
    public void OnRequested()
    {
        
    }

    public void OnDisposed()
    {
    }
    
    #endregion

}
