using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public partial class Corridor : MonoBehaviour
{
    const float AmplitudeStep = .05f;
    const float CycleStep = .05f;
    const float PhaseStep = 5f;
    const float OffsetStep = .01f;
    const float TargetOffset = .35f;
    const int MaxScore = 1500;
    int score, scoreChange;
    float corridorMood, swayTime;
    GameObject corridorDisplay;
    Texture2D edgeHaze;
    readonly Texture2D[] portraits = new Texture2D[2];
    const int PortraitSize = 48;
    readonly Color32[] portraitPixels = new Color32[PortraitSize*PortraitSize];
    readonly int[] portraitFrames = { -1, -1 };
    LivePortraitCamera livePortrait;
    static bool SupportsPortraitCamera => Application.platform != RuntimePlatform.WebGLPlayer;
    bool cameraEnabled = true;
    static readonly Color LowMood = new Color(.31f,.045f,.07f);
    static readonly Color HighMood = new Color(.55f,.78f,.83f);
    const float FlightDuration = 1.05f;
    const int FrameCount = 11;
    const float FrontFrame = 2f;
    const float FrameSpacing = 1.25f;
    const float CorridorEnd = FrontFrame + (FrameCount - 1) * FrameSpacing;
    const float SecondsPerBlock = 15f;
    const float LaunchCooldown = 3f;
    const float LevelDuration = (FrameCount - 1) * SecondsPerBlock;
    float elapsed, readyAt, firedCycles, firedPower;
    int powerTenths;
    readonly List<Color> shotColors = new List<Color>();
    readonly List<float> shotPowers = new List<float>();
    readonly float[] fired = new float[N];
    int historyPage;
    Button launch, history;

    const int N = 161;
    const float Span = 3.3f;
    readonly float[] original = new float[N], residual = new float[N], wave = new float[N], sum = new float[N];
    readonly List<float[]> shots = new List<float[]>();
    readonly Color targetRed = new Color(1f,.095f,.16f), waveGreen = new Color(.08f,1f,.29f), grid = new Color(.13f,.31f,.36f);
    readonly List<float> shotCycles = new List<float>();
    float accuracy, efficiency, elegance, consistency, finalRms;
    enum Stage { Tune, Flight, Impact, Reveal }
    Stage stage;
    AudioSource music, effects;
    AudioClip launchSound, clashSound;
    Vector3 cameraRest;
    Quaternion cameraRestRotation;
    float hitQuality;

    void CreateAudio()
    {
        if (Object.FindAnyObjectByType<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();
        music = gameObject.AddComponent<AudioSource>();
        effects = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = effects.playOnAwake = false;
        music.loop = true;
        music.clip = Resources.Load<AudioClip>("IndustrialLoop");
        music.volume = .23f;
        music.Play();
        launchSound = Resources.Load<AudioClip>("LaunchCrack");
        clashSound = Resources.Load<AudioClip>("ClashExplosion");
        cameraRest = transform.localPosition;
        cameraRestRotation = transform.localRotation;
    }

    ArcadeControls cabinet;
    struct DirectionRepeat
    {
        int held;
        float next;
        public int Steps(bool positive,bool negative,float now)
        {
            int direction = (positive ? 1 : 0) - (negative ? 1 : 0);
            if (direction != held)
            {
                held = direction;
                next = now + .28f;
                return direction;
            }
            if (held == 0 || now < next) return 0;
            int count = 1 + Mathf.FloorToInt((now-next)/.08f);
            next += count*.08f;
            return held*count;
        }
    }
    DirectionRepeat verticalRepeat, horizontalRepeat;
    Camera viewCamera;
    Material ink;
    PanelSettings controlSettings;
    VisualElement tuning;
    Button replay;
    Slider amplitudeControl, cyclesControl, phaseControl, offsetControl;
    Mesh geometry;
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<Color> colors = new List<Color>();
    readonly List<int> lines = new List<int>();
    readonly List<int> faces = new List<int>();
    float amp, phase, verticalOffset, timer, gain, nextGain, depth;
    float frequency;

    static float Harmonic(int i,float a,float f,float p)
    {
        return a*Mathf.Sin(2*Mathf.PI*f*i/(N-1)+p*Mathf.Deg2Rad);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartCorridor()
    {
        if (Object.FindAnyObjectByType<Corridor>() != null) return;
        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = new GameObject("Corridor camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
        }
        camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        camera.orthographic = false;
        camera.gameObject.AddComponent<Corridor>();
    }

    void Awake()
    {
        var cam = viewCamera = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.025f,.043f,.06f);
        cam.fieldOfView = 54;
        ink = new Material(Resources.Load<Shader>("Corridor"));
        var display = corridorDisplay = new GameObject("Corridor geometry", typeof(MeshFilter), typeof(MeshRenderer));
        display.transform.SetPositionAndRotation(transform.position,transform.rotation);
        geometry = new Mesh { name = "Corridor and waveforms" };
        geometry.MarkDynamic();
        display.GetComponent<MeshFilter>().sharedMesh = geometry;
        display.GetComponent<MeshRenderer>().sharedMaterials = new[] { ink, ink };
        cabinet = new ArcadeControls();
        secondCabinet = new ArcadeControls();
        RefreshCabinets();
        edgeHaze = new Texture2D(64,1,TextureFormat.RGBA32,false);
        edgeHaze.wrapMode = TextureWrapMode.Clamp;
        for (int x=0; x<64; x++)
        {
            float edge = Mathf.Abs(2f*x/63f-1f);
            edgeHaze.SetPixel(x,0,new Color(1f,1f,1f,Mathf.Pow(edge,1.65f)));
        }
        edgeHaze.Apply();
        for (int i=0;i<portraits.Length;i++)
            portraits[i] = new Texture2D(PortraitSize,PortraitSize,TextureFormat.RGBA32,false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        CreateAudio();
        CreateTensionAudio();
        CreateControls();
        SetControls();
    }
    void OnEnable()
    {
        cabinet?.Enable();
        secondCabinet?.Enable();
        InputSystem.onDeviceChange += OnCabinetDeviceChange;
        RefreshCabinets();
        if (screen == FrontScreen.Playing && stage != Stage.Reveal) StartPortraitCamera();
    }
    void OnDisable() { InputSystem.onDeviceChange -= OnCabinetDeviceChange; StopPortraitCamera(); cabinet?.Disable(); secondCabinet?.Disable(); verticalRepeat = horizontalRepeat = default; }
    void OnDestroy() { DestroyTensionAudio(); Destroy(corridorDisplay); Destroy(edgeHaze); foreach (var portrait in portraits) Destroy(portrait); cabinet?.Dispose(); secondCabinet?.Dispose(); Destroy(controlSettings); Destroy(geometry); Destroy(ink); }
    void Restart()
    {
        elapsed = readyAt = timer = 0;
        ResetSecondPlayer();
        historyPage = 0;
        nextGain = 1;
        ResetTension();
        verticalRepeat = horizontalRepeat = default;
        effects.Stop();
        transform.localPosition = cameraRest;
        transform.localRotation = cameraRestRotation;
        if (!music.isPlaying) music.Play();
        StartPortraitCamera();
        score = scoreChange = 0;
        corridorMood = .5f;
        swayTime = 0;
        ClearShotHistory();
        powerTenths = 10;
        stage=Stage.Tune; depth=CorridorEnd; gain=1; amp=.5f; phase=0; frequency=1; verticalOffset=0;
        GenerateTarget();
        System.Array.Clear(sum,0,N);
        System.Array.Copy(original,residual,N);
        amplitudeControl.SetValueWithoutNotify(amp);
        cyclesControl.SetValueWithoutNotify(frequency);
        phaseControl.SetValueWithoutNotify(phase);
        offsetControl.SetValueWithoutNotify(verticalOffset);
        ScoreRun();
        SetControls();
        Preview();
    }
    void GenerateTarget()
    {
        for (int i = 0; i < N; i++) original[i] = TargetOffset;
        // Disjoint frequency bands keep a broad contour with two layers of detail.
        AddTargetComponent(12, 17, 15, 30);
        AddTargetComponent(5, 9, 35, 60);
        AddTargetComponent(2, 4, 65, 110);
    }

    void ClearShotHistory()
    {
        shots.Clear();
        shotCycles.Clear();
        shotPowers.Clear();
        shotColors.Clear();
    }

    void AddTargetComponent(int minAmplitudeSteps, int maxAmplitudeSteps, int minCycleSteps, int maxCycleSteps)
    {
        float amplitude = Random.Range(minAmplitudeSteps, maxAmplitudeSteps + 1) * AmplitudeStep;
        float cycles = Random.Range(minCycleSteps, maxCycleSteps + 1) * CycleStep;
        float degrees = Random.Range(-36, 36) * PhaseStep;
        for (int i = 0; i < N; i++)
            original[i] += Harmonic(i, amplitude, cycles, degrees);
    }

    static void Nudge(Slider slider, bool increase, bool decrease, float step)
    {
        if (increase == decrease) return;
        slider.value = Mathf.Clamp(slider.value + (increase ? step : -step), slider.lowValue, slider.highValue);
    }

    void Preview()
    {
        for(int i=0;i<N;i++) wave[i]=Harmonic(i,amp,frequency,phase)+verticalOffset;
    }
    void Update()
    {
        if (screen != FrontScreen.Playing)
        {
            UpdateFrontScreen();
            return;
        }
        if (stage == Stage.Reveal)
        {
            if (Time.frameCount != openedFrame && AnyFrontScreenPress())
                OpenFrontScreen(FrontScreen.Title);
            return;
        }
        if (SupportsPortraitCamera && Keyboard.current?.f8Key.wasPressedThisFrame == true)
        {
            cameraEnabled = !cameraEnabled;
            if (cameraEnabled && stage!=Stage.Reveal) StartPortraitCamera();
            else StopPortraitCamera();
        }
        float delta = Time.deltaTime;
        if (AdvanceDetailZoom(delta))
        {
            UpdateTension(delta);
            return;
        }
        Tick(delta);
        UpdateTension(delta);
        UpdateSecondPlayer();
        if (elapsed >= LevelDuration || elapsed < readyAt)
        {
            verticalRepeat = horizontalRepeat = default;
            return;
        }
        ReadWaveControls(elapsed, WaveSetting.All);
        var keys = Keyboard.current;
        var pad = cabinet.Arcade;
        bool powerUp = pad.ButtonC.WasPressedThisFrame() || keys?.iKey.wasPressedThisFrame == true;
        bool powerDown = pad.ButtonF.WasPressedThisFrame() || keys?.kKey.wasPressedThisFrame == true;
        powerTenths = Mathf.Max(0, powerTenths + (powerUp ? 1 : 0) - (powerDown ? 1 : 0));
        if (pad.ButtonExit.WasPressedThisFrame() || keys?.spaceKey.wasPressedThisFrame == true) TryLaunch();
    }

    void ReadWaveControls(float now, WaveSetting setting)
    {
        var keys = Keyboard.current;
        var pad = cabinet.Arcade;
        Vector2 stick = pad.Move.ReadValue<Vector2>();
        int vertical = verticalRepeat.Steps(stick.y >= .5f || keys?.wKey.isPressed == true,
            stick.y <= -.5f || keys?.sKey.isPressed == true,now);
        int horizontal = horizontalRepeat.Steps(stick.x <= -.5f || keys?.aKey.isPressed == true,
            stick.x >= .5f || keys?.dKey.isPressed == true,now);
        if (vertical != 0 && (setting == WaveSetting.All || setting == WaveSetting.Offset))
            offsetControl.value = Mathf.Clamp(offsetControl.value+vertical*OffsetStep,offsetControl.lowValue,offsetControl.highValue);
        if (horizontal != 0 && (setting == WaveSetting.All || setting == WaveSetting.Phase))
            phaseControl.value = Mathf.Repeat(phase+horizontal*PhaseStep+180f,360f)-180f;
        if (setting == WaveSetting.All || setting == WaveSetting.Frequency)
            Nudge(cyclesControl, pad.ButtonD.WasPressedThisFrame() || keys?.hKey.wasPressedThisFrame == true,
                pad.ButtonA.WasPressedThisFrame() || keys?.yKey.wasPressedThisFrame == true, CycleStep);
        if (setting == WaveSetting.All || setting == WaveSetting.Amplitude)
            Nudge(amplitudeControl, pad.ButtonB.WasPressedThisFrame() || keys?.uKey.wasPressedThisFrame == true,
                pad.ButtonE.WasPressedThisFrame() || keys?.jKey.wasPressedThisFrame == true, AmplitudeStep);
    }

    static float ShotVolume(float power)
    {
        return .13f + .87f * (1f - Mathf.Exp(-.55f*power));
    }

    void TryLaunch()
    {
        if (stage != Stage.Tune || elapsed >= LevelDuration || elapsed < readyAt || zoomAge>=0) return;
        System.Array.Copy(wave, fired, N);
        firedCycles = frequency;
        firedPower = powerTenths / 10f;
        readyAt = elapsed + LaunchCooldown;
        effects.PlayOneShot(launchSound,ShotVolume(firedPower));
        Advance(Stage.Flight);
    }

    void Tick(float delta)
    {
        if (stage == Stage.Reveal) return;
        AdvanceSecondShot(delta);
        elapsed = Mathf.Min(LevelDuration,elapsed + delta);
        depth = CorridorEnd - FrameSpacing * (elapsed / SecondsPerBlock);
        timer += delta;
        if (zoomLead<0) gain = Mathf.Lerp(gain,nextGain,1-Mathf.Exp(-delta*2));
        launch.SetEnabled(stage == Stage.Tune && elapsed >= readyAt && elapsed < LevelDuration);
        if(stage==Stage.Flight && timer>=FlightDuration)
        {
            hitQuality = ResolveShot(fired,firedCycles,firedPower,waveGreen);
            Advance(Stage.Impact);
        }
        else if(stage==Stage.Impact && timer>=1.1f)
        {
            Advance(Stage.Tune);
        }
        if (elapsed >= LevelDuration && stage == Stage.Tune && secondFlight < 0 && secondImpact < 0) Advance(Stage.Reveal);
    }
    float ResolveShot(float[] samples,float cycles,float power,Color shotColor)
    {
        shots.Add((float[])samples.Clone());
        shotCycles.Add(cycles);
        shotPowers.Add(power);
        shotColors.Add(shotColor);
        float peak=0;
        for(int i=0;i<N;i++)
        {
            sum[i]+=samples[i];
            residual[i]=original[i]-sum[i];
            peak=Mathf.Max(peak,Mathf.Abs(residual[i]));
        }
        nextGain=Mathf.Clamp(1.35f/Mathf.Max(.001f,peak),.25f,8);
        QueueDetailZoom();
        float before = 0, after = 0;
        for (int i=0; i<N; i++)
        {
            float previous = residual[i]+samples[i];
            before += previous*previous;
            after += residual[i]*residual[i];
        }
        float quality = before > .00001f ? Mathf.Clamp01(1f-Mathf.Sqrt(after/before)) : 0f;
        effects.PlayOneShot(clashSound,Mathf.Min(1f,1.12f*ShotVolume(power)));
        int previousScore = score;
        ScoreRun();
        scoreChange = score-previousScore;
        return quality;
    }

    void Advance(Stage next)
    {
        stage = next;
        timer = 0;
        if (next == Stage.Reveal)
        {
            openedFrame = Time.frameCount;
            music.Stop();
            hiss.Stop();
            StopPortraitCamera();
        }
        SetControls();
    }
    void Line(Vector3 a,Vector3 b,Color c)
    {
        lines.Add(vertices.Count); vertices.Add(a); colors.Add(c);
        lines.Add(vertices.Count); vertices.Add(b); colors.Add(c);
    }
    Vector3 Point(int i,float y,float z) => new Vector3(Mathf.Lerp(-Span,Span,i/(N-1f)),y,z);
    static float LaunchRecoil(float age,float power)
    {
        if (age<0 || age>=.28f) return 0;
        return Mathf.Sin(age/.28f*Mathf.PI)*Mathf.Exp(-age*10f)*(.10f+.23f*power/(1f+power));
    }

    void LateUpdate()
    {
        if (screen != FrontScreen.Playing) return;
        float recoil = LaunchRecoil(stage==Stage.Flight ? timer : -1f,firedPower)
            + LaunchRecoil(secondFlight,secondFiredPower);
        // Most runs land between 30% and 50%. Use that range for the visible transition.
        float scoreFraction = (float)score/MaxScore;
        float moodTarget = shots.Count == 0 ? .5f
            : scoreFraction <= .5f
                ? Mathf.Lerp(0f,.8f,Mathf.InverseLerp(.25f,.5f,scoreFraction))
                : Mathf.Lerp(.8f,1f,Mathf.InverseLerp(.5f,.7f,scoreFraction));
        corridorMood = Mathf.Lerp(corridorMood,moodTarget,1f-Mathf.Exp(-Time.deltaTime*2.5f));
        viewCamera.backgroundColor = Color.Lerp(new Color(.009f,.003f,.009f),
            new Color(.065f,.115f,.14f),corridorMood);
        float distress = Mathf.Pow(1f-corridorMood,1.55f);
        swayTime += Time.deltaTime*Mathf.Lerp(1.05f,2.05f,distress);
        float horizontal = Mathf.Sin(swayTime*2.1f)*.72f
            + (Mathf.PerlinNoise(2.4f,swayTime)*2f-1f)*.55f;
        float vertical = Mathf.Sin(swayTime*1.6f+1f)*.68f
            + (Mathf.PerlinNoise(8.7f,swayTime)*2f-1f)*.55f;
        float roll = Mathf.Sin(swayTime*1.15f);
        float pan = stage==Stage.Reveal ? 0f : Mathf.Lerp(.65f,3.15f,distress)*(2f/3f);
        transform.localPosition = cameraRest + cameraRestRotation *
            new Vector3(horizontal*pan*.012f,vertical*pan*.012f,-recoil);
        transform.localRotation = cameraRestRotation *
            Quaternion.Euler(-vertical*pan*.82f-recoil*11f,horizontal*pan,roll*pan*.12f);
        geometry.Clear();
        if (stage == Stage.Reveal) return;
        vertices.Clear(); colors.Clear(); lines.Clear(); faces.Clear();
        for(int frame=0; frame<FrameCount; frame++)
        {
            float z = FrontFrame + frame * FrameSpacing;
            Vector3 a=new Vector3(-4,-2,z), b=new Vector3(4,-2,z), c=new Vector3(4,2,z), d=new Vector3(-4,2,z);
            float instability = Mathf.PerlinNoise(frame*.29f,Time.time*2.1f);
            float rhythm = .5f+.5f*Mathf.Sin(Time.time*3f-frame*.32f);
            float pulse = Mathf.Lerp(instability,rhythm,corridorMood);
            Color frameColor = Color.Lerp(LowMood,HighMood,corridorMood)
                * Mathf.Lerp(.07f+.93f*Mathf.Pow(pulse,2f),1f,corridorMood);
            frameColor.a = 1f;
            if ((stage==Stage.Impact || secondImpact>=0) && scoreChange!=0)
            {
                float progress = Mathf.Clamp01((secondImpact>=0 ? secondImpact : timer)/.8f);
                float head = scoreChange>0 ? Mathf.Lerp(depth,FrontFrame,progress)
                    : Mathf.Lerp(FrontFrame,depth,progress);
                float band = Mathf.Clamp01(1f-Mathf.Abs(z-head)/(FrameSpacing*1.5f))*(1f-progress);
                frameColor = Color.Lerp(frameColor,scoreChange>0 ? Color.white : new Color(.035f,.002f,.006f),band);
            }
            Line(a,b,frameColor); Line(b,c,frameColor); Line(c,d,frameColor); Line(d,a,frameColor);
        }
        for(int x=-4;x<=4;x+=2) Line(new Vector3(x,-2,FrontFrame),new Vector3(x,-2,CorridorEnd),Color.Lerp(LowMood*.22f,grid,corridorMood));
        float scale=gain;
        float offset=stage==Stage.Impact ? 1-Mathf.SmoothStep(0,1,timer/1.1f) : 0;
        for(int i=1;i<N;i++)
        {
            Vector3 a=Point(i-1,offset*fired[i-1]*scale,depth), b=Point(i,offset*fired[i]*scale,depth);
            Vector3 c=b+Vector3.up*residual[i]*scale, d=a+Vector3.up*residual[i-1]*scale;
            float tear = stage==Stage.Impact ? Mathf.Clamp01(1f-timer/.65f) : 0f;
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            Color fill = new Color(targetRed.r,targetRed.g,targetRed.b,.28f);
            Color seam = Color.Lerp(fill, new Color(1f,.49f,.15f,.72f), tear);
            colors.Add(seam); colors.Add(seam); colors.Add(fill); colors.Add(fill);
            faces.Add(start); faces.Add(start+1); faces.Add(start+2);
            faces.Add(start+2); faces.Add(start+3); faces.Add(start);

        }
        Line(Point(0,0,depth),Point(N-1,0,depth),Color.gray);
        DrawBlade(residual,scale,offset,depth,targetRed,0);
        float flight=stage==Stage.Flight ? Mathf.Lerp(FrontFrame,depth,timer/FlightDuration) : depth;
        if(elapsed < LevelDuration)
        {
            DrawProjection(scale,wave,powerTenths,waveGreen);
            if (secondJoined) DrawProjection(scale,secondWave,secondPower,waveBlue);
        }
        if(stage==Stage.Flight || stage==Stage.Impact)
            DrawBlade(fired,stage==Stage.Impact ? scale*offset : scale,0,flight,waveGreen,17,firedPower);
        if (stage == Stage.Flight) DrawTrail(flight,scale,fired,firedPower,waveGreen);
        if (stage == Stage.Impact) DrawImpact(scale,fired,firedPower,timer,hitQuality,waveGreen);
        if (secondFlight >= 0)
        {
            float secondDepth = Mathf.Lerp(FrontFrame,depth,secondFlight/FlightDuration);
            DrawBlade(secondFired,scale,0,secondDepth,waveBlue,29,secondFiredPower);
            DrawTrail(secondDepth,scale,secondFired,secondFiredPower,waveBlue);
        }
        if (secondImpact >= 0) DrawImpact(scale,secondFired,secondFiredPower,secondImpact,secondHitQuality,waveBlue);
        geometry.SetVertices(vertices);
        geometry.SetColors(colors);
        geometry.subMeshCount = 2;
        geometry.SetIndices(faces, MeshTopology.Triangles, 0);
        geometry.SetIndices(lines, MeshTopology.Lines, 1);
    }
    void DrawProjection(float scale,float[] profile,int powerLevel,Color color)
    {
        float power = powerLevel / 10f;
        float charge = Mathf.Log(1f + power,2f);
        float light = 1f-Mathf.Exp(-.8f*power);
        float pixel = 2f*depth*Mathf.Tan(.5f*viewCamera.fieldOfView*Mathf.Deg2Rad)/Mathf.Max(1,viewCamera.pixelHeight);
        float halo = (1f+4.2f*charge)*pixel;
        float separation = halo+(1f+1.25f*charge)*pixel;
        Color rim = color;
        rim.a = .08f+.92f*light;
        Color core = Color.Lerp(color,Color.white,light);
        core.a = .24f+.76f*light;
        for (int side=-1;side<=1;side+=2)
        {
            int first = vertices.Count;
            for(int i=0;i<N;i++)
            {
                int lo=Mathf.Max(0,i-1), hi=Mathf.Min(N-1,i+1);
                Vector3 normal=new Vector3((profile[lo]-profile[hi])*scale,(hi-lo)*2*Span/(N-1),0).normalized;
                Vector3 rail=Point(i,profile[i]*scale,depth)+normal*(side*separation);
                GlowSection(rail,normal*halo,rim,core);
            }
            RibbonFaces(first);
        }
    }

    void GlowSection(Vector3 center,Vector3 normal,Color glow,Color core)
    {
        Color edge=new Color(glow.r,glow.g,glow.b,0);
        vertices.Add(center-normal); colors.Add(edge);
        vertices.Add(center-normal*.32f); colors.Add(glow);
        vertices.Add(center-normal*.095f); colors.Add(core);
        vertices.Add(center+normal*.095f); colors.Add(core);
        vertices.Add(center+normal*.32f); colors.Add(glow);
        vertices.Add(center+normal); colors.Add(edge);
    }

    void DrawBlade(float[] profile,float scale,float waveOffset,float z,Color glow,int seed,float power=-1)
    {
        int first = vertices.Count;
        Color core = Color.white;
        int sparks = 28;
        float radius = (.125f + .009f*Mathf.Sin(Time.time*5f+seed)) * Mathf.Sqrt(z/10f);
        if (power>=0)
        {
            float light=power/(1f+power);
            radius *= .28f+.85f*Mathf.Log(1f+power,2f);
            glow.a = .18f+.82f*light;
            core = Color.Lerp(glow,Color.white,Mathf.Sqrt(light));
            core.a = .45f+.55f*light;
            sparks = Mathf.RoundToInt(4+52*light);
        }
        for (int i=0;i<N;i++)
        {
            Vector3 center = Point(i,(profile[i]+waveOffset*fired[i])*scale,z);
            int lo=Mathf.Max(0,i-1), hi=Mathf.Min(N-1,i+1);
            float dy=(profile[hi]-profile[lo]+waveOffset*(fired[hi]-fired[lo]))*scale;
            Vector3 normal=new Vector3(-dy,(hi-lo)*2*Span/(N-1),0).normalized*radius;
            GlowSection(center,normal,glow,core);
        }
        RibbonFaces(first);
        for (int n=0;n<sparks;n++)
        {
            float clock=Time.time*(.8f+(n%5)*.12f)+n*.618f;
            float age=Mathf.Repeat(clock,1f);
            int sample=(n*67+seed+Mathf.FloorToInt(clock)*13)%N;
            Vector3 origin=Point(sample,(profile[sample]+waveOffset*fired[sample])*scale,z);
            float angle=n*2.4f+seed;
            Vector3 drift=new Vector3(Mathf.Cos(angle)*.35f,Mathf.Sin(angle)*.46f,-.16f);
            Color spark=glow;
            spark.a*=Mathf.Sin(age*Mathf.PI);
            Line(origin+drift*age,origin+drift*(age+.085f),spark);
        }
    }

    void DrawTrail(float front,float scale,float[] samples,float power,Color color)
    {
        for (int echo = 1; echo <= 4; echo++)
        {
            float z = front - echo * .36f;
            if (z < FrontFrame) continue;
            Color tint = new Color(color.r,color.g,color.b,(.12f+.85f*power/(1+power))/echo);
            for (int i = 2; i < N; i += 2)
                Line(Point(i-2, samples[i-2]*scale, z), Point(i, samples[i]*scale, z), tint);
        }
    }

    void RibbonFaces(int first)
    {
        for (int i=1; i<N; i++)
        {
            int left = first+6*(i-1);
            for (int band=0; band<5; band++)
            {
                int a = left+band;
                faces.Add(a); faces.Add(a+6); faces.Add(a+1);
                faces.Add(a+1); faces.Add(a+6); faces.Add(a+7);
            }
        }
    }

    void DrawImpact(float scale,float[] samples,float power,float impactTime,float quality,Color color)
    {
        float charge = power / (1f + power);
        float force = 1f + .65f * charge;
        int sparkCount = 160 + Mathf.RoundToInt(96 * charge);
        for (int n = 0; n < sparkCount; n++)
        {
            float life = .24f + .78f * ((n*13)%43)/42f;
            if (impactTime >= life) continue;
            int sample = (n*67 + shots.Count*13)%N;
            Vector3 origin = Point(sample,samples[sample]*scale,depth);
            float angle = n*2.39996f + shots.Count;
            Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), -.12f-(n%6)*.17f)
                * (3.2f + (n%11)*.68f) * force;
            velocity.y += Mathf.Sign(residual[sample]) * 3f * force;
            // Drag takes the edge off the initial burst as the sparks cool.
            float travel = impactTime/(1f+1.8f*impactTime);
            Vector3 head = origin + velocity*travel + Vector3.down*(1.75f*impactTime*impactTime);
            if (head.z < .4f) continue;
            Vector3 tail = head - velocity*(.1f + .04f*charge);
            tail.z = Mathf.Max(.4f,tail.z);
            Color spark = Color.Lerp(new Color(1f,.83f,.35f),
                Color.HSVToRGB((n*.618034f)%1f,.86f,1f), Mathf.Clamp01(impactTime*7f));
            spark.a = 1f-impactTime/life;
            Line(tail,head,spark);
            Color tip = new Color(1f,.95f,.82f,spark.a);
            Line(head,head+Vector3.up*.035f*force,tip);
            Line(head,head+Vector3.right*.035f*force,tip);
        }

        const float flashDuration = .22f;
        if (impactTime < flashDuration)
        {
            float t = impactTime/flashDuration;
            Color hot = new Color(1f,.56f,.16f, .92f*(1f-t));
            Color white = new Color(1f,.97f,.84f,1f-t);
            int first = vertices.Count;
            for (int i=0; i<N; i++)
                GlowSection(Point(i,samples[i]*scale,depth), Vector3.up*((.26f+.6f*t)*force),hot,white);
            RibbonFaces(first);
        }

        // The contact highlight sweeps across matching sections of the two curves.
        for (int i=1;i<N;i++)
        {
            float arrival = .19f*i/(N-1);
            float age = impactTime-arrival;
            float gap = Mathf.Abs(residual[i]);
            if (age>=0 && age<.14f && gap<.17f)
            {
                Color contact = new Color(1f,1f,1f,(1f-age/.14f)*(1f-gap/.17f));
                Vector3 point = Point(i,samples[i]*scale,depth);
                Line(point-Vector3.up*.11f,point+Vector3.up*.11f,contact);
            }
            if (quality>.45f && impactTime>.12f && impactTime<.65f)
            {
                Color ghost = new Color(.8f,.93f,1f,quality*(.65f-impactTime)/.53f);
                if (impactTime<.38f || i%5<3)
                    Line(Point(i-1,samples[i-1]*scale,depth-.15f),Point(i,samples[i]*scale,depth-.15f),ghost);
            }
        }

        float pulseDepth = depth-18f*impactTime;
        for (int frame=0;frame<FrameCount;frame++)
        {
            float frameDepth = FrontFrame+frame*FrameSpacing;
            if (frameDepth>depth) continue;
            float intensity = Mathf.Clamp01(1f-Mathf.Abs(frameDepth-pulseDepth)/1.25f);
            if (intensity<=0) continue;
            Color pulse = new Color(1f,.67f,.3f,intensity*.9f);
            Vector3 a=new Vector3(-4,-2,frameDepth), b=new Vector3(4,-2,frameDepth);
            Vector3 c=new Vector3(4,2,frameDepth), d=new Vector3(-4,2,frameDepth);
            Line(a,b,pulse); Line(b,c,pulse); Line(c,d,pulse); Line(d,a,pulse);
        }

        const int ringSegments = 56;
        for (int ring=0; ring<2; ring++)
        {
            float age = impactTime-ring*.09f;
            if (age<0 || age>=.68f) continue;
            float t = age/.68f;
            float radius = (.22f+4.8f*(1f-(1f-t)*(1f-t)))*force;
            float z = Mathf.Max(.65f,depth-5.5f*t);
            Color ringColor = ring==0 ? new Color(1f,.76f,.28f) : color;
            ringColor.a = (1f-t)*(1f-t)*.85f;
            Vector3 last = new Vector3(radius,0,z);
            for (int i=1;i<=ringSegments;i++)
            {
                float angle = i*(2f*Mathf.PI/ringSegments);
                Vector3 point = new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius*.56f,z);
                Line(last,point,ringColor);
                last = point;
            }
        }
    }

    void Plot(float[] values, Color color,Rect area,float gain=1,float thickness=2)
    {
        if (Event.current.type != EventType.Repaint) return;
        Matrix4x4 panel = GUI.matrix;
        Color previousColor = GUI.color;
        GUI.color = color;
        float step = area.width / (values.Length - 1);
        float verticalScale = area.height * .32f * gain;
        for (int i = 1; i < values.Length; i++)
        {
            float x = area.x + (i - 1) * step;
            float y = area.center.y - values[i - 1] * verticalScale;
            float dy = (values[i - 1] - values[i]) * verticalScale;
            float angle = Mathf.Atan2(dy, step) * Mathf.Rad2Deg;
            // Position each segment in panel coordinates before scaling the whole panel.
            GUI.matrix = panel * Matrix4x4.TRS(new Vector3(x, y, 0),
                Quaternion.Euler(0, 0, angle), Vector3.one);
            GUI.DrawTexture(new Rect(0, -thickness / 2, Mathf.Sqrt(step * step + dy * dy) + .4f,
                thickness), Texture2D.whiteTexture);
        }
        GUI.matrix = panel;
        GUI.color = previousColor;
    }
    void ScoreRun()
    {
        float initialEnergy = 0;
        foreach (float v in original) initialEnergy += v*v;
        float initialRms = Mathf.Sqrt(initialEnergy/N);
        float previous = initialRms, effort = 0;
        float improved = 0, distinctImprovements = 0, totalWeight = 0, weightedReduction = 0;
        var used = new HashSet<float>();
        var remaining = (float[])original.Clone();
        for (int n = 0; n < shots.Count; n++)
        {
            float energy = 0, shotEnergy = 0;
            for (int i = 0; i < N; i++)
            {
                float value = shots[n][i];
                remaining[i] -= value;
                energy += remaining[i]*remaining[i];
                shotEnergy += value*value;
            }
            float current = Mathf.Sqrt(energy/N);
            float weight = shotPowers[n];
            totalWeight += weight;
            weightedReduction += weight * (previous - current);
            bool fresh = used.Add(shotCycles[n]);
            if (current < previous - .00001f)
            {
                improved += weight;
                if (fresh) distinctImprovements += weight;
            }
            effort += weight * Mathf.Sqrt(shotEnergy/N);
            previous = current;
        }
        finalRms = previous;
        float reduction = Mathf.Max(0, initialRms-finalRms);
        accuracy = Mathf.Clamp01(reduction/initialRms);
        efficiency = effort > 0 ? Mathf.Clamp01(weightedReduction/effort) : 0;
        elegance = totalWeight > 0 ? distinctImprovements/totalWeight : 0;
        consistency = totalWeight > 0 ? improved/totalWeight : 0;
        score = Mathf.RoundToInt(1000*accuracy+300*efficiency+200*elegance);
    }

    static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    void DrawReveal()
    {
        const float width = 800, height = 480;
        float scale = Mathf.Min(Screen.width/width, Screen.height/height);
        var previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-width*scale)/2,
            (Screen.height-height*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
        Fill(new Rect(0,0,width,height), new Color(.045f,.065f,.078f));
        Fill(new Rect(389,12,1,456), grid);
        Fill(new Rect(408,294,364,1), grid);
        GUI.Label(new Rect(20,12,360,24),"USED HARMONICS");
        GUI.Label(new Rect(408,12,370,24),"INITIAL TARGET  /  FINAL SUM");
        for (int i=historyPage*10; i<Mathf.Min(shots.Count,(historyPage+1)*10); i++)
        {
            float top = 38 + (i%10)*43;
            GUI.Label(new Rect(16,top+5,88,24), $"{i+1:00}   {shotCycles[i]:0.##} cyc");
            Rect trace = new Rect(104,top,266,40);
            Fill(new Rect(trace.x,trace.center.y,trace.width,.6f),grid);
            Plot(shots[i],shotColors[i],trace,.60f,1.4f);
        }
        Fill(new Rect(408,50,12,3),targetRed);
        Fill(new Rect(518,50,12,3),waveGreen);
        GUI.Label(new Rect(534,40,160,24),"Final sum");
        GUI.Label(new Rect(424,40,94,24),"Initial target");
        float peak=1.5f;
        for(int i=0;i<N;i++) peak=Mathf.Max(peak,Mathf.Abs(sum[i]),Mathf.Abs(residual[i]));
        float gainForPlot=1.5f/peak;
        Plot(original,targetRed,new Rect(408,70,364,124),gainForPlot,4);
        Plot(sum,waveGreen,new Rect(408,70,364,124),gainForPlot);
        GUI.Label(new Rect(408,201,370,24), $"Final residual    ·    RMS {finalRms:F3}");
        Plot(residual,targetRed,new Rect(408,225,364,62),gainForPlot);
        GUI.Label(new Rect(408,300,372,28), $"FINAL FIT     {score} / {MaxScore}");
        GUI.Label(new Rect(408,329,372,24), $"Accuracy    {accuracy:P0}    ·    Error removed");
        GUI.Label(new Rect(408,351,372,24), $"Efficiency    {efficiency:P0}    ·    Reduction per effort");
        GUI.Label(new Rect(408,373,372,24), $"Elegance    {elegance:P0}    ·    Useful new frequencies");
        GUI.Label(new Rect(408,395,372,24), $"Consistency    {consistency:P0}    ·    Improving shots");

        GUI.matrix=previous;
    }

    void OnGUI()
    {
        if (screen != FrontScreen.Playing)
        {
            DrawFrontScreen();
            return;
        }
        if(stage==Stage.Reveal)
        {
            DrawReveal();
            return;
        }
        if (Event.current.type==EventType.Repaint)
        {
            Color previous = GUI.color;
            GUI.color = new Color(.72f,.012f,.055f,.92f*Mathf.Pow(1f-corridorMood,1.3f));
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),edgeHaze);
            GUI.color = previous;
        }
        if (Event.current.type == EventType.Repaint)
        {
            DrawPortrait(0);
            if (secondJoined) DrawPortrait(1);
        }
    }

    void PortraitBlock(int x,int y,int width,int height,Color color)
    {
        Color32 pixel = color;
        for (int row=y;row<y+height;row++)
            for (int column=x;column<x+width;column++)
                portraitPixels[(PortraitSize-1-row)*PortraitSize+column] = pixel;
    }

    void StartPortraitCamera()
    {
        if (!SupportsPortraitCamera || !cameraEnabled || livePortrait != null || Application.isBatchMode) return;
        livePortrait = new LivePortraitCamera();
        livePortrait.Start();
    }

    void StopPortraitCamera()
    {
        livePortrait?.Dispose();
        livePortrait = null;
    }

    void DrawPortrait(int player)
    {
        int frame = Mathf.FloorToInt(Time.time*12f)+player*13;
        float strain = 1f-corridorMood;
        bool blink = frame%49==0 || frame%49==1;
        int flutterPeriod = Mathf.RoundToInt(Mathf.Lerp(43f,19f,strain));
        int flutterFrame = (frame+11)%flutterPeriod;
        bool nervousBlink = flutterFrame==0 || (strain>.55f && flutterFrame==3);
        float size = Mathf.Clamp(Mathf.Floor(Screen.height/175f),2,6)*PortraitSize;
        Rect area = new Rect(player==0 ? 16 : Screen.width-size-16,16,size,size);
        DrawAlarmLamp(player,area);
        if (livePortrait != null && livePortrait.Draw(player,area,shots.Count==0 ? 0f : strain,
            blink || nervousBlink,player==0 ? stage==Stage.Impact && timer<.18f : secondImpact>=0 && secondImpact<.18f)) return;
        if (frame!=portraitFrames[player])
        {
            portraitFrames[player] = frame;
            bool bluePlayer = player == 1;
            int twitch = strain>.55f && frame%17<3 ? 1 : 0;
            int jaw = (player==0 ? stage==Stage.Impact : secondImpact>=0) || (strain>.7f && frame%12<4) ? 1 : 0;
            Color dark = new Color(.12f,.075f,.09f);
            Color skin = Color.Lerp(new Color(.88f,.71f,.59f),new Color(.66f,.42f,.39f),strain);
            Color shadow = Color.Lerp(new Color(.38f,.23f,.18f),new Color(.29f,.10f,.14f),strain);
            Color light = Color.Lerp(new Color(1f,.88f,.75f),new Color(.88f,.60f,.53f),strain);
            Color alert = Color.Lerp(new Color(.11f,.26f,.27f),new Color(.47f,.065f,.09f),strain);
            Color hair = bluePlayer ? new Color(.35f,.16f,.09f) : dark;
            if (bluePlayer)
            {
                skin = Color.Lerp(new Color(.72f,.51f,.36f),new Color(.55f,.28f,.26f),strain);
                shadow = Color.Lerp(new Color(.42f,.27f,.20f),new Color(.32f,.13f,.14f),strain);
                light = Color.Lerp(new Color(.92f,.73f,.51f),new Color(.74f,.47f,.39f),strain);
                alert = Color.Lerp(new Color(.13f,.32f,.58f),alert,strain*.8f);
            }
            PortraitBlock(0,0,48,48,dark);
            PortraitBlock(1,1,46,46,alert);
            PortraitBlock(3,3,42,42,new Color(.045f,.065f,.083f));
            PortraitBlock(6,42,36,4,new Color(.19f,.28f,.32f));
            PortraitBlock(14,37,20,8,shadow);
            PortraitBlock(9,8,30,25,dark);
            PortraitBlock(7,21,34,8,shadow);
            for(int y=10;y<40;y++)
            {
                int inset = bluePlayer ? 1+Mathf.Max(0,(y-27)/2) : y<14 ? 2 : Mathf.Max(0,(y-30)/2);
                PortraitBlock(10+inset,y,28-2*inset,1,skin);
                PortraitBlock(11+inset,y,4,1,shadow);
                PortraitBlock(32-inset,y,4,1,shadow);
            }
            if (bluePlayer)
            {
                PortraitBlock(12,7,22,6,hair);
                PortraitBlock(10,10,10,5,hair);
                PortraitBlock(10,14,5,7,hair);
                PortraitBlock(15,12,6,5,hair);
                PortraitBlock(21,11,8,4,hair);
                PortraitBlock(17,8,13,2,new Color(.59f,.31f,.15f));
                PortraitBlock(32,10,5,7,hair);
            }
            else
            {
                PortraitBlock(12,9,25,4,dark);
                PortraitBlock(16,7,17,4,dark);
                PortraitBlock(12,13,4,6,dark);
            }
            PortraitBlock(17,14,13,4,light);
            PortraitBlock(22,21,4,10,light);
            PortraitBlock(26,26,2,5,shadow);
            PortraitBlock(20,30,8,2,shadow);
            for(int eye=0;eye<2;eye++)
            {
                int x=14+eye*12;
                int y=20+(eye==1 ? twitch : 0)+(bluePlayer && eye==0 ? 1 : 0);
                PortraitBlock(x,y-2,8,2,dark);
                PortraitBlock(x+(eye==0 ? 4 : 0),y-1,4,2,dark);
                // The portrait faces the player, so its left eye is on screen right.
                bool eyeClosed = blink || (eye==1 && nervousBlink);
                PortraitBlock(x,y+1,8,eyeClosed ? 1 : 5,eyeClosed ? dark : new Color(.96f,.85f-.32f*strain,.73f-.28f*strain));
                if(!eyeClosed) PortraitBlock(x+3,y+2,2,3,dark);
                if(strain>.55f) PortraitBlock(x+1,y+6,6,2,shadow);
            }
            int mouthY = bluePlayer ? 35 : 34;
            PortraitBlock(17,mouthY,14,2+jaw,dark);
            if (bluePlayer)
            {
                PortraitBlock(15,mouthY+1,3,2,shadow);
                PortraitBlock(28,mouthY-2,4,2,dark);
            }
            PortraitBlock(19,mouthY,10,1+jaw,new Color(.85f,.75f,.60f));
            PortraitBlock(20,38+jaw,8,1,light);
            float injury = shots.Count == 0 ? 0f : strain;
            if (injury>.22f)
            {
                Color blood = new Color(.57f,.045f,.075f);
                PortraitBlock(34,17,2,4,blood);
                PortraitBlock(35,21,1,3,blood);
                PortraitBlock(31,29,4,2,blood);
            }
            if (injury>.48f)
            {
                Color blood = new Color(.46f,.025f,.067f);
                PortraitBlock(15,15,3,2,blood);
                PortraitBlock(14,16,2,5,blood);
                PortraitBlock(13,25,3,6,blood);
                PortraitBlock(14,30,2,4,blood);
                PortraitBlock(30,33,4,3,blood);
            }
            if(injury>.72f)
            {
                Color flush = new Color(.56f,.14f,.16f);
                PortraitBlock(12,27,5,3,flush);
                PortraitBlock(31,26,4,4,flush);
                PortraitBlock(19,17,2,1,shadow);
                Color smear = new Color(.38f,.02f,.045f);
                PortraitBlock(26,30,2,4,smear);
                PortraitBlock(17,37,5,2,smear);
                PortraitBlock(27,38,4,2,smear);
                PortraitBlock(29,39,2,3,smear);
                PortraitBlock(35,24,3,5,smear);
            }
            portraits[player].SetPixels32(portraitPixels);
            portraits[player].Apply();
        }

        GUI.DrawTexture(area,portraits[player]);
    }

    void CreateControls()
    {
        controlSettings = ScriptableObject.CreateInstance<PanelSettings>();
        controlSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("CorridorTheme");
        var document = gameObject.AddComponent<UIDocument>();
        document.panelSettings = controlSettings;
        var root = document.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        // WASD belongs to waveform tuning even after a mouse click focuses a slider.
        root.RegisterCallback<NavigationMoveEvent>(evt => {
            var keys = Keyboard.current;
            if (elapsed < LevelDuration && (cabinet.Arcade.Move.ReadValue<Vector2>().sqrMagnitude > .1f || keys != null &&
                (keys.wKey.isPressed || keys.sKey.isPressed || keys.aKey.isPressed || keys.dKey.isPressed)))
                evt.StopPropagation();
        }, TrickleDown.TrickleDown);
        root.RegisterCallback<NavigationSubmitEvent>(evt => {
            var keys = Keyboard.current;
            var firstPower = cabinet.Arcade.ButtonC;
            var secondPowerButton = secondCabinet.Arcade.ButtonC;
            bool secondLaunch = keys?.enterKey.isPressed == true || keys?.enterKey.wasReleasedThisFrame == true
                || keys?.numpadEnterKey.isPressed == true || keys?.numpadEnterKey.wasReleasedThisFrame == true;
            if (elapsed < LevelDuration && (secondLaunch || firstPower.IsPressed() || firstPower.WasReleasedThisFrame()
                || secondPowerButton.IsPressed() || secondPowerButton.WasReleasedThisFrame()))
                evt.StopPropagation();
        }, TrickleDown.TrickleDown);
        root.style.alignItems = Align.Center;
        tuning = new VisualElement();
        tuning.style.position = Position.Absolute;
        tuning.style.bottom = 12;
        root.Add(tuning);
        amplitudeControl = AddSlider("Amplitude",0,1.5f,AmplitudeStep, v => amp=v);
        cyclesControl = AddSlider("Cycles",.05f,6,CycleStep, v => frequency=v);
        phaseControl = AddSlider("Phase (degrees)",-180,180,PhaseStep, v => phase=v);
        offsetControl = AddSlider("Vertical offset",-1,1,OffsetStep, v => verticalOffset=v);
        launch = new Button(TryLaunch) { text="Launch harmonic (Space)" };
        tuning.Add(launch);
        replay = new Button(() => OpenFrontScreen(FrontScreen.Title)) { text="Any key to return" };
        replay.style.position = Position.Absolute;
        root.Add(replay);
        history = new Button(() => historyPage = (historyPage+1) % ((shots.Count+9)/10))
            { text="Next 10" };
        history.style.position = Position.Absolute;
        root.Add(history);
        root.RegisterCallback<GeometryChangedEvent>(e => {
            float scale = Mathf.Min(e.newRect.width/800f,e.newRect.height/480f);
            history.style.left = (e.newRect.width-800*scale)/2 + 270*scale;
            history.style.top = (e.newRect.height-480*scale)/2 + 6*scale;
            history.style.width = 100*scale;
            replay.style.left = (e.newRect.width-800*scale)/2 + 408*scale;
            replay.style.bottom = (e.newRect.height-480*scale)/2 + 20*scale;
            replay.style.width = 200*scale;
            replay.style.height = 32*scale;
        });
    }

    Slider AddSlider(string title,float min,float max,float step,System.Action<float> apply)
    {
        var slider = new Slider(title,min,max);
        slider.labelElement.style.color = Color.white;
        slider.style.display = DisplayStyle.None;
        tuning.Add(slider);
        slider.RegisterValueChangedCallback(e => {
            float value = Mathf.Clamp(Mathf.Round(e.newValue/step)*step,min,max);
            slider.SetValueWithoutNotify(value);
            apply(value);
            slider.label = $"{title}   {value:0.##}";
            Preview();
        });
        return slider;
    }

    void SetControls()
    {
        history.style.display = screen==FrontScreen.Playing && stage==Stage.Reveal && shots.Count>10 ? DisplayStyle.Flex : DisplayStyle.None;
        launch.SetEnabled(stage==Stage.Tune && elapsed>=readyAt && elapsed<LevelDuration);
        tuning.style.display = screen==FrontScreen.Playing && stage!=Stage.Reveal && elapsed<LevelDuration ? DisplayStyle.Flex : DisplayStyle.None;
        replay.style.display = screen==FrontScreen.Playing && stage==Stage.Reveal ? DisplayStyle.Flex : DisplayStyle.None;
        if(stage==Stage.Tune)
        {
            amplitudeControl.label = $"Amplitude   {amp:0.##}";
            cyclesControl.label = $"Cycles   {frequency:0.##}";
            offsetControl.label = $"Vertical offset   {verticalOffset:0.00}";
            phaseControl.label = $"Phase (degrees)   {phase:0.##}";
        }
    }
}
