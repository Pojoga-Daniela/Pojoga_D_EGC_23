using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Camera FPS (first-person): orientarea vine din două unghiuri Euler, yaw și pitch,
/// controlate cu mouse-ul; deplasarea se face cu W A S D.
/// În modul "mers" (implicit) camera rămâne la înălțimea ochilor; în modul "zbor liber"
/// (free-fly, tasta G) se deplasează pe direcția privirii.
/// </summary>
public class FpsCamera : Camera
{
    // --- Starea camerei ---------------------------------------------------------

    private Vector3 _position;
    public override Vector3 Position => _position;

    /// <summary>Girația (yaw), în grade: rotația stânga-dreapta, în jurul axei Y. -90° = privire spre -Z.</summary>
    public float Yaw { get; private set; }

    /// <summary>Tangajul (pitch), în grade: rotația sus-jos. Limitat la ±MaxPitch.</summary>
    public float Pitch { get; private set; }

    /// <summary>Zbor liber (true) sau mers pe sol (false). Comutat cu tasta G (facultativ, TODO 5.7).</summary>
    public bool FreeFly { get; set; }

    // Baza camerei: trei vectori unitate, perpendiculari doi câte doi (secțiunea 5).
    private Vector3 _front = -Vector3.UnitZ;
    private Vector3 _right = Vector3.UnitX;
    private Vector3 _up = Vector3.UnitY;

    public override Vector3 Front => _front;

    // --- Parametri ----------------------------------------------------------------

    public const float MaxPitch = 89.0f;               // grade; de ce nu 90, vezi secțiunea 5.3
    public float MoveSpeed = 3.0f;                     // unități de lume pe secundă
    public float SprintFactor = 2.5f;                  // Shift ținut apăsat
    public float MouseSensitivity = 0.1f;              // grade pe pixel
    public float FovStep = 2.0f;                       // grade pentru un pas al rotiței

    private static readonly Vector3 WorldUp = Vector3.UnitY;
    private static readonly Vector3 StartPosition = new(0.0f, 1.7f, 6.0f);   // 1,7 m = înălțimea ochilor
    private const float StartYaw = -90.0f;
    private const float StartPitch = 0.0f;

    public FpsCamera()
    {
        Reset();
    }

    public override string Name => FreeFly ? "FPS – zbor liber" : "FPS – mers";

    /// <summary>Matricea view: camera aflată în Position privește spre punctul Position + Front.</summary>
    public override Matrix4 ViewMatrix =>
        Matrix4.LookAt(_position, _position + _front, _up);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        // --- 1. Orientarea: mouse-ul modifică yaw și pitch -------------------------

        // TODO 5.5 (a): deplasarea pe orizontală modifică yaw, cea pe verticală modifică pitch.
        //               Atenție: pe ecran Y crește în jos, iar pitch pozitiv înseamnă privire în sus.
        //               Apoi limitați pitch-ul la intervalul [-MaxPitch, MaxPitch].

        // 1. Orientarea: mouse-ul modifică yaw și pitch
        Yaw += mouseDelta.X * MouseSensitivity;
        Pitch -= mouseDelta.Y * MouseSensitivity; // Semn minus deoarece Y pe ecran crește în jos

        // Limităm pitch-ul la ±89° pentru a evita răsturnarea imaginii (gimbal lock)
        Pitch = Math.Clamp(Pitch, -MaxPitch, MaxPitch);

        UpdateVectors();

        // --- 2. FOV: rotița mouse-ului --------------------------------------------

        // TODO 5.1 (c): rotița în sus (ScrollDelta.Y > 0) micșorează FOV-ul (efect de zoom)
        //               (folosiți ChangeFov din clasa Camera și FovStep)


        if (mouse.ScrollDelta.Y != 0)
        {
            ChangeFov(-mouse.ScrollDelta.Y * 2.0f);
        }


        // --- 3. Deplasarea: W A S D ---------------------------------------------------

        // TODO 5.6: direcția "înainte" (în modul mers: proiecția lui Front pe planul orizontal),
        //           apoi deplasarea cu viteză constantă, independentă de numărul de cadre pe secundă.
        //           Pași: (1) forward; (2) vectorul "move" = suma direcțiilor tastelor apăsate
        //           (W +forward, S -forward, D +_right, A -_right); (3) normalizați "move";
        //           (4) _position += move * viteză * dt (viteza crește de SprintFactor ori cu Shift stânga).

        // TODO 5.7 (facultativ): în zbor liber (FreeFly), "forward" este chiar _front,
        //           iar Space urcă și Ctrl stânga coboară (pe axa Y a lumii, WorldUp).
        // --- P6 (facultativ): Comutare mod Zbor Liber (FreeFly) cu tasta G ------------
        if (keyboard.IsKeyPressed(Keys.G))
        {
            FreeFly = !FreeFly;
        }

        // --- P5 & P6: Calculul deplasării (W, A, S, D, Space, Ctrl) ------------------

        // Multiplicator de viteză când tasta LeftShift este apăsată
        float speedMultiplier = keyboard.IsKeyDown(Keys.LeftShift) ? SprintFactor : 1.0f;
        float currentSpeed = MoveSpeed * speedMultiplier;

        // Direcția "înainte": în modul mers (y = 0) este proiectată pe orizontală; în modul zbor (FreeFly) este chiar direcția de privire
        Vector3 forward = FreeFly ? _front : Vector3.Normalize(new Vector3(_front.X, 0.0f, _front.Z));

        Vector3 moveDir = Vector3.Zero;

        if (keyboard.IsKeyDown(Keys.W)) moveDir += forward;
        if (keyboard.IsKeyDown(Keys.S)) moveDir -= forward;
        if (keyboard.IsKeyDown(Keys.D)) moveDir += _right;
        if (keyboard.IsKeyDown(Keys.A)) moveDir -= _right;

        // P6 (facultativ): În modul FreeFly, Space urcă și LeftControl coboară pe axa Y
        if (FreeFly)
        {
            if (keyboard.IsKeyDown(Keys.Space)) moveDir += Vector3.UnitY;
            if (keyboard.IsKeyDown(Keys.LeftControl)) moveDir -= Vector3.UnitY;
        }

        // Normalizăm vectorul de mișcare pentru ca deplasarea pe diagonală (W + D) să nu fie mai rapidă
        if (moveDir != Vector3.Zero)
        {
            moveDir = Vector3.Normalize(moveDir);
        }

        // Actualizăm poziția camerei independent de FPS (înmulțind cu dt)
        _position += moveDir * currentSpeed * dt;
    }

    /// <summary>
    /// Recalculează baza camerei din yaw și pitch (unghiuri Euler -> vector de direcție).
    /// </summary>
    private void UpdateVectors()
    {
        // TODO 5.5 (b): Front din yaw și pitch (formula din secțiunea 5.2), apoi Right și Up
        //               prin produse vectoriale; toți trei normalizați.
        //               Până atunci camera privește mereu spre -Z.
        // Convertim unghiurile din grade în radiani pentru funcțiile trigonometrice
        float yawRad = MathHelper.DegreesToRadians(Yaw);
        float pitchRad = MathHelper.DegreesToRadians(Pitch);

        // Calculăm vectorul direcție front din unghiurile yaw și pitch
        Vector3 front;
        front.X = MathF.Cos(yawRad) * MathF.Cos(pitchRad);
        front.Y = MathF.Sin(pitchRad);
        front.Z = MathF.Sin(yawRad) * MathF.Cos(pitchRad);

        _front = Vector3.Normalize(front);

        // Calculăm vectorii right și up prin produse vectoriale (cross product)
        _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
    }

    public override void Reset()
    {
        _position = StartPosition;
        Yaw = StartYaw;
        Pitch = StartPitch;
        FieldOfView = 60.0f;
        UpdateVectors();
    }

    public override IEnumerable<string> Describe()
    {
        yield return $"Poziție        : ({_position.X,6:F2}, {_position.Y,6:F2}, {_position.Z,6:F2})";
        yield return $"Yaw / pitch    : {NormalizeAngle(Yaw),7:F1}° / {Pitch,6:F1}°";
        yield return $"Front          : ({_front.X,6:F2}, {_front.Y,6:F2}, {_front.Z,6:F2})";
        yield return $"Right          : ({_right.X,6:F2}, {_right.Y,6:F2}, {_right.Z,6:F2})";
        yield return $"Up             : ({_up.X,6:F2}, {_up.Y,6:F2}, {_up.Z,6:F2})";
    }

    /// <summary>Aduce un unghi în intervalul (-180°, 180°] (doar pentru afișare).</summary>
    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360.0f;
        if (degrees > 180.0f) degrees -= 360.0f;
        if (degrees <= -180.0f) degrees += 360.0f;
        return degrees;
    }
}
