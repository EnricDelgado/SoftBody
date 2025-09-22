using UnityEngine;

public class SoftBody2D : MonoBehaviour
{
    public Transform[] controlPoints; // place them around the sprite
    public float stiffness = 8f;
    public float damping   = 0.9f;
    Vector3[] vel;
    Vector3[] restLocal;

    void Start()
    {
        vel = new Vector3[controlPoints.Length];
        restLocal = new Vector3[controlPoints.Length];
        for (int i = 0; i < controlPoints.Length; i++)
            restLocal[i] = controlPoints[i].localPosition;
    }

    void LateUpdate()
    {
        for (int i = 0; i < controlPoints.Length; i++)
        {
            Vector3 p = controlPoints[i].localPosition;
            Vector3 toRest = restLocal[i] - p;
            // simple spring
            vel[i] = vel[i] * damping + toRest * (stiffness * Time.deltaTime);
            controlPoints[i].localPosition += vel[i] * Time.deltaTime;
        }
    }

    // Call this when you detect an impact: n = hit normal, amt = 0..1
    public void Nudge(Vector2 n, float amt = 0.2f)
    {
        for (int i = 0; i < controlPoints.Length; i++)
        {
            Vector3 dir = (controlPoints[i].position - transform.position).normalized;
            float influence = Mathf.Clamp01(Vector3.Dot(dir, -new Vector3(n.x, n.y, 0)));
            vel[i] += -new Vector3(n.x, n.y, 0) * (amt * influence);
        }
    }
}