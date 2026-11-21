using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class Corridor
{
    static readonly Color waveBlue = new Color(.12f,.48f,1f);
    ArcadeControls secondCabinet;
    InputDevice firstDevice, secondDevice;
    bool secondJoined;
    DirectionRepeat secondVertical, secondHorizontal;
    readonly float[] secondWave = new float[N], secondFired = new float[N];
    float secondAmplitude, secondFrequency, secondPhase, secondOffset;
    float secondReadyAt, secondFlight, secondImpact, secondFiredCycles, secondFiredPower, secondHitQuality;
    int secondPower;

    void ResetSecondPlayer()
    {
        secondAmplitude = .5f;
        secondFrequency = 1f;
        secondPhase = secondOffset = secondReadyAt = 0f;
        secondFlight = secondImpact = -1f;
        secondPower = 10;
        secondVertical = secondHorizontal = default;
        PreviewSecond();
    }

    void OnCabinetDeviceChange(InputDevice device,InputDeviceChange change)
    {
        if ((device is Gamepad || device is Joystick) &&
            (change == InputDeviceChange.Added || change == InputDeviceChange.Removed))
            RefreshCabinets();
    }

    void RefreshCabinets()
    {
        InputDevice first = firstDevice != null && firstDevice.added ? firstDevice : null;
        InputDevice second = secondDevice != null && secondDevice.added ? secondDevice : null;
        foreach (var device in InputSystem.devices)
        {
            if (!(device is Gamepad) && !(device is Joystick)) continue;
            if (device == first || device == second) continue;
            if (first == null) first = device;
            else if (second == null) second = device;
        }
        if (first != firstDevice || !cabinet.asset.devices.HasValue)
        {
            firstDevice = first;
            cabinet.asset.devices = new ReadOnlyArray<InputDevice>(first == null
                ? System.Array.Empty<InputDevice>() : new[] { first });
        }
        if (second != secondDevice || !secondCabinet.asset.devices.HasValue)
        {
            secondDevice = second;
            secondCabinet.asset.devices = new ReadOnlyArray<InputDevice>(second == null
                ? System.Array.Empty<InputDevice>() : new[] { second });
        }
    }

    void UpdateSecondPlayer()
    {
        if (!allowSecondPlayer || elapsed >= LevelDuration || stage == Stage.Reveal) return;
        var keys = Keyboard.current;
        var pad = secondCabinet.Arcade;
        bool enter = keys?.enterKey.wasPressedThisFrame == true
            || keys?.numpadEnterKey.wasPressedThisFrame == true;
        if (!secondJoined)
        {
            secondJoined = pad.ButtonStart.WasPressedThisFrame() || enter;
            return;
        }
        Vector2 stick = pad.Move.ReadValue<Vector2>();
        bool up = stick.y >= .5f || keys?.upArrowKey.isPressed == true;
        bool down = stick.y <= -.5f || keys?.downArrowKey.isPressed == true;
        bool left = stick.x <= -.5f || keys?.leftArrowKey.isPressed == true;
        bool right = stick.x >= .5f || keys?.rightArrowKey.isPressed == true;
        bool ampUp = pad.ButtonB.WasPressedThisFrame() || keys?.pageUpKey.wasPressedThisFrame == true;
        bool ampDown = pad.ButtonE.WasPressedThisFrame() || keys?.pageDownKey.wasPressedThisFrame == true;
        bool freqUp = pad.ButtonD.WasPressedThisFrame() || keys?.rightBracketKey.wasPressedThisFrame == true;
        bool freqDown = pad.ButtonA.WasPressedThisFrame() || keys?.leftBracketKey.wasPressedThisFrame == true;
        bool powerUp = pad.ButtonC.WasPressedThisFrame() || keys?.equalsKey.wasPressedThisFrame == true
            || keys?.numpadPlusKey.wasPressedThisFrame == true;
        bool powerDown = pad.ButtonF.WasPressedThisFrame() || keys?.minusKey.wasPressedThisFrame == true
            || keys?.numpadMinusKey.wasPressedThisFrame == true;
        bool fire = pad.ButtonExit.WasPressedThisFrame() || keys?.leftShiftKey.wasPressedThisFrame == true
            || keys?.rightShiftKey.wasPressedThisFrame == true;
        if (elapsed < secondReadyAt)
        {
            secondVertical = secondHorizontal = default;
            return;
        }
        int vertical = secondVertical.Steps(up,down,elapsed);
        int horizontal = secondHorizontal.Steps(left,right,elapsed);
        secondOffset = Mathf.Clamp(secondOffset+vertical*OffsetStep,-1f,1f);
        secondPhase = Mathf.Repeat(secondPhase+horizontal*PhaseStep+180f,360f)-180f;
        secondAmplitude = Mathf.Clamp(secondAmplitude+Step(ampUp,ampDown)*AmplitudeStep,0f,1.5f);
        secondFrequency = Mathf.Clamp(Mathf.Round((secondFrequency+Step(freqUp,freqDown)*CycleStep)/CycleStep)*CycleStep,.05f,6f);
        secondPower = Mathf.Max(0,secondPower+Step(powerUp,powerDown));
        if (vertical!=0 || horizontal!=0 || ampUp || ampDown || freqUp || freqDown) PreviewSecond();
        if (fire)
        {
            System.Array.Copy(secondWave,secondFired,N);
            secondFiredCycles = secondFrequency;
            secondFiredPower = secondPower/10f;
            secondReadyAt = elapsed + LaunchCooldown;
            secondFlight = 0f;
            effects.PlayOneShot(launchSound,ShotVolume(secondFiredPower));
        }
    }

    static int Step(bool up,bool down) => (up ? 1 : 0)-(down ? 1 : 0);

    void PreviewSecond()
    {
        for (int i=0;i<N;i++) secondWave[i] = Harmonic(i,secondAmplitude,secondFrequency,secondPhase)+secondOffset;
    }

    void AdvanceSecondShot(float delta)
    {
        if (secondImpact >= 0)
        {
            secondImpact += delta;
            if (secondImpact >= 1.1f) secondImpact = -1f;
        }
        if (secondFlight < 0) return;
        secondFlight += delta;
        if (secondFlight < FlightDuration) return;
        secondFlight = -1f;
        secondImpact = 0f;
        secondHitQuality = ResolveShot(secondFired,secondFiredCycles,secondFiredPower,waveBlue);
    }
}
