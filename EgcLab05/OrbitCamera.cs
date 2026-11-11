using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Camera Orbită privește spre o țintă fixă și se rotește în jurul ei pe o sferă.
/// </summary>
public class OrbitCamera : Camera
{
    public Vector3 Target = Vector3.Zero;
    public float Distance = 5.0f;
    public float Azimuth = -90.0f;   // grade în jurul axei Y
    public float Elevation = 20.0f;  // grade deasupra planului orizontal

    public const float MinDistance = 1.0f;
    public const float MaxDistance = 30.0f;
    public const float MaxElevation = 89.0f;

    public float MouseSensitivity = 0.25f;
    public float KeyRotationSpeed = 60.0f;

    public override string Name => "Orbită";

    public override Vector3 Position
    {
        get
        {
            float azRad = MathHelper.DegreesToRadians(Azimuth);
            float elRad = MathHelper.DegreesToRadians(Elevation);

            float x = Target.X + Distance * MathF.Cos(elRad) * MathF.Cos(azRad);
            float y = Target.Y + Distance * MathF.Sin(elRad);
            float z = Target.Z + Distance * MathF.Cos(elRad) * MathF.Sin(azRad);

            return new Vector3(x, y, z);
        }
    }

    public override Vector3 Front => Vector3.Normalize(Target - Position);

    public override Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, Vector3.UnitY);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        // 1. Rotația cu mouse-ul (clic stânga apăsat)
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth += mouseDelta.X * MouseSensitivity;
            Elevation -= mouseDelta.Y * MouseSensitivity;
        }

        // 2. Rotația cu săgețile de la tastatură
        if (keyboard.IsKeyDown(Keys.Left)) Azimuth -= KeyRotationSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Right)) Azimuth += KeyRotationSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Up)) Elevation += KeyRotationSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Down)) Elevation -= KeyRotationSpeed * dt;

        // Limitarea elevației la ±89°
        Elevation = Math.Clamp(Elevation, -MaxElevation, MaxElevation);

        // 3. Rotița mouse-ului modifică distanța față de țintă
        if (mouse.ScrollDelta.Y != 0)
        {
            Distance -= mouse.ScrollDelta.Y * 0.5f;
            Distance = Math.Clamp(Distance, MinDistance, MaxDistance);
        }

        // 4. Deplasarea țintei în plan orizontal cu W, A, S, D
        float moveSpeed = 3.0f * dt;
        float azRad = MathHelper.DegreesToRadians(Azimuth);
        Vector3 forward = Vector3.Normalize(new Vector3(-MathF.Cos(azRad), 0, -MathF.Sin(azRad)));
        Vector3 right = Vector3.Normalize(new Vector3(-MathF.Sin(azRad), 0, MathF.Cos(azRad)));

        if (keyboard.IsKeyDown(Keys.W)) Target += forward * moveSpeed;
        if (keyboard.IsKeyDown(Keys.S)) Target -= forward * moveSpeed;
        if (keyboard.IsKeyDown(Keys.D)) Target += right * moveSpeed;
        if (keyboard.IsKeyDown(Keys.A)) Target -= right * moveSpeed;
    }

    public override void Reset()
    {
        Target = Vector3.Zero;
        Distance = 5.0f;
        Azimuth = -90.0f;
        Elevation = 20.0f;
    }

    public override IEnumerable<string> Describe()
    {
        yield return $"Camera Orbită | Țintă: ({Target.X:F2}, {Target.Y:F2}, {Target.Z:F2})";
        yield return $"Distanță: {Distance:F2} | Azimut: {Azimuth:F1}° | Elevație: {Elevation:F1}°";
        yield return $"Poziție: ({Position.X:F2}, {Position.Y:F2}, {Position.Z:F2})";
    }
}