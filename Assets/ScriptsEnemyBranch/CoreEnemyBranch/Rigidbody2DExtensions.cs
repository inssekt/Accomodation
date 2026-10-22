using UnityEngine;

public static class Rigidbody2DExtensions
{
    public static Vector2 GetVelocity(this Rigidbody2D body) => body.linearVelocity;

    public static void SetVelocity(this Rigidbody2D body, Vector2 velocity) => body.linearVelocity = velocity;
}