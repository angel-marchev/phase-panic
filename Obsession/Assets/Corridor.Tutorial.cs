using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public partial class Corridor
{
    enum WaveSetting { Amplitude, Frequency, Phase, Offset, All }

    sealed class WaveLesson
    {
        public readonly WaveSetting setting;
        public readonly string title, instruction, controls;
        public readonly float amplitude, cycles, phase, offset, start;

        public WaveLesson(WaveSetting setting, string title, string instruction, string controls,
            float amplitude, float cycles, float phase, float offset, float start)
        {
            this.setting = setting;
            this.title = title;
            this.instruction = instruction;
            this.controls = controls;
            this.amplitude = amplitude;
            this.cycles = cycles;
            this.phase = phase;
            this.offset = offset;
            this.start = start;
        }

        public float Sample(int i) => Harmonic(i, amplitude, cycles, phase) + offset;
    }

    static readonly WaveLesson[] lessons = {
        new WaveLesson(WaveSetting.Amplitude, "Amplitude", "Make the hills taller until their tops touch the red hills.",
            "U taller / J shorter     Pad B / E", .85f, 1f, 0, 0, .45f),
        new WaveLesson(WaveSetting.Frequency, "Frequency", "Fit more hills across the same space. Match their spacing.",
            "H more cycles / Y fewer     Pad D / A", .50f, 2f, 0, 0, 1.6f),
        new WaveLesson(WaveSetting.Phase, "Phase", "The hills have the right shape. Slide them sideways to line them up.",
            "A left / D right     Stick left / right", .30f, 3f, -45f, 0, 0),
        new WaveLesson(WaveSetting.Offset, "Vertical offset", "Lift the whole line. The hills and valleys move up together.",
            "W up / S down     Stick up / down", .16f, 4f, 0, .16f, 0)
    };

    int lessonIndex, reportPage;
    bool lessonCleared, tutorialReport, needsAdjustment;
    readonly float[] lessonTarget = new float[N], beforePracticeShot = new float[N];
    WaveLesson Lesson => lessons[lessonIndex];

    void BeginTutorial()
    {
        ClearShotHistory();
        historyPage = reportPage = lessonIndex = 0;
        tutorialReport = false;
        System.Array.Clear(sum, 0, N);
        for (int i = 0; i < N; i++)
        {
            // Leave a quiet fifth component in the report so the residual is visible.
            original[i] = Harmonic(i, .08f, 5f, 0);
            foreach (var lesson in lessons) original[i] += lesson.Sample(i);
        }
        System.Array.Copy(original, residual, N);
        BeginLesson();
    }

    Slider LessonControl()
    {
        switch (Lesson.setting)
        {
            case WaveSetting.Amplitude: return amplitudeControl;
            case WaveSetting.Frequency: return cyclesControl;
            case WaveSetting.Phase: return phaseControl;
            default: return offsetControl;
        }
    }

    void BeginLesson()
    {
        lessonCleared = needsAdjustment = false;
        verticalRepeat = horizontalRepeat = default;
        amp = Lesson.amplitude;
        frequency = Lesson.cycles;
        phase = Lesson.phase;
        verticalOffset = Lesson.offset;
        amplitudeControl.SetValueWithoutNotify(amp);
        cyclesControl.SetValueWithoutNotify(frequency);
        phaseControl.SetValueWithoutNotify(phase);
        offsetControl.SetValueWithoutNotify(verticalOffset);
        LessonControl().value = Lesson.start;
        for (int i = 0; i < N; i++) lessonTarget[i] = Lesson.Sample(i);
        Preview();
    }

    void UpdateTutorial()
    {
        var keys = Keyboard.current;
        var pad = cabinet.Arcade;
        if (keys?.escapeKey.wasPressedThisFrame == true || pad.ButtonStart.WasPressedThisFrame())
        {
            OpenFrontScreen(FrontScreen.Title);
            return;
        }
        if (!tutorialReport && !lessonCleared) ReadWaveControls(Time.unscaledTime, Lesson.setting);
        if (keys?.spaceKey.wasPressedThisFrame == true || pad.ButtonExit.WasPressedThisFrame())
            TakeTutorialAction();
    }

    void TakeTutorialAction()
    {
        if (tutorialReport)
        {
            if (++reportPage == reportNotes.Length) OpenFrontScreen(FrontScreen.Title);
            return;
        }
        if (lessonCleared)
        {
            if (lessonIndex == lessons.Length-1) tutorialReport = true;
            else { lessonIndex++; BeginLesson(); }
            return;
        }
        // Check the trace itself so equivalent cyclic positions count as a match.
        if (!LessonMatches())
        {
            needsAdjustment = true;
            return;
        }
        System.Array.Copy(residual, beforePracticeShot, N);
        ResolveShot(wave, frequency, 1f, waveGreen);
        lessonCleared = true;
    }

    static readonly string[] reportNotes = {
        "Each row above is one shot, drawn at the same\nscale. Small ripples stay small. On the right,\ngreen adds your four shots together. Where\ngreen covers red, you rebuilt the glitch.",
        "The red residual is what got through. RMS\ntells you how much is left. Smaller is better.\nHere you can still see a faint fifth ripple.\nIn a live run, aim for that next.",
        "Accuracy rewards removing the glitch.\nEfficiency rewards doing it with less effort.\nElegance rewards helpful new frequencies.\nThese three make up Final Fit, out of 1500.",
        "Consistency shows how often shots helped.\nPower gives a shot more weight in the ratings.\nIn a live run, find a big shape first, fire,\nthen look for the next ripple in what remains."
    };

    void DrawTutorial()
    {
        if (tutorialReport)
        {
            MenuText(new Rect(18,222,363,30),"READING YOUR RESULTS",18,menuCyan);
            MenuText(new Rect(18,258,367,106),reportNotes[reportPage],13,menuWhite);
            TutorialFooter(reportPage == reportNotes.Length-1 ? "FINISH" : "NEXT");
            return;
        }
        MenuText(new Rect(34,25,730,27),$"TRAINING   {lessonIndex+1} / {lessons.Length}",16,menuCyan);
        MenuText(new Rect(34,52,730,44),Lesson.title,31,menuWhite);
        MenuText(new Rect(34,101,738,56),lessonCleared
            ? "That layer is gone. The bright line is what your next shot can quiet."
            : "We picked out one layer of the glitch for you.\n" + Lesson.instruction,16,menuWhite);
        var chart = new Rect(42,167,716,143);
        Fill(chart,new Color(.045f,.075f,.11f));
        Fill(new Rect(chart.x,chart.center.y,chart.width,1),grid);
        if (lessonCleared)
        {
            Plot(beforePracticeShot, new Color(.48f,.54f,.59f), chart,.65f,2);
            Plot(residual, targetRed, chart,.65f,3);
            MenuText(new Rect(42,318,720,29),"GRAY   Before the shot       RED   What remains",15,menuWhite);
            MenuText(new Rect(42,348,730,60),"Finding the simple waves inside a complicated one is harmonic decomposition.\nIn the corridor, the red line always shows what your shots have left behind.",15,menuCyan);
        }
        else
        {
            float scale = lessonIndex < 2 ? .9f : 1.4f;
            Plot(lessonTarget, targetRed, chart,scale,5);
            Plot(wave, waveGreen, chart,scale,2);
            MenuText(new Rect(42,314,730,27),"RED   One layer of the glitch       GREEN   Your shot",15,menuWhite);
            MenuText(new Rect(42,351,530,29),Lesson.controls,16,menuCyan);
            MenuText(new Rect(42,380,730,27),LessonMatches() ? "The two traces line up. Fire when ready." : needsAdjustment ? TutorialHint() :
                "Only this control is active. Take as long as you need.",14,menuWhite);
            var slider = LessonControl();
            float step = Lesson.setting == WaveSetting.Offset ? OffsetStep : Lesson.setting == WaveSetting.Phase ? PhaseStep :
                Lesson.setting == WaveSetting.Amplitude ? AmplitudeStep : CycleStep;
            int direction = 0;
            if (MenuButton(new Rect(594,347,72,35),"-",false)) direction = -1;
            if (MenuButton(new Rect(676,347,72,35),"+",false)) direction = 1;
            if (direction != 0)
            {
                if (Lesson.setting == WaveSetting.Phase)
                    slider.value = Mathf.Repeat(phase + direction*step + 180f,360f)-180f;
                else Nudge(slider,direction > 0,direction < 0,step);
            }
        }
        TutorialFooter(lessonCleared ? (lessonIndex == lessons.Length-1 ? "VIEW RESULTS" : "NEXT LAYER") : "FIRE");
    }

    bool LessonMatches()
    {
        for (int i = 0; i < N; i++)
            if (Mathf.Abs(lessonTarget[i]-wave[i]) > .005f) return false;
        return true;
    }

    string TutorialHint()
    {
        switch (Lesson.setting)
        {
            case WaveSetting.Amplitude: return amp < Lesson.amplitude ? "A little taller. Tap U or pad B." : "Too tall. Tap J or pad E.";
            case WaveSetting.Frequency: return frequency < Lesson.cycles ? "Squeeze in more cycles. Tap H or pad D." : "Spread the hills out. Tap Y or pad A.";
            case WaveSetting.Phase: return Mathf.DeltaAngle(phase, Lesson.phase) > 0 ? "Slide left a little. Use A or the stick." : "Slide right a little. Use D or the stick.";
            default: return verticalOffset < Lesson.offset ? "Lift the whole line a little. Use W or the stick." : "Lower the whole line a little. Use S or the stick.";
        }
    }

    void TutorialFooter(string action)
    {
        if (MenuButton(new Rect(28,422,238,38),action,false)) TakeTutorialAction();
        MenuText(new Rect(277,424,246,34),"Space / Pad Select",15,menuWhite);
        if (MenuButton(new Rect(532,422,244,38),"EXIT  Esc / Start",false)) OpenFrontScreen(FrontScreen.Title);
    }
}
