// Varianta C: planeta
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc.Teme;

public sealed class PlanetaScene : Scene
{
    private const string NewPlanetAction = "planetă nouă (seed)";
    private const string ToggleCloudsAction = "nori: afișare / ascundere";
    private const string ToggleCitiesAction = "orașe noaptea: pornit / oprit";

    private Material _planet = null!;
    private Tween _seedTween = null!;
    private Tween _cloudTween = null!;

    private readonly Random _random = new(42);
    private Vector3 _currentSeed = new(1.2f, 3.4f, 5.6f);
    private bool _cloudsVisible = true;
    private bool _cityLights = true;
    private int _planetNumber = 1;

    public override string Name => "tema C: planeta";

    public override IEnumerable<string> ActionNames =>
        new[] { NewPlanetAction, ToggleCloudsAction, ToggleCitiesAction };

    public override string Status =>
        $"planeta #{_planetNumber} | nori: {(_cloudsVisible ? "da" : "nu")} | orașe: {(_cityLights ? "da" : "nu")}";

    protected override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 0.4f, 4.2f);
        CameraTarget = Vector3.Zero;

        input.Bind(NewPlanetAction, Keys.G);
        input.Bind(ToggleCloudsAction, Keys.Space);
        input.Bind(ToggleCitiesAction, Keys.K);

        Mesh sphere = AddMesh(Shapes.ShadedSphere(1.35f, 72, 36));

        _planet = AddMaterial("planetă", "procedural.vert", "planet.frag")
            .Param("uSeaLevel", 0.25f, 0.75f, 0.48f)
            .Param("uIceCap", 0.45f, 1.00f, 0.78f)
            .Param("uScale", 1.20f, 5.00f, 2.40f)
            .Param("uCloudSpeed", 0.00f, 0.50f, 0.08f);

        _planet.Uniforms.Set("uSeed", _currentSeed);
        _planet.Uniforms.Set("uCloudVisibility", 1.0f);
        _planet.Uniforms.Set("uCityLights", _cityLights);

        _seedTween = new Tween(_currentSeed, _currentSeed, 1.2f, Easing.EaseInOut);
        _cloudTween = new Tween(new Vector3(1.0f), new Vector3(1.0f), 0.8f, Easing.EaseInOut);

        SceneObject planetObj = AddObject("planeta", sphere, _planet);
        planetObj.Transform.Position = Vector3.Zero;
        planetObj.Transform.Rotation = new Vector3(12.0f, 0.0f, -15.0f);
        planetObj.Spin = new Vector3(0.0f, 12.0f, 0.0f);
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        // Tasta G: genereaza o planeta noua printr-o tranzitie lina a samantei 3D
        if (input.WasPressed(NewPlanetAction))
        {
            _planetNumber++;
            _currentSeed = new Vector3(
                _random.NextSingle() * 40.0f - 20.0f,
                _random.NextSingle() * 40.0f - 20.0f,
                _random.NextSingle() * 40.0f - 20.0f
            );
            _seedTween.GoTo(_currentSeed, 1.2f);
            ConsolePanel.PrintMessage($"planeta #{_planetNumber}: seed nou ({_currentSeed.X:F1}, {_currentSeed.Y:F1}, {_currentSeed.Z:F1})");
        }

        // Tasta Space: ascunde / arata norii lin
        if (input.WasPressed(ToggleCloudsAction))
        {
            _cloudsVisible = !_cloudsVisible;
            _cloudTween.GoTo(new Vector3(_cloudsVisible ? 1.0f : 0.0f), 0.8f);
            ConsolePanel.PrintMessage($"nori: {(_cloudsVisible ? "vizibili" : "ascunși")}");
        }

        // Tasta K: porneste / opreste luminile oraselor pe partea intunecata
        if (input.WasPressed(ToggleCitiesAction))
        {
            _cityLights = !_cityLights;
            _planet.Uniforms.Set("uCityLights", _cityLights);
            ConsolePanel.PrintMessage($"orașe noaptea: {(_cityLights ? "pornite" : "oprite")}");
        }

        _seedTween.Update(dt);
        _cloudTween.Update(dt);

        _planet.Uniforms.Set("uSeed", _seedTween.Value);
        _planet.Uniforms.Set("uCloudVisibility", _cloudTween.Value.X);
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Planeta curentă: #{_planetNumber} | Seed: ({_seedTween.Value.X:F2}, {_seedTween.Value.Y:F2}, {_seedTween.Value.Z:F2})";
        yield return $"Nori: {(_cloudsVisible ? "activați" : "ascunși")} ({_cloudTween.Value.X:P0}) | Orașe noaptea: {(_cityLights ? "da" : "nu")}";
        yield return "Comenzi temă: G (planetă nouă), Space (nori), K (orașe noaptea), ↑↓←→ (parametri)";
    }
}