using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class SoftBodyShaderController : MonoBehaviour
{
    public float radius = 0.5f;        // roughly half sprite size in world units
    public float wobbleDamping = 6f;   // how fast wobble dies
    public float wobbleFreq = 10f;     // wobble frequency
    public float squashImpact = 0.35f; // squash intensity on collisions

    Vector2 wobble, wobbleVel;
    float squash, squashVel;

    SpriteRenderer sr;
    Rigidbody2D rb;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        mpb = new MaterialPropertyBlock();

        // initialize radius if you want from sprite bounds
        if (sr.sprite != null)
        {
            var ext = sr.sprite.bounds.extents; // world units (depends on transform scale)
            radius = Mathf.Max(ext.x, ext.y);
        }
    }

    void Update()
    {
        // Critically damped spring back to zero
        SpringToZero(ref wobble, ref wobbleVel, wobbleFreq, wobbleDamping);
        SpringToZero(ref squash, ref squashVel, wobbleFreq, wobbleDamping);

        sr.GetPropertyBlock(mpb);
        mpb.SetVector("_Wobble", new Vector4(wobble.x, wobble.y, 0, 0));
        mpb.SetFloat("_Squash", squash);
        mpb.SetFloat("_Radius", radius);
        sr.SetPropertyBlock(mpb);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.contactCount == 0) return;
        var n = col.GetContact(0).normal;
        float impulse = col.relativeVelocity.magnitude;

        // Drive wobble opposite the hit normal
        wobble += -n * Mathf.Clamp01(impulse * 0.02f);
        // Add a bit of squash
        squash += Mathf.Clamp(impulse * squashImpact * 0.01f, -0.6f, 0.6f);
    }

    static void SpringToZero(ref Vector2 x, ref Vector2 v, float freq, float damping)
    {
        float dt = Time.deltaTime;
        if (dt <= 0) return;
        float k = (2 * Mathf.PI * freq);
        float c = 2 * damping * k;
        v += (-k*k * x - c * v) * dt;
        x += v * dt;
    }

    static void SpringToZero(ref float x, ref float v, float freq, float damping)
    {
        float dt = Time.deltaTime;
        if (dt <= 0) return;
        float k = (2 * Mathf.PI * freq);
        float c = 2 * damping * k;
        v += (-k*k * x - c * v) * dt;
        x += v * dt;
    }
}
