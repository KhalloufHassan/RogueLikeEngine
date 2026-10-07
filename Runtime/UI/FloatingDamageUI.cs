using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Weapons;
using TMPro;
using UnityEngine;

public class FloatingDamageUI : MonoBehaviour,IPoolObject
{
    private const float PunchDuration = 0.3f;

    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float floatingDistance;
    [SerializeField] private float floatingDuration;
    [SerializeField] private float randomOffsetValue;
    [SerializeField] private float scalePunch;

    private Vector3 m_baseScale;
    private Vector3 m_startPosition;
    private Vector3 m_floatDirection = Vector3.up;
    private float m_elapsed;
    private bool m_isAnimating;

    private void Awake()
    {
        m_baseScale = transform.localScale;
    }

    public void Show(Damage damage,Vector3 position)
    {
        Camera viewer = Camera.main;
        Vector3 right = viewer ? viewer.transform.right : Vector3.right;
        m_floatDirection = viewer ? viewer.transform.up : Vector3.up;
        if (viewer) transform.rotation = viewer.transform.rotation;

        text.text = damage.Value.ToString();
        transform.position = position + right * Random.Range(-randomOffsetValue, randomOffsetValue);

        Animate();
    }

    private void Animate()
    {
        Color c = text.color;
        c.a = 1f;
        text.color = c;

        m_startPosition = transform.position;
        m_elapsed = 0;
        m_isAnimating = true;
    }

    private void Update()
    {
        if (!m_isAnimating) return;

        m_elapsed += Time.deltaTime;
        float t = floatingDuration > 0 ? Mathf.Clamp01(m_elapsed / floatingDuration) : 1f;

        // Move upward (ease out cubic)
        float moveT = 1f - Mathf.Pow(1f - t, 3f);
        transform.position = m_startPosition + m_floatDirection * (floatingDistance * moveT);

        // Fade out (ease in cubic)
        Color c = text.color;
        c.a = 1f - t * t * t;
        text.color = c;

        // Scale punch
        float punchT = Mathf.Clamp01(m_elapsed / PunchDuration);
        transform.localScale = m_baseScale * (1f + Mathf.Sin(punchT * Mathf.PI) * scalePunch);

        if (t >= 1f) Release();
    }

    private void Release()
    {
        m_isAnimating = false;
        if (ParentPool != null)
            ParentPool.ReturnToPool(this);
        else
            Destroy(gameObject);
    }

    #region Pool Implementation

    public IPool ParentPool { get; set; }
    public bool IsDisposed { get; set; }
    public void OnRequested()
    {

    }

    public void OnDisposed()
    {
        m_isAnimating = false;
        transform.localScale = m_baseScale;
    }

    #endregion

}
