#version 330 core

// Tema 07 - Varianta C: Planeta procedurala cu zgomot 3D (fbm)

in vec3 vColor;
in vec2 vUV;
in vec3 vLocalPos;

uniform bool uShowUV;
uniform float uTime;
uniform vec3 uSeed;              // samanta zgomotului 3D (animata cu Tween)
uniform float uSeaLevel;         // nivelul oceanului
uniform float uIceCap;           // latitudinea calotelor polare
uniform float uScale;            // scara continentelor
uniform float uCloudVisibility;  // vizibilitatea norilor (0..1, animata cu Tween)
uniform float uCloudSpeed;       // viteza de deplasare a norilor
uniform bool uCityLights;        // bonus: orase luminate pe partea intunecata

out vec4 FragColor;

// Hash 3D pseudo-aleator in [0, 1)
float hash3(vec3 p)
{
    p = fract(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return fract((p.x + p.y) * p.z);
}

// Zgomot 3D: interpolare trilineara neteda intre cele 8 colturi ale celulei
float valueNoise3D(vec3 p)
{
    vec3 i = floor(p);
    vec3 f = fract(p);
    vec3 u = f * f * (3.0 - 2.0 * f);

    float n000 = hash3(i + vec3(0.0, 0.0, 0.0));
    float n100 = hash3(i + vec3(1.0, 0.0, 0.0));
    float n010 = hash3(i + vec3(0.0, 1.0, 0.0));
    float n110 = hash3(i + vec3(1.0, 1.0, 0.0));
    float n001 = hash3(i + vec3(0.0, 0.0, 1.0));
    float n101 = hash3(i + vec3(1.0, 0.0, 1.0));
    float n011 = hash3(i + vec3(0.0, 1.0, 1.0));
    float n111 = hash3(i + vec3(1.0, 1.0, 1.0));

    float nx00 = mix(n000, n100, u.x);
    float nx10 = mix(n010, n110, u.x);
    float nx01 = mix(n001, n101, u.x);
    float nx11 = mix(n011, n111, u.x);

    float nxy0 = mix(nx00, nx10, u.y);
    float nxy1 = mix(nx01, nx11, u.y);

    return mix(nxy0, nxy1, u.z);
}

// Fractal Brownian Motion 3D (5 octave)
float fbm3(vec3 p)
{
    float sum = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 5; i++)
    {
        sum += amplitude * valueNoise3D(p);
        p *= 2.02;
        amplitude *= 0.5;
    }
    return sum;
}

// Stratul de nori: fronturi rarefiate + detalii fine de vant
float cloudLayer(vec3 dir)
{
    vec3 windOffset = vec3(uTime * uCloudSpeed, 0.0, -uTime * uCloudSpeed * 0.5);
    vec3 pCloud = dir * vec3(uScale * 1.35, uScale * 2.5, uScale * 1.35) + uSeed * 2.3 + windOffset;

    // Deformare usoara tip curenti atmosferici
    vec3 warp = vec3(
        fbm3(pCloud * 0.9 + vec3(3.1)),
        fbm3(pCloud * 0.9 + vec3(7.4)),
        fbm3(pCloud * 0.9 + vec3(1.9))
    ) - 0.5;
    vec3 cPos = pCloud + warp * 0.55;

    // 1. Fronturi mari rare (lasa mult ocean si uscat vizibil)
    float macro = smoothstep(0.53, 0.68, fbm3(cPos));
    // 2. Detaliu fin care sparge norii in fasii subtiri si bine conturate
    float detail = fbm3(cPos * 2.7 + vec3(5.2));
    float rawCloud = macro * smoothstep(0.36, 0.72, detail);

    return smoothstep(0.16, 0.50, rawCloud) * uCloudVisibility;
}

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    vec3 nPos = normalize(vLocalPos);

    // 1. Relieful (altitudinea) din zgomot 3D
    float h = fbm3(nPos * uScale + uSeed);

    // 2. Oceanul (mai deschis langa tarm)
    vec3 deepOcean    = vec3(0.03, 0.13, 0.36);
    vec3 shallowOcean = vec3(0.11, 0.46, 0.68);
    float shoreFactor = smoothstep(uSeaLevel - 0.12, uSeaLevel, h);
    vec3 oceanColor   = mix(deepOcean, shallowOcean, shoreFactor);

    // 3. Uscatul: plaja, campie, padure, munte, zapada doar pe crestele foarte inalte
    float alt = clamp((h - uSeaLevel) / max(1.0 - uSeaLevel, 0.001), 0.0, 1.0);
    vec3 beach    = vec3(0.85, 0.79, 0.56);
    vec3 plains   = vec3(0.24, 0.56, 0.19);
    vec3 forest   = vec3(0.10, 0.35, 0.12);
    vec3 mountain = vec3(0.45, 0.42, 0.39);
    vec3 snow     = vec3(0.95, 0.96, 0.98);

    vec3 landColor = mix(beach, plains, smoothstep(0.02, 0.09, alt));
    landColor = mix(landColor, forest, smoothstep(0.16, 0.38, alt));
    landColor = mix(landColor, mountain, smoothstep(0.45, 0.68, alt));
    landColor = mix(landColor, snow, smoothstep(0.82, 0.95, alt));

    // Trecere neteda la linia tarmului
    float landMask = smoothstep(uSeaLevel - 0.008, uSeaLevel + 0.008, h);
    vec3 surface = mix(oceanColor, landColor, landMask);

    // 4. Calote polare cu margine neregulata
    float poleNoise = (fbm3(nPos * (uScale * 1.8) + uSeed * 1.7) - 0.5) * 0.14;
    float iceMask = smoothstep(uIceCap - 0.04, uIceCap + 0.04, abs(nPos.y) + poleNoise);
    surface = mix(surface, vec3(0.92, 0.96, 1.0), iceMask);

    // 5. Bonus: orase luminate si halou urban pe partea umbrita
    vec3 lights = vec3(0.0);
    if (uCityLights)
    {
        float darkSide = 1.0 - smoothstep(0.32, 0.68, vColor.r);
        float habitable = landMask * (1.0 - iceMask) * (1.0 - smoothstep(0.50, 0.72, alt));
        float region = smoothstep(0.46, 0.66, fbm3(nPos * 7.5 + uSeed * 1.9));
        float fine = valueNoise3D(nPos * 55.0 + uSeed * 3.1);
        float core = smoothstep(0.66, 0.82, fine) * region;
        float glow = smoothstep(0.50, 0.76, fine) * region * 0.45;
        float cityMask = (core + glow) * habitable * darkSide;
        lights = vec3(1.0, 0.72, 0.22) * cityMask * 2.2;
    }

    // 6. Norii si umbra lor exacta decalata pe suprafata
    float clouds = cloudLayer(nPos);
    float cloudShadow = cloudLayer(normalize(nPos + vec3(-0.025, 0.02, 0.02)));

    surface *= 1.0 - cloudShadow * 0.48;
    surface = mix(surface, vec3(0.98, 0.99, 1.0), clouds * 0.92);

    vec3 finalColor = surface * vColor + lights * (1.0 - clouds * 0.65);
    FragColor = vec4(finalColor, 1.0);
}