// Varianta C: Roata panoramică
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc.Teme;

public class RoataPanoramicaScene : Scene
{
    public override string Name => "Roata panoramică";

    private SceneObject _wheelHub = null!;
    private readonly List<SceneObject> _cabinPivots = new();
    private readonly List<SceneObject> _cabins = new();

    private float _currentSpeed = 30.0f; // grade / secunda
    private float _targetSpeed = 30.0f;
    private float _wheelAngle = 0.0f;

    // Legănarea cabinelor (resort amortizat)
    private float _swayAngle = 0.0f;
    private float _swayVelocity = 0.0f;

    // Tween pentru aducerea unei cabine la peron (tastele 1-8)
    private Tween? _stationTween;
    private bool _isTweeningToStation = false;

    public const string ActionSpeedUp = "mărește viteza roții";
    public const string ActionSpeedDown = "micșorează viteza roții";
    public const string ActionStop = "oprire lină roată";
    public const string ActionCabin1 = "aduce cabina 1 la peron";
    public const string ActionCabin2 = "aduce cabina 2 la peron";
    public const string ActionCabin3 = "aduce cabina 3 la peron";
    public const string ActionCabin4 = "aduce cabina 4 la peron";
    public const string ActionCabin5 = "aduce cabina 5 la peron";
    public const string ActionCabin6 = "aduce cabina 6 la peron";
    public const string ActionCabin7 = "aduce cabina 7 la peron";
    public const string ActionCabin8 = "aduce cabina 8 la peron";

    public override void Load(Input input)
    {
        // Pozitionam camera mai sus (Y = 4.5) si mai in spate (Z = 12.0) pentru incadrare completa
        CameraPosition = new Vector3(0.0f, 4.5f, 12.0f);
        CameraTarget = new Vector3(0.0f, 4.5f, 0.0f);

        // Legături acțiuni -> taste
        input.Bind(ActionSpeedUp, Keys.Up);
        input.Bind(ActionSpeedDown, Keys.Down);
        input.Bind(ActionStop, Keys.Space);

        input.Bind(ActionCabin1, Keys.D1, Keys.KeyPad1);
        input.Bind(ActionCabin2, Keys.D2, Keys.KeyPad2);
        input.Bind(ActionCabin3, Keys.D3, Keys.KeyPad3);
        input.Bind(ActionCabin4, Keys.D4, Keys.KeyPad4);
        input.Bind(ActionCabin5, Keys.D5, Keys.KeyPad5);
        input.Bind(ActionCabin6, Keys.D6, Keys.KeyPad6);
        input.Bind(ActionCabin7, Keys.D7, Keys.KeyPad7);
        input.Bind(ActionCabin8, Keys.D8, Keys.KeyPad8);

        // Forme geometrice
        Mesh cubeMesh = Shapes.ShadedCube();
        Mesh sphereMesh = Shapes.ShadedSphere(0.3f, 16, 8);

        // Pilonii de susținere
        SceneObject pillarLeft = AddObject("PilonStanga", cubeMesh);
        pillarLeft.Transform.Position = new Vector3(-1.2f, 2.5f, -0.5f);
        pillarLeft.Transform.Scale = new Vector3(0.2f, 5.0f, 0.2f);
        pillarLeft.Transform.Rotation = new Vector3(0, 0, -15.0f);
        pillarLeft.Tint = new Color4(0.4f, 0.4f, 0.45f, 1.0f);

        SceneObject pillarRight = AddObject("PilonDreapta", cubeMesh);
        pillarRight.Transform.Position = new Vector3(1.2f, 2.5f, -0.5f);
        pillarRight.Transform.Scale = new Vector3(0.2f, 5.0f, 0.2f);
        pillarRight.Transform.Rotation = new Vector3(0, 0, 15.0f);
        pillarRight.Tint = new Color4(0.4f, 0.4f, 0.45f, 1.0f);

        // Peron
        SceneObject platform = AddObject("Peron", cubeMesh);
        platform.Transform.Position = new Vector3(0.0f, 0.15f, 0.0f);
        platform.Transform.Scale = new Vector3(3.5f, 0.3f, 2.0f);
        platform.Tint = new Color4(0.6f, 0.3f, 0.1f, 1.0f);

        // Butuc central (Pivot) înălțat la Y = 5.0
        _wheelHub = AddObject("ButucRoata", sphereMesh);
        _wheelHub.Transform.Position = new Vector3(0.0f, 5.0f, 0.0f);
        _wheelHub.Transform.Scale = new Vector3(1.2f);
        _wheelHub.Tint = new Color4(0.9f, 0.2f, 0.2f, 1.0f);

        const int numCabins = 8;
        const float radius = 3.2f;

        for (int i = 0; i < numCabins; i++)
        {
            float angleDeg = i * (360.0f / numCabins);
            float angleRad = MathHelper.DegreesToRadians(angleDeg);

            // Pivot cabină (copil al butucului roții)
            SceneObject cabinPivot = AddObject($"PivotCabina_{i + 1}", null);
            cabinPivot.Parent = _wheelHub;
            cabinPivot.Transform.Position = new Vector3(radius * MathF.Cos(angleRad), radius * MathF.Sin(angleRad), 0.0f);
            _cabinPivots.Add(cabinPivot);

            // Spiță (poziționată puțin în spate pe Z)
            SceneObject spoke = AddObject($"Spita_{i + 1}", cubeMesh);
            spoke.Parent = _wheelHub;
            spoke.Transform.Position = new Vector3((radius / 2.0f) * MathF.Cos(angleRad), (radius / 2.0f) * MathF.Sin(angleRad), -0.05f);
            spoke.Transform.Scale = new Vector3(radius, 0.08f, 0.08f);
            spoke.Transform.Rotation = new Vector3(0, 0, angleDeg);
            spoke.Tint = new Color4(0.8f, 0.8f, 0.8f, 1.0f);

            // Corpul cabinei (copil al pivotului cabinei, adus puțin în față pe Z)
            SceneObject cabin = AddObject($"Cabina_{i + 1}", cubeMesh);
            cabin.Parent = cabinPivot;
            cabin.Transform.Position = new Vector3(0.0f, -0.4f, 0.1f);
            cabin.Transform.Scale = new Vector3(0.7f, 0.8f, 0.7f);

            float hue = (float)i / numCabins;
            cabin.Tint = ColorUtil.FromHsv(hue, 0.85f, 0.95f);
            _cabins.Add(cabin);
        }
    }

    public override void Update(float dt, Input input)
    {
        // 1. Modificarea vitezei țintă
        if (input.WasPressed(ActionSpeedUp))
        {
            _targetSpeed = MathF.Min(_targetSpeed + 30.0f, 180.0f);
            _isTweeningToStation = false;
        }
        if (input.WasPressed(ActionSpeedDown))
        {
            _targetSpeed = MathF.Max(_targetSpeed - 30.0f, -180.0f);
            _isTweeningToStation = false;
        }
        if (input.WasPressed(ActionStop))
        {
            _targetSpeed = 0.0f;
            _isTweeningToStation = false;
        }

        // 2. Tastele 1-8: aducerea cabinei alese la peron
        for (int i = 0; i < 8; i++)
        {
            string action = i switch
            {
                0 => ActionCabin1,
                1 => ActionCabin2,
                2 => ActionCabin3,
                3 => ActionCabin4,
                4 => ActionCabin5,
                5 => ActionCabin6,
                6 => ActionCabin7,
                _ => ActionCabin8
            };

            if (input.WasPressed(action))
            {
                MoveCabinToStation(i);
                break;
            }
        }

        // 3. Calculul vitezei reale independent de FPS
        float prevSpeed = _currentSpeed;
        if (!_isTweeningToStation)
        {
            _currentSpeed += (_targetSpeed - _currentSpeed) * (1.0f - MathF.Exp(-4.0f * dt));
            _wheelAngle += _currentSpeed * dt;
        }
        else if (_stationTween != null)
        {
            _stationTween.Update(dt);
            _wheelAngle = _stationTween.Value.X;
            _currentSpeed = (_wheelAngle - prevSpeed) / MathF.Max(dt, 0.0001f);

            if (_stationTween.IsFinished)
            {
                _isTweeningToStation = false;
                _targetSpeed = 0.0f;
                _currentSpeed = 0.0f;
            }
        }

        // 4. Legănarea cabinelor (resort amortizat)
        float accel = (_currentSpeed - prevSpeed) / MathF.Max(dt, 0.0001f);
        float springK = 25.0f;
        float damping = 3.0f;

        float force = -accel * 0.15f - springK * _swayAngle - damping * _swayVelocity;
        _swayVelocity += force * dt;
        _swayAngle += _swayVelocity * dt;

        // 5. Rotația ierarhică (Vector3 în grade)
        _wheelHub.Transform.Rotation = new Vector3(0, 0, _wheelAngle);

        float cabinLocalRotation = -_wheelAngle + _swayAngle;
        Vector3 cabinRot = new Vector3(0, 0, cabinLocalRotation);

        foreach (var cabin in _cabins)
        {
            cabin.Transform.Rotation = cabinRot;
        }
    }

    private void MoveCabinToStation(int cabinIndex)
    {
        float cabinAngleOffset = cabinIndex * 45.0f;
        float targetWheelAngle = -90.0f - cabinAngleOffset;

        float currentNormalized = _wheelAngle % 360.0f;
        float delta = (targetWheelAngle - currentNormalized) % 360.0f;

        if (delta > 180.0f) delta -= 360.0f;
        if (delta < -180.0f) delta += 360.0f;

        float finalTargetAngle = _wheelAngle + delta;

        _stationTween = new Tween(
            new Vector3(_wheelAngle, 0, 0),
            new Vector3(finalTargetAngle, 0, 0),
            2.0f,
            Easing.EaseInOut
        );

        _isTweeningToStation = true;
    }

    public override IEnumerable<string> Describe()
    {
        yield return "=== Varianta C: Roata Panoramică ===";
        yield return $"Viteză țintă: {_targetSpeed:F1}°/s | Viteză reală: {_currentSpeed:F1}°/s";
        yield return $"Unghi roată: {(_wheelAngle % 360.0f + 360.0f) % 360.0f:F1}° | Legănare cabină: {_swayAngle:F1}°";
        yield return "Comenzi: Săgeți Sus/Jos (viteză), Space (oprire), Taste 1-8 (aduce cabina la peron)";
    }
}