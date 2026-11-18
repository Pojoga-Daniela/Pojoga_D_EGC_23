using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc.Teme;

public class MasinaOrbitaScene : Scene
{
    public override string Name => "Mașină neagră cu minge orbitală";

    public const string ActionCarLeft = "deplasare mașină stânga";
    public const string ActionCarRight = "deplasare mașină dreapta";
    public const string ActionToggleAuto = "pornește / oprește mișcarea automată";

    private SceneObject _carRoot = null!;
    private SceneObject _orbitPivot = null!;
    private SceneObject _orbitingBall = null!;
    private readonly List<SceneObject> _wheelPivots = new();
    private Tween _carTween = null!;

    private float _carX = -4.0f;
    private float _currentYaw = 0.0f; // 0° = orientată spre dreapta (+X), 180° = orientată spre stânga (-X)
    private float _targetYaw = 0.0f;
    private float _wheelSpinAngle = 0.0f;

    private bool _autoMove = true;
    private const float ManualSpeed = 5.5f; // unități / secundă la controlul din săgeți
    private const float WheelRadius = 0.28f;

    private float _orbitAngle = 0.0f;
    private const float OrbitSpeed = 160.0f; // grade / secundă

    public override void Load(Input input)
    {
        // Legăm acțiunile de tastele săgeți și Space
        input.Bind(ActionCarLeft, Keys.Left);
        input.Bind(ActionCarRight, Keys.Right);
        input.Bind(ActionToggleAuto, Keys.Space);

        // Poziționăm camera astfel încât să încadreze tot traseul și mingea de deasupra
        CameraPosition = new Vector3(0.0f, 4.0f, 11.0f);
        CameraTarget = new Vector3(0.0f, 1.2f, 0.0f);

        Mesh cubeMesh = Shapes.ShadedCube();
        Mesh wheelMesh = Shapes.ShadedSphere(WheelRadius, 18, 12);
        Mesh rimMesh = Shapes.ShadedSphere(0.16f, 14, 10);
        Mesh ballMesh = Shapes.ShadedSphere(0.42f, 24, 16);

        // 1. Șoseaua
        SceneObject road = AddObject("Sosea", cubeMesh);
        road.Transform.Position = new Vector3(0.0f, -0.05f, 0.0f);
        road.Transform.Scale = new Vector3(13.0f, 0.1f, 4.2f);
        road.Tint = new Color4(0.30f, 0.32f, 0.36f, 1.0f);

        SceneObject roadLine = AddObject("MarcajSosea", cubeMesh);
        roadLine.Transform.Position = new Vector3(0.0f, 0.005f, 0.0f);
        roadLine.Transform.Scale = new Vector3(12.4f, 0.01f, 0.12f);
        roadLine.Tint = new Color4(0.85f, 0.85f, 0.88f, 1.0f);

        // 2. Rădăcina mașinii (Pivot invizibil cu scalare 1,1,1)
        _carRoot = AddObject("MasinaRoot", null);
        _carRoot.Transform.Position = new Vector3(_carX, 0.0f, 0.0f);

        // Caroseria inferioară (negru metalizat)
        SceneObject carBody = AddObject("CaroserieMasina", cubeMesh);
        carBody.Parent = _carRoot;
        carBody.Transform.Position = new Vector3(0.0f, 0.48f, 0.0f);
        carBody.Transform.Scale = new Vector3(2.5f, 0.48f, 1.15f);
        carBody.Tint = new Color4(0.17f, 0.18f, 0.21f, 1.0f);

        // Plafonul / structura cabinei
        SceneObject cabin = AddObject("CabinaMasina", cubeMesh);
        cabin.Parent = _carRoot;
        cabin.Transform.Position = new Vector3(-0.15f, 0.88f, 0.0f);
        cabin.Transform.Scale = new Vector3(1.25f, 0.42f, 0.96f);
        cabin.Tint = new Color4(0.14f, 0.15f, 0.18f, 1.0f);

        // Parbriz și lunetă
        SceneObject frontBackGlass = AddObject("ParbrizLuneta", cubeMesh);
        frontBackGlass.Parent = _carRoot;
        frontBackGlass.Transform.Position = new Vector3(-0.15f, 0.88f, 0.0f);
        frontBackGlass.Transform.Scale = new Vector3(1.30f, 0.32f, 0.84f);
        frontBackGlass.Tint = new Color4(0.45f, 0.65f, 0.80f, 1.0f);

        // Geamuri laterale
        SceneObject sideGlass = AddObject("GeamuriLaterale", cubeMesh);
        sideGlass.Parent = _carRoot;
        sideGlass.Transform.Position = new Vector3(-0.15f, 0.88f, 0.0f);
        sideGlass.Transform.Scale = new Vector3(1.05f, 0.30f, 0.99f);
        sideGlass.Tint = new Color4(0.45f, 0.65f, 0.80f, 1.0f);

        // Bare față și spate (argintiu / crom)
        SceneObject frontBumper = AddObject("BaraFata", cubeMesh);
        frontBumper.Parent = _carRoot;
        frontBumper.Transform.Position = new Vector3(1.28f, 0.36f, 0.0f);
        frontBumper.Transform.Scale = new Vector3(0.12f, 0.16f, 1.18f);
        frontBumper.Tint = new Color4(0.75f, 0.78f, 0.82f, 1.0f);

        SceneObject rearBumper = AddObject("BaraSpate", cubeMesh);
        rearBumper.Parent = _carRoot;
        rearBumper.Transform.Position = new Vector3(-1.28f, 0.36f, 0.0f);
        rearBumper.Transform.Scale = new Vector3(0.12f, 0.16f, 1.18f);
        rearBumper.Tint = new Color4(0.75f, 0.78f, 0.82f, 1.0f);

        // Faruri și stopuri
        float[] zLights = { 0.38f, -0.38f };
        for (int i = 0; i < zLights.Length; i++)
        {
            SceneObject headlight = AddObject($"Far_{i + 1}", cubeMesh);
            headlight.Parent = _carRoot;
            headlight.Transform.Position = new Vector3(1.26f, 0.54f, zLights[i]);
            headlight.Transform.Scale = new Vector3(0.08f, 0.13f, 0.24f);
            headlight.Tint = new Color4(1.0f, 0.96f, 0.75f, 1.0f);

            SceneObject taillight = AddObject($"Stop_{i + 1}", cubeMesh);
            taillight.Parent = _carRoot;
            taillight.Transform.Position = new Vector3(-1.26f, 0.54f, zLights[i]);
            taillight.Transform.Scale = new Vector3(0.08f, 0.13f, 0.24f);
            taillight.Tint = new Color4(0.95f, 0.15f, 0.15f, 1.0f);
        }

        // Cele 4 roți cu pivot individual (anvelopă + jantă + spiță vizibilă la rotație)
        _wheelPivots.Clear();
        Vector3[] wheelOffsets =
        {
            new Vector3(-0.78f, 0.26f,  0.58f),
            new Vector3( 0.78f, 0.26f,  0.58f),
            new Vector3(-0.78f, 0.26f, -0.58f),
            new Vector3( 0.78f, 0.26f, -0.58f)
        };

        for (int i = 0; i < wheelOffsets.Length; i++)
        {
            float sideSign = wheelOffsets[i].Z > 0 ? 1.0f : -1.0f;

            SceneObject wheelPivot = AddObject($"PivotRoata_{i + 1}", null);
            wheelPivot.Parent = _carRoot;
            wheelPivot.Transform.Position = wheelOffsets[i];
            _wheelPivots.Add(wheelPivot);

            SceneObject wheel = AddObject($"Roata_{i + 1}", wheelMesh);
            wheel.Parent = wheelPivot;
            wheel.Transform.Position = Vector3.Zero;
            wheel.Transform.Scale = new Vector3(1.0f, 1.0f, 0.55f);
            wheel.Tint = new Color4(0.07f, 0.07f, 0.08f, 1.0f);

            SceneObject rim = AddObject($"Janta_{i + 1}", rimMesh);
            rim.Parent = wheelPivot;
            rim.Transform.Position = new Vector3(0.0f, 0.0f, sideSign * 0.04f);
            rim.Transform.Scale = new Vector3(1.0f, 1.0f, 0.50f);
            rim.Tint = new Color4(0.78f, 0.80f, 0.85f, 1.0f);

            SceneObject spoke = AddObject($"SpitaJanta_{i + 1}", cubeMesh);
            spoke.Parent = wheelPivot;
            spoke.Transform.Position = new Vector3(0.0f, 0.0f, sideSign * 0.09f);
            spoke.Transform.Scale = new Vector3(0.24f, 0.045f, 0.04f);
            spoke.Tint = new Color4(0.22f, 0.24f, 0.28f, 1.0f);
        }

        // 3. Pivotul orbitei plasat deasupra plafonului mașinii
        _orbitPivot = AddObject("PivotBilaDeasupra", null);
        _orbitPivot.Parent = _carRoot;
        _orbitPivot.Transform.Position = new Vector3(0.0f, 1.90f, 0.0f);

        // Mingea blue deschis care se rotește circular deasupra mașinii
        _orbitingBall = AddObject("MingeOrbitala", ballMesh);
        _orbitingBall.Parent = _orbitPivot;
        _orbitingBall.Transform.Position = new Vector3(1.05f, 0.0f, 0.0f);
        _orbitingBall.Transform.Scale = Vector3.One;
        _orbitingBall.Spin = new Vector3(0.0f, 180.0f, 0.0f);
        _orbitingBall.Tint = new Color4(0.45f, 0.82f, 1.0f, 1.0f); // blue deschis

        // 4. Mișcarea automată dus-întors stânga-dreapta
        _carTween = new Tween(
            from: new Vector3(-4.0f, 0.0f, 0.0f),
            to: new Vector3(4.0f, 0.0f, 0.0f),
            duration: 3.0f,
            ease: Easing.EaseInOut,
            pingPong: true
        );
    }

    public override void Update(float dt, Input input)
    {
        float prevX = _carX;
        float moveAxis = input.Axis(ActionCarLeft, ActionCarRight);

        if (moveAxis != 0.0f)
        {
            // Control manual din săgeți
            _autoMove = false;
            _carX = Math.Clamp(_carX + moveAxis * ManualSpeed * dt, -4.8f, 4.8f);
            _carRoot.Transform.Position = new Vector3(_carX, 0.0f, 0.0f);
        }
        else if (input.WasPressed(ActionToggleAuto))
        {
            // Space comută între modul manual și cel automat
            _autoMove = !_autoMove;
            if (_autoMove)
            {
                float targetX = _carX <= 0.0f ? 4.0f : -4.0f;
                _carTween = new Tween(
                    from: new Vector3(_carX, 0.0f, 0.0f),
                    to: new Vector3(targetX, 0.0f, 0.0f),
                    duration: 3.0f,
                    ease: Easing.EaseInOut,
                    pingPong: true
                );
            }
        }
        else if (_autoMove)
        {
            // Mișcare automată dus-întors
            _carTween.Update(dt);
            _carRoot.Transform.Position = _carTween.Value;
            _carX = _carRoot.Transform.Position.X;
        }

        // Calculăm deplasarea pe axa X și orientăm mașina spre sensul de mers
        float dx = _carX - prevX;
        if (MathF.Abs(dx) > 0.0001f)
        {
            _targetYaw = dx > 0.0f ? 0.0f : 180.0f;
        }

        // Rotire lină a mașinii la capete sau la schimbarea direcției din săgeți
        _currentYaw += (_targetYaw - _currentYaw) * (1.0f - MathF.Exp(-12.0f * dt));
        _carRoot.Transform.Rotation = new Vector3(0.0f, _currentYaw, 0.0f);

        // Rotim roțile proporțional cu distanța parcursă pe șosea
        float forwardDistance = dx * MathF.Cos(MathHelper.DegreesToRadians(_currentYaw));
        float deltaWheelDeg = MathHelper.RadiansToDegrees(forwardDistance / WheelRadius);
        _wheelSpinAngle = (_wheelSpinAngle - deltaWheelDeg) % 360.0f;

        Vector3 wheelRot = new Vector3(0.0f, 0.0f, _wheelSpinAngle);
        foreach (var wheelPivot in _wheelPivots)
        {
            wheelPivot.Transform.Rotation = wheelRot;
        }

        // Mingea se rotește permanent deasupra mașinii
        _orbitAngle = (_orbitAngle + OrbitSpeed * dt) % 360.0f;
        _orbitPivot.Transform.Rotation = new Vector3(0.0f, _orbitAngle, 0.0f);
        _orbitingBall.Update(dt);
    }

    public override IEnumerable<string> Describe()
    {
        string direction = _targetYaw < 90.0f ? "Dreapta (+X)" : "Stânga (-X)";
        yield return "=== Scena: Mașină neagră cu minge orbitală ===";
        yield return $"Mod: {(_autoMove ? "Automat" : "Manual")} | Poziție X: {_carX:F2} | Orientare: {direction}";
        yield return $"Unghi orbită minge: {_orbitAngle:F1}° (viteză: {OrbitSpeed:F0}°/s)";
        yield return "Comenzi: Săgeți Stânga/Dreapta (control manual), Space (reia mișcarea automată)";
    }
}