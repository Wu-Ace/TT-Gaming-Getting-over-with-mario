using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class OrbitRidePlatform : MonoBehaviour
{
    public Vector2 center;
    public float radius=1.2f, period=7f, phase;
    Rigidbody2D body;
    float elapsed;
    void Awake(){body=GetComponent<Rigidbody2D>();}
    void FixedUpdate(){
        elapsed+=Time.fixedDeltaTime;
        float angle=(phase+elapsed/Mathf.Max(period,.5f))*Mathf.PI*2;
        body.MovePosition(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);
    }
}
