using UnityEngine;

// Kinematic motion participates in hammer/body collision resolution.
[RequireComponent(typeof(Rigidbody2D))]
public sealed class VerticalRidePlatform : MonoBehaviour
{
    public float travel = 3.2f;
    public float period = 6f;
    public float phase;
    Rigidbody2D body;
    Vector2 origin;
    float elapsed;
    void Awake() { body=GetComponent<Rigidbody2D>(); origin=body.position; }
    void FixedUpdate()
    {
        elapsed+=Time.fixedDeltaTime;
        float t=(1-Mathf.Cos((elapsed/Mathf.Max(period,.5f)+phase)*Mathf.PI*2))*.5f;
        body.MovePosition(origin+Vector2.up*(travel*t));
    }
}
