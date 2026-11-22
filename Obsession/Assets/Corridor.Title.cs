using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public partial class Corridor
{
    enum FrontScreen { Title, Settings, Help, Intro, Playing, Tutorial }
    FrontScreen screen;
    int selectedMenu, selectedSetting, openedFrame;
    int menuDirection;
    bool allowSecondPlayer = true;
    Font titleFont;
    Texture2D introComic;
    float introStarted;
    int introPanel;
    const float GlitchAppears = 6f, PulseFires = 12f, ScopeAppears = 18f, IntroEnds = 26f;
    static readonly string[] comicCaptions = {
        "We fix code from the inside now.\nI'm halfway through my build when I see it.",
        "A red glitch slips through the far wall.\nIt's coming straight at me.",
        "I fire. The glare dies. For one second, quiet.\nThen I hear the hiss again.",
        "A red scratch. Still coming. I zoom in and fire again.\nEvery shot leaves a trace. Quiet it before it reaches me."
    };

    static readonly Color menuInk = new Color(.025f,.04f,.075f);
    static readonly Color menuCyan = new Color(.31f,1f,.82f);
    static readonly Color menuPink = new Color(1f,.3f,.43f);
    static readonly Color menuWhite = new Color(.87f,.91f,1f);
    static readonly string[] titleItems = { "START", "SETTINGS", "HELP", "INTRO", "TUTORIAL" };
    static readonly string[] pixelLetters = {
        "111101101111100100100", "101101101111101101101", "010101101111101101101",
        "111100100111001001111", "111100100110100100111", "101111111111111111101",
        "111010010010010010111", "111100100100100100111"
    };
    const string PixelAlphabet = "PHASENIC";

    void OpenFrontScreen(FrontScreen next)
    {
        if (screen == FrontScreen.Intro)
        {
            hiss.Stop();
            effects.Stop();
        }
        screen = next;
        openedFrame = Time.frameCount;
        menuDirection = 0;
        if (next == FrontScreen.Title && !music.isPlaying) music.Play();
        if (next == FrontScreen.Tutorial) BeginTutorial();
        if (next == FrontScreen.Settings) selectedSetting = 0;
        if (next == FrontScreen.Intro)
        {
            if (introComic == null) introComic = Resources.Load<Texture2D>("QuietTheGlitch");
            introStarted = Time.unscaledTime;
            introPanel = 0;
        }
        if (next == FrontScreen.Playing)
        {
            secondJoined = false;
            Restart();
        }
        else SetControls();
    }

    void ChooseMenu(int item)
    {
        selectedMenu = item;
        switch (item)
        {
            case 0: OpenFrontScreen(FrontScreen.Playing); break;
            case 1: OpenFrontScreen(FrontScreen.Settings); break;
            case 2: OpenFrontScreen(FrontScreen.Help); break;
            case 3: OpenFrontScreen(FrontScreen.Intro); break;
            case 4: OpenFrontScreen(FrontScreen.Tutorial); break;
        }
    }

    void ChooseSetting(int item)
    {
        if (item==0) cameraEnabled = SupportsPortraitCamera && !cameraEnabled;
        else if (item==1) allowSecondPlayer = !allowSecondPlayer;
        else OpenFrontScreen(FrontScreen.Title);
    }

    void UpdateFrontScreen()
    {
        if (Time.frameCount == openedFrame) return;
        var keys = Keyboard.current;
        var pad = cabinet.Arcade;
        if (screen == FrontScreen.Tutorial)
        {
            UpdateTutorial();
            return;
        }
        if (screen==FrontScreen.Help || screen==FrontScreen.Intro)
        {
            if (AnyFrontScreenPress())
                OpenFrontScreen(FrontScreen.Title);
            else if (screen == FrontScreen.Intro)
                UpdateIntro(Time.unscaledTime - introStarted);
            return;
        }

        if (screen==FrontScreen.Settings && (keys?.escapeKey.wasPressedThisFrame == true || pad.ButtonExit.WasPressedThisFrame()))
        {
            OpenFrontScreen(FrontScreen.Title);
            return;
        }
        float stick = pad.Move.ReadValue<Vector2>().y;
        int direction = stick>.5f ? -1 : stick<-.5f ? 1 : 0;
        int move = direction!=menuDirection ? direction : 0;
        menuDirection = direction;
        if (keys?.upArrowKey.wasPressedThisFrame == true || keys?.wKey.wasPressedThisFrame == true) move = -1;
        if (keys?.downArrowKey.wasPressedThisFrame == true || keys?.sKey.wasPressedThisFrame == true) move = 1;
        bool confirm = keys?.enterKey.wasPressedThisFrame == true || keys?.numpadEnterKey.wasPressedThisFrame == true
            || keys?.spaceKey.wasPressedThisFrame == true || pad.ButtonC.WasPressedThisFrame();
        if (screen==FrontScreen.Title)
        {
            selectedMenu = (selectedMenu+move+titleItems.Length)%titleItems.Length;
            if (confirm) ChooseMenu(selectedMenu);
        }
        else
        {
            selectedSetting = (selectedSetting+move+3)%3;
            if (confirm) ChooseSetting(selectedSetting);
        }
    }

    bool AnyFrontScreenPress() => AnyButtonPress(Keyboard.current) || AnyButtonPress(firstDevice) || AnyButtonPress(secondDevice);

    static bool AnyButtonPress(InputDevice device)
    {
        if (device == null) return false;
        foreach (var control in device.allControls)
            if (control is ButtonControl button && button.wasPressedThisFrame) return true;
        return false;
    }

    void DrawFrontScreen()
    {
        Fill(new Rect(0,0,Screen.width,Screen.height),menuInk);
        if (titleFont==null)
            titleFont = Resources.Load<Font>("Fonts/DejaVuSansMono");
        if (screen == FrontScreen.Intro)
        {
            DrawIntro();
            return;
        }
        if (screen == FrontScreen.Tutorial && tutorialReport) DrawReveal();
        float scale = Mathf.Min(Screen.width/800f,Screen.height/480f);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-800*scale)/2,(Screen.height-480*scale)/2,0),
            Quaternion.identity,new Vector3(scale,scale,1));
        if (screen == FrontScreen.Tutorial) DrawTutorial();
        else
        {
            Fill(new Rect(18,18,764,444),new Color(.12f,.23f,.36f));
            Fill(new Rect(22,22,756,436),menuInk);
            if (screen==FrontScreen.Title) DrawTitle();
            else if (screen==FrontScreen.Help) DrawHelp();
            else DrawSettings();
        }
        GUI.matrix = previous;
    }

    void UpdateIntro(float age)
    {
        if (age >= IntroEnds)
        {
            OpenFrontScreen(FrontScreen.Playing);
            return;
        }
        int panel = age < GlitchAppears ? 0 : age < PulseFires ? 1 : age < ScopeAppears ? 2 : 3;
        if (panel == introPanel) return;
        introPanel = panel;
        if (panel == 2) effects.PlayOneShot(launchSound, .45f);
        if (panel == 3)
        {
            hiss.volume = .026f;
            hiss.Play();
        }
    }

    void DrawIntro()
    {
        if (introComic == null) return;
        float age = Time.unscaledTime - introStarted;
        float aspect = (float)introComic.width / introComic.height;
        float width = Mathf.Min(Screen.width, Screen.height * aspect);
        float height = width / aspect;
        var frame = new Rect((Screen.width-width)/2f, (Screen.height-height)/2f, width, height);
        float size = .5f;
        if (introPanel == 3)
            size /= Mathf.SmoothStep(1f, 2.6f, Mathf.InverseLerp(ScopeAppears + .65f, IntroEnds, age));
        float centerX = introPanel % 2 == 0 ? .25f : .75f;
        float centerY = introPanel < 2 ? .75f : .25f;
        GUI.DrawTextureWithTexCoords(frame, introComic,
            new Rect(centerX-size/2f, centerY-size/2f, size, size));
        // A paper caption inside the panel leaves the terminal and the scope in view.
        float unit = width / 768f;
        var captionStyle = new GUIStyle(GUI.skin.label) {
            font = titleFont, fontSize = Mathf.Max(1, Mathf.RoundToInt(18f * unit)),
            fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleLeft
        };
        captionStyle.normal.textColor = new Color(.09f,.075f,.065f);
        string words = comicCaptions[introPanel];
        float captionWidth = Mathf.Min(width-36*unit, captionStyle.CalcSize(new GUIContent(words)).x+26*unit);
        float captionHeight = captionStyle.CalcHeight(new GUIContent(words),captionWidth-20*unit)+14*unit;
        var box = new Rect(frame.x+16*unit, frame.y+16*unit, captionWidth, captionHeight);
        Fill(new Rect(box.x+4*unit,box.y+4*unit,box.width,box.height),Color.black);
        Fill(box, new Color(.92f,.87f,.73f));
        GUI.Label(new Rect(box.x+10*unit,box.y+6*unit,box.width-20*unit,box.height-12*unit),words,captionStyle);
        if (introPanel == 2)
            Fill(frame, new Color(0, 0, 0, Mathf.InverseLerp(ScopeAppears-.35f, ScopeAppears, age)));
        if (GUI.Button(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, GUIStyle.none))
            OpenFrontScreen(FrontScreen.Title);
    }

    void DrawTitle()
    {
        DrawPixelLogo(59,71,7);
        DrawTitleGraphic();
        for (int i=0;i<titleItems.Length;i++)
        {
            int item = i;
            if (MenuButton(new Rect(483,76+item*64,241,48),titleItems[item],item==selectedMenu))
            {
                ChooseMenu(item);
                return;
            }
        }
        MenuText(new Rect(57,429,686,24),"↑ ↓  SELECT     ENTER / PAD C  CONFIRM     P1 START  QUIT",14,menuWhite);
    }

    void DrawPixelLogo(int x,int y,int pixel)
    {
        string[] words = { "PHASE", "PANIC" };
        for(int line=0;line<words.Length;line++)
        {
            for(int i=0;i<words[line].Length;i++)
            {
                string glyph = pixelLetters[PixelAlphabet.IndexOf(words[line][i])];
                for(int row=0;row<7;row++)
                    for(int col=0;col<3;col++)
                        if (glyph[row*3+col]=='1')
                            Fill(new Rect(x+(i*4+col)*pixel,y+(line*9+row)*pixel,pixel,pixel),line==0 ? menuCyan : menuPink);
            }
        }
    }

    void DrawTitleGraphic()
    {
        Fill(new Rect(55,226,374,183),new Color(.045f,.075f,.14f));
        for(int frame=0;frame<6;frame++)
        {
            int inset = frame*13;
            Color ink = Color.Lerp(new Color(.24f,.43f,.59f),new Color(.12f,.21f,.33f),frame/5f);
            int x=57+inset, y=228+inset, w=370-inset*2, h=179-inset*2;
            Fill(new Rect(x,y,w,3),ink); Fill(new Rect(x,y+h-3,w,3),ink);
            Fill(new Rect(x,y,3,h),ink); Fill(new Rect(x+w-3,y,3,h),ink);
        }
        for(int i=0;i<79;i++)
        {
            int red = Mathf.RoundToInt(9*Mathf.Sin(i*.16f)+4*Mathf.Sin(i*.37f));
            int green = Mathf.RoundToInt(7*Mathf.Sin(i*.16f+.55f));
            Fill(new Rect(82+i*4,313+red*3,4,5),menuPink);
            Fill(new Rect(82+i*4,313+green*3,4,4),menuCyan);
        }
        for(int spark=0;spark<13;spark++)
        {
            float angle = spark*2.39996f;
            float distance = 12+spark*2;
            Fill(new Rect(253+Mathf.Round(Mathf.Cos(angle)*distance),
                314+Mathf.Round(Mathf.Sin(angle)*distance),3,3),menuWhite);
        }
    }

    void DrawHelp()
    {
        MenuText(new Rect(52,47,700,46),"CONTROLS",32,menuCyan);
        MenuText(new Rect(52,109,700,26),"ACTION                PLAYER 1             PLAYER 2",16,menuPink);
        string[] rows = {
            "Move wave             WASD                 Arrow keys",
            "Amplitude             U / J                PgUp / PgDn",
            "Frequency             Y / H                [ / ]",
            "Power                 I / K                + / -",
            "Launch                Space                Shift",
            "Start buttons         P1 quits the game. P2 joins (or Enter).",
            "Cabinet               Stick to move. A/D frequency.",
            "                      B/E amplitude. C/F power. Select fires.",
            SupportsPortraitCamera ? "Camera                F8 toggles live face portraits." : "Portraits             Animated arcade faces in the browser."
        };
        for(int i=0;i<rows.Length;i++)
            MenuText(new Rect(52,145+i*27,700,24),rows[i],15,menuWhite);
        if (MenuButton(new Rect(52,407,250,34),"ANY KEY TO RETURN",false)) OpenFrontScreen(FrontScreen.Title);
    }

    void DrawSettings()
    {
        MenuText(new Rect(52,56,700,50),"SETTINGS",32,menuCyan);
        string[] values = { !SupportsPortraitCamera ? "PORTRAITS  PIXEL" : cameraEnabled ? "CAMERA  ON" : "CAMERA  OFF",
            allowSecondPlayer ? "PLAYER 2  ON" : "PLAYER 2  OFF", "BACK" };
        for(int i=0;i<values.Length;i++)
            if(MenuButton(new Rect(52,143+i*70,330,50),values[i],selectedSetting==i))
            {
                selectedSetting = i;
                ChooseSetting(i);
                return;
            }
    }

    void MenuText(Rect area,string value,int size,Color color)
    {
        var style = new GUIStyle(GUI.skin.label) { font=titleFont, fontSize=size, alignment=TextAnchor.MiddleLeft };
        style.normal.textColor = color;
        GUI.Label(area,value,style);
    }

    bool MenuButton(Rect area,string value,bool selected)
    {
        Color edge = selected ? menuCyan : new Color(.25f,.36f,.53f);
        Fill(area,edge);
        Fill(new Rect(area.x+3,area.y+3,area.width-6,area.height-6),menuInk);
        MenuText(new Rect(area.x+13,area.y,area.width-20,area.height),
            (selected ? ">  " : "   ")+value,17,selected ? menuCyan : menuWhite);
        return GUI.Button(area,GUIContent.none,GUIStyle.none);
    }
}
