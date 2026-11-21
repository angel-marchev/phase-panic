# PHASE PANIC — Unity prototype handoff

Snapshot of the playable demo on 21 November 2026. Open the `Obsession/` directory as the Unity project. Use the current behavior below as the baseline for future changes. The appendix reproduces the original exploratory design in full, including alternatives and earlier terminology.

## Keeping this handoff current

The public GitHub repository is [angel-marchev/phase-panic](https://github.com/angel-marchev/phase-panic). The owner authorized the visibility change for GitHub Pages after GitHub declined to host Pages from the private repository on its current plan. Open `Obsession/` as the Unity project. The repository tracks the game assets, their Unity metadata, package manifests, project settings, MIDI source, design documents, and the static browser build in `docs/`. The unpacked Linux player is a local build artifact.

Whenever you change the project, update this handoff in the same task. Revise the affected sections to describe the current behavior, the reason for the change, and what was verified. Preserve the original design reference verbatim in the appendix.

Commit each completed project update together with its handoff change, push it to `main` on the GitHub repository, and verify that the commit reached GitHub before reporting completion.

## Run the demo

1. Open `Obsession/` in Unity Hub with Unity **6000.6.5f1**. Allow package import to finish. If Unity requests a restart after a graphics or input backend change, reopen the Editor.
2. Press **Play** and open **Game**. An untitled Scene view before Play is expected. The runtime bootstrap finds an existing corridor or configures a camera and attaches the demo. Ordinary Play works from an empty scene.
3. Alternatively, **Obsession → Play corridor** creates or opens `Assets/Corridor.unity` and enters Play mode.
4. Choose **Start** on the PHASE PANIC title screen. Tune the green waveform against the red silhouette. Press **Space** or click **Launch harmonic (Space)** to commit it. When the target reaches the front, inspect the decomposition and press any key or cabinet button to return to the splash screen.

## Browser release

Play at [Phase Panic on GitHub Pages](https://angel-marchev.github.io/phase-panic/). The packaged site is approximately 18 MiB. Chrome loaded it from an ordinary local HTTP server under a project subpath, initialized WebGL 2, resumed audio after a click, accepted a keyboard-tuned tutorial shot, displayed the illustrated intro, and rendered the playable corridor with its pixel portrait. Unity's release report counted three warnings.

The Unity Web build lives in `docs/`. Serve this directory over HTTP or HTTPS. For local play, run `python3 -m http.server 8000 --bind 127.0.0.1 --directory docs` from the workspace root and open [localhost:8000](http://localhost:8000/). Click inside the game to activate audio and keyboard input. The browser version uses the animated pixel portraits because the desktop camera tracker requires a local Python/OpenCV process.

For ordinary static hosting, upload the entire contents of `docs/` together. Asset URLs are relative and the loader includes gzip decompression fallback for servers that serve those files without a content-encoding header. For GitHub Pages, use `main` and `/docs` as the branch publishing source. The build includes `.nojekyll` and the font license.

To rebuild, use Unity 6000.6.5f1 with Web Build Support installed. From the repository root, invoke the Unity executable with `-batchmode -quit -projectPath "$PWD/Obsession" -buildTarget WebGL -executeMethod WebRelease.Build`. This creates an empty startup scene for the runtime bootstrap and writes the release into `docs/`.

## Linux standalone release

Run `./PhasePanic-Linux-x86_64/PhasePanic.x86_64` from the workspace root. Copy the whole `PhasePanic-Linux-x86_64` directory when moving the game to another Linux x86-64 machine. It contains the executable, game data, Mono runtime, native libraries, and bundled font license. Live face capture uses the existing external Python/OpenCV worker when available.

The 99 MiB release was built with Unity 6000.6.5f1, the Linux x86-64 Mono player, LZ4HC compression, and an empty startup scene that invokes the game's existing runtime bootstrap. Unity reported zero build errors and warnings. On Ubuntu the packaged player rendered the title menu through Vulkan on Intel Iris Xe. A separate smoke test ran with Mesa llvmpipe software OpenGL, opened Tutorial, returned to the splash screen, started the corridor, and sent the launch input. For software rendering on Mesa, use `LIBGL_ALWAYS_SOFTWARE=1 ./PhasePanic-Linux-x86_64/PhasePanic.x86_64 -force-glcore`.

## Title screen and options

PHASE PANIC starts on an 8-bit title screen with an original pixel logo and wave clash illustration. Choose Start, Settings, Help, Intro, or Tutorial with the mouse, Up/Down and Enter, or the first pad’s stick and Start. Start begins a fresh run immediately. The industrial synth loop starts on the splash screen and plays continuously through the comic, training, and entry into gameplay. The real results screen goes quiet and returning to the title starts the loop again. Settings toggles the live camera and Player 2 joining for the current session. Help shows both keyboard layouts, the cabinet controls, how to join Player 2, and F8 for the camera. Any keyboard key or cabinet button returns from Help, Intro, or the results screen. The results screen also has a clickable return button. Menu text uses the bundled DejaVu Sans Mono font and its license under Assets/Resources/Fonts.

### Quiet the Glitch

Choose **Intro** to watch the 26-second dark sci-fi comic. First-person narration appears on paper-colored caption boxes inside each panel. A programmer’s wrist terminal reads “BUILD PASSED” as a red pinprick enters the virtual corridor. It grows into a jagged scar. The programmer fires, the impact fades to black, and the scope picks up a surviving hairline signal. A faint hiss accompanies the zoom into that detail. The final caption sets the objective of quieting what remains before it reaches the programmer. The final cut starts a fresh game with its full clock. Clicking anywhere also returns to the title during the comic.

The built-in image generator produced the four panels as one landscape sheet at `Obsession/Assets/Resources/QuietTheGlitch.png`. `Corridor.Title.cs` shows a quadrant at a time in reading order, preserves its aspect ratio, and uses the existing launch and hiss audio. Unity Play Mode checks confirmed the 1536 × 1024 import, all four panel cues, residual hiss playback, and the transition to gameplay with zero elapsed time.

<details><summary>Artwork generation prompt</summary>

Illustration-story, original dark science fiction graphic novel interior page for a game's cinematic intro. Single LANDSCAPE 3:2 sheet of exactly four equal full-bleed landscape panels in a precise 2x2 grid separated only by a very thin black rule. Serious moody hand-drawn ink on paper, dense crosshatching, dry-brush scratches, strong chiaroscuro, textured slate and desaturated petrol-grey washes, tarnished bone highlights, tiny selective vermilion accents. No pixel art, no arcade neon, no cartoon gloss. One lean tired adult programmer with cropped hair wearing a battered dark field jacket and an old metal wrist terminal. The virtual corridor is made of ominous recursive architectural frames and impossibly fine broken grid lines in shadow. Top left: intimate over-shoulder shot, wrist terminal legibly says 'BUILD PASSED', and a pinprick of red glows far down the empty corridor. Top right: same person frozen in same place, closer threatening red jagged signal tears through the air like a scar, casting red light on cheekbone. Bottom left: side view firing a pale green-white debugging pulse at the red scar, stark impact flash with inked silhouette and red fragments. Bottom right: extreme first-person shot into large centered worn optic; inside its lens a very fine residual crimson signal trembles horizontally across the almost black receding corridor. Essential subjects inset inside each quadrant. Coherent cinematic sequential narrative, masterful expressive ink drawing. No lettering except BUILD PASSED, no onomatopoeia, no speech bubbles, no title, no labels, no outer page margins.

</details>

### Tutorial

**Tutorial** opens an untimed single-player practice level. Each of four drills isolates one layer of the same signal and leaves one control active. Amplitude changes hill height, frequency changes spacing, phase slides peaks sideways, and vertical offset lifts or lowers the entire line. Tune with the usual Player 1 keys or cabinet controls, or click the minus and plus buttons. Match green to red and fire with Space, cabinet Select, or the on-screen button. A miss gives a directional hint and allows a retry. A match compares the whole signal before and after the shot and waits for the player to continue. Escape, cabinet Start, or Exit leaves training.

The last drill opens the real results layout with the four practice shots. Four short notes explain the stack of harmonics, the original target and final sum, the residual and RMS, and the difference between Final Fit and its supporting metrics. A small fifth ripple deliberately survives so players can see what they would try next. Practice uses the gameplay sampler, tuning steps, subtraction, and scoring routines with power fixed at 1. A new tutorial or live run clears practice history. The drills and guided results live in `Obsession/Assets/Corridor.Tutorial.cs`.

An isolated Unity Play Mode check opened Tutorial from the splash menu, attempted an early shot, checked that a locked control had no effect, and solved all four layers using simulated keyboard and cabinet inputs. The report contained four shots and a final RMS between 0.05 and 0.06. The check also exercised report paging, replay, early exit, splash and comic music playback, and the existing co-op regression checks. The test editor logged its existing Quick Search indexing exception during startup.

## Keyboard and mouse tuning

While tuning, tap **W/S** to move the wave up/down by 0.01, **A/D** to shift it left/right by 5° of phase, **Y/H** to decrease/increase frequency by 0.05 cycles, and **U/J** to raise/lower amplitude by 0.05. Each press changes one step. A/D wraps continuously around the 360° phase circle, and the mouse slider shows the equivalent phase in its signed range. The other controls stop at their limits. Unity Play mode verified 145 consecutive taps in each direction across the phase boundary. Positive phase shifts this sine formula left. During tuning, the panel intercepts WASD navigation before a focused slider can also consume it. A Play mode regression check focused the opposite axis, sent all four keys and their UI navigation events, and confirmed only the intended value moved. The keys drive the same slider values and live preview as mouse input. The four tuning sliders are now hidden. Their callbacks still serve keyboard and cabinet tuning. Launch and the results screen’s return button remain clickable. Press **Space** once during tuning to launch the current preview. Mouse removal is deferred. A Unity Play mode test sent all eight key presses through the Input System and checked each resulting slider value. The exact solution used the sliders for tuning and the previous simulated launch key for each of the ten launches. It completed the level with final RMS 2.51e-8.

The original ten-launch tests below describe the turn-based prototype. Current progression is governed by elapsed gameplay time.

## Implementation plan and decisions

The first playable plan followed four milestones. Keep this order when diagnosing a regression.

1. **Tune.** Use a fixed first-person camera, a common horizontal domain, a zero line, and a live overlay. Give the player direct control over the component they contribute.
2. **Launch and subtract.** Freeze the component on launch. At contact show how the signed gap between the two curves becomes the next silhouette. A poor shot can increase the error.
3. **Refine.** Move the residual forward by exactly one corridor frame, then magnify vertically for legibility. Apply the same display gain to the next preview. Keep the original target, shot samples, sum, and error in their mathematical units.
4. **Reveal.** Show what the player contributed, how the contributions sum, and how the result scores. A seeded synthetic target in a test makes it possible to check the arithmetic independently of the effects.

The first version used three shots. We briefly extended the scenery to 34 world units, then clarified that “deeper” meant more playable stations with shallower spacing. A twelve-launch version established traversal from the back frame to the second frame from the front. The current request settled the count at **ten**. The earlier reveal used pagination. The current reveal shows every shot together.

## Level geometry, timing, and math

- The target moves every gameplay frame from 14.5 to 2 units, covering each of ten 1.25-unit blocks in 15 seconds. A full run lasts two minutes and 30 seconds, including idle time. 
- Space and the launch button share a three-second launch-to-launch cooldown. The first shot is available immediately. Each player’s tuning and launch are locked during their own three-second cooldown. Launch snapshots the component, frequency, and power. Power starts at 1.0 on each run. Tap I to increase power or K to decrease it by 0.1. Power bottoms out at zero. The aiming pipe shows the charge of the next launch, including during cooldown. Power weights the score and sets visible charge. Before remapping the keys, Unity Play mode verified power adjustment during flight, independent 1.0 and 1.1 launch weights, the resulting weighted metrics, and replay reset. It lands after 1.05 seconds and the residual changes at impact while movement continues. The other player can tune and fire independently. At arrival, new launches close and any committed shot completes before the reveal. An idle run ends at zero points. An accelerated Unity Play mode check verified one block of idle travel in 15 gameplay seconds, idle timeout, cooldown rejection, shot snapshots, and a 13-shot run including a launch just before arrival.
- Each curve has 161 samples across normalized x from 0 to 1. A shot is `A × sin(2πfx + φ) + vertical offset`, with phase converted from degrees. Amplitude ranges from 0 to 1.5 by 0.05, frequency from 0.05 to 6 cycles by 0.05, and phase from −180° to 180° by 5°. Vertical offset ranges from −1 to +1 in 0.01 steps, in original signal units. It starts at zero, persists between launches like the other sliders, and resets on replay. The other initial settings are amplitude 0.5, 1 cycle, and 0°. Fractional frequencies carry through to scoring and history.
- The target starts with a constant +0.35 displacement and adds three sinusoidal components. This gives the offset control a baseline correction to make. On initial Play and each replay, independently draw amplitude, cycles, and phase for each component on the tuning grid. In coarse-to-fine order the amplitude ranges are 0.60–0.85, 0.25–0.45, and 0.10–0.20. Their frequency bands are 0.75–1.50, 1.75–3.00, and 3.25–5.50 cycles. All shots and the reveal share that run's original target. At each impact add the committed waveform to the running sum, then compute residual = original − sum. For display, use a peak-based gain clamped between 0.25 and 8.

## Visuals and score screen

The target has a white blade core, feathered red halo, and drifting red particles. The aiming pipe has two parallel green rails and an open center. Their spacing and halo width increase with power in screen pixels, and their cores brighten toward white. The launched blade uses its snapshotted power for width, brightness, particle density, and trail brightness. Zero power remains dimly visible. Unity camera captures at 0×, 1×, and 4× showed increasing green intensity in aiming and flight. The flight check reset the next-shot power to zero after launch. At launch the committed waveform takes on a white core, green halo, and green particles. Flight adds four trailing echoes. Impact flashes white and amber across the struck waveform, scatters 160 to 256 sparks that cool from gold into multicolor trails, and sends two expanding shock rings toward the camera. The fired power controls the size and reach of the burst. The effects use dynamic mesh geometry and an unlit vertex-color shader. Preserve the visibility of the signed residual during the clash.

The end screen stacks committed waveforms in groups of up to ten in launch order on the left. Each row uses the same height and gain. A smaller-amplitude shot therefore looks smaller even if gameplay magnified its preview. The right column contains the original versus final sum overlay, final residual with RMS error, score breakdown, and replay control. Chart segments are transformed in panel-local coordinates before the whole panel scales to the screen. This corrected the earlier bug where traces spilled across the labels.

| Metric | Current calculation |
|---|---|
| Accuracy | Positive reduction in residual RMS divided by original RMS, clamped to 0–1. |
| Efficiency | Sum of each shot’s signed RMS reduction times its launch power, divided by the sum of its power-weighted shot RMS values, clamped to 0–1. Zero effort gives zero efficiency. |
| Elegance | Share of total launch power assigned to shots that reduce RMS and use a frequency for the first time in that run. |
| Consistency | Share of total launch power assigned to shots that reduce RMS. Shown separately. |
| Final Fit | Round 1000 × accuracy + 300 × efficiency + 200 × elegance. Maximum 1500. |

Live scoring updates after each committed impact. The corridor and portrait reflect the current result, and the reveal gives the numeric breakdown.

## Arcade controller integration

The input portion of the supplied DHO-Arcade-GameJam-Template.unitypackage is installed with its original Unity asset GUIDs. The Arcade action map at `Obsession/Assets/Input/ArcadeControls.inputactions` exposes Move, ButtonA through ButtonF, ButtonStart, and ButtonExit. Its generated wrapper is alongside it. The supplied `ArcadesTemplate.Input.ArcadeInput` MonoBehaviour is in `Obsession/Assets/Scripts/ArcadeTemplate/Input/ArcadeInput.cs`. The cabinet's game-jam scenes, leaderboard, joystick tester scene logic, and project setup scripts remain available in the source archive. Each arcade action map is restricted to its assigned pad. The game reads keyboard shortcuts separately. Stick up/down controls vertical offset and left/right controls phase, one immediate step per deflection followed by repeat after 0.28 seconds, then every 0.08 seconds while held. WASD uses the same repeat timing. The axes repeat independently, release stops movement, and reversing steps immediately. A Unity Play mode check verified single-step taps, held diagonal movement, and stopping on release with a simulated cabinet controller. A/D decrease/increase cycles, B/E increase/decrease amplitude, and C/F increase/decrease power. Launch uses the remaining cabinet action ButtonExit (Change Game, Gamepad Select, USB button 9). Start/Play no longer fires. The supplied archive has no named Coin action. Both Space and cabinet launch use the same cooldown. Unity Play mode verified all nine actions loaded and a simulated gamepad north-button press triggered ButtonA. A simulated controller in Unity Play mode verified four directions, all six gameplay button adjustments, and the earlier Start mapping. A subsequent Unity Play mode check verified cabinet tuning with hidden sliders, Select launching, and Start staying idle. Physical cabinet wiring remains to be tested.

## Clash feedback and audio

Contact light runs along close matches. The signed gap briefly lights amber as the red residual takes shape. Debris gets an upward kick where the shot falls short and a downward kick where it overshoots. A light pulse runs through the corridor frames. Camera recoil occurs only during the first 0.28 seconds of launch, with a stronger kick at higher power. The corridor mesh is world anchored so it stays still as the camera pans and tilts. The camera uses restrained handheld pan and tilt with slight roll during play. Low scores increase the amount and speed of movement. Launch adds a short backward kick and upward tilt over that motion. Replay and the reveal restore the resting camera pose. Strong hits leave a white afterimage that breaks into segments. The effects use the existing waveform mesh.

Launch uses an electrical crack with a bass punch. Clash uses an explosion with a sharp initial crack, a descending bass hit, and a rumbling tail. During play, an original eight-bar industrial synth loop runs at 120 BPM with distorted sub bass, detuned drones, a metallic arpeggio and machine percussion. Unity plays the directly synthesized WAV from `Assets/Resources`. The earlier arrangement remains available as `Obsession/IndustrialLoop.mid`. The synth loop stops on reveal. Replay restarts the loop. The effects use the committed shot power for volume, with both mixes rising from a quiet zero-power hit toward full volume at high power. The aim rails now brighten faster and spread a broader halo with increasing power. Unity imported all three WAVs and reached the clash in an isolated Play mode run. The speaker mix still needs a listening check.

## Score feedback in the corridor

Before the first hit, the corridor starts in a neutral state. After each impact, the current score eases the corridor from heavily dimmed red frames, irregular brightness drops, a darker floor grid and a broad, saturated red edge haze toward steady cool-white frames with clearer visibility. A score gain sends a bright band toward the player. A loss sends a dimming band toward the target. The visual response reaches full distress at 25%, passes through 16% of the lighting transition at a 30% score and 80% at a 50% score, and reaches clear, steady lighting at 70%. The game score itself still uses the original scale. The numeric gameplay HUD is hidden. An original pixel-art human portrait at the top left for player one, with a second at the top right when player two joins, blinks, stares ahead, and clenches its jaw at impacts. The enlarged pale portrait has wide eyes and a clenched stare. Lower scores add twitching, darker eye shadows, and three progressive levels of blood marks across the forehead, cheeks, and mouth. The portrait’s left eye, on screen right, has an independent nervous blink that becomes faster and sometimes doubles at low scores. The opening face is clean. The reveal retains the score breakdown.

## Live player portrait

During interactive play the top-right portrait tries the first camera. A local OpenCV worker detects and crops up to two frontal faces, then streams pixels and landmark positions in memory to Unity. Score-driven marks, red tint, the nervous eyelid and a clash grimace follow the live crop. An open camera exclusively owns the active portrait areas. It shows a tracking message before the first face and holds the latest crop across brief tracking gaps. The pixel face is reserved for capture disabled or unavailable, including with no usable Python/OpenCV. The crop enlarges both detected eyes and stretches the jaw region into a fixed, tense expression before drawing the moving score overlays. F8 toggles capture. The reveal and Editor Play mode exit release the camera. Camera images stay on the device and are not saved.

The worker requires Python 3 with OpenCV and its bundled face-cascade data. Set `OBSESSION_CAMERA_PYTHON` to the interpreter path on a cabinet with a custom installation. On this development machine the existing `neat-snake` environment supplies OpenCV. Otherwise the game tries `python3` on PATH. The detector produced a 96×96 crop and overlay coordinates from a public sample face, and an isolated Unity Play mode run rendered the fallback through impact. Live alignment on the physical camera still needs a playtest.

## Two-player play

Player one uses the green waveform and the top-left portrait. Press Start on the second pad or Enter on the keyboard to join as the blue player and show the top-right portrait. Joining does not launch a shot. Other player-two controls are ignored until joining. Arrow keys adjust position and phase, Page Up/Down change amplitude, [ and ] decrease/increase frequency, + and − change power, and either Shift key launches. Numpad plus and minus adjust power, and Numpad Enter can join. Enter is reserved for joining during gameplay. The second pad uses the same cabinet layout as the first. Gamepad/joystick devices are assigned to separate action maps, with the keyboard handled independently.

Both waves can travel in parallel and contribute to the same residual and score on impact. Each launch locks that player's tuning, power, and launch for three seconds. The other player can act during this interval. The reveal distinguishes shot history by color and waits for in-flight shots at the deadline.

When two faces are visible, the local camera worker follows separate left and right crops in the mirrored image, with independent portrait textures and animations. The second camera slot waits for a detectable face when only one is visible. F8 disables capture for both players, and each active slot gets an animated pixel portrait when capture is unavailable. Player two has swept auburn hair, a narrower jaw, an uneven grin, and a blue portrait frame. Unity Play mode verified isolated keyboard and pad tuning, independent cooldowns, overlapping launches, and both impacts reaching the shared score. OpenCV produced both crop packets from a two-face sample. Physical pads and live two-person framing remain to be checked.

## Detail zoom and tension cues

Handheld displacement and rotation are two-thirds of their previous strength. When an impact calls for at least 7% more vertical gain, the clash gets a brief lead-in, then the clock, target, flights, cooldowns, and input pause for a 0.26-second hold and a 1.25-second eased zoom. The presentation and audio continue during the pause.

A quiet hiss layers over the music and rises linearly as the target approaches. Its start and end volumes are half the previous values. A short beep sounds once per remaining gameplay second starting at 30. A round red pixel alarm lamp under each active portrait blinks from 0.75 flashes per second at the best score to 3.2 at the worst. The heartbeat sound was removed with the heart indicator.

## Where to work

- [Corridor.cs](Obsession/Assets/Corridor.cs) owns the bootstrap, level constants, state transitions, sampled waveform math, dynamic geometry, effects, input controls, scoring, and reveal. Keep frame count, traversal, and score history in agreement when changing the level.
- [Demo.cs](Obsession/Assets/Editor/Demo.cs) owns the Editor menu entry and optional scene creation.
- [Rendering assets](Obsession/Assets/Rendering/) include the URP pipeline and renderer assets. The Resources subdirectory holds the corridor shader and runtime UI theme. Unity-generated URP global settings and the volume profile live in `Assets/`.
- [Package manifest](Obsession/Packages/manifest.json) and the adjacent lock file record URP 17.6.0 and Input System 1.20.0. The interactive sliders and buttons use UI Toolkit. The read-only HUD and reveal charts use IMGUI. Keep the instruction and slider labels explicitly light against the dark tuning panel.
- [Project settings](Obsession/ProjectSettings/) select URP as the default pipeline, the new Input System as the sole input backend, and Vulkan first on Linux with OpenGL Core as fallback.

## Environment and troubleshooting history

The built-in audio module is an explicit project dependency. Unity previously stripped the default camera AudioListener when the module was disabled. An isolated Unity Editor check created a default scene and verified its camera retained an enabled listener after enabling the module.

The initial Editor launch failed before license activation. Later Play mode tests succeeded after activation. The first empty-scene demo also depended on a menu command, which prompted the ordinary Play bootstrap described above.

Unity Hub warned that the Built-In Render Pipeline was deprecated. The migration replaced immediate-mode drawing in the camera callback with a dynamic mesh and URP-compatible shader. An OpenGL Console error named `LightmapIndirectIntegration` came from Unity’s lightmap baker. Vulkan-first startup avoided that error on the tested Intel Iris Xe system. OpenGL remains available for systems using software rendering. The input backend and interactive controls were subsequently migrated after the legacy Input Manager warning appeared.

The corridor was rendered successfully on Intel integrated graphics and, in a separate test, through Mesa llvmpipe using OpenGL CPU rendering. That test predates the latest effects. Minimum-spec frame rate, software rendering of the final effects, and other operating systems still need measurement. A separate Ubuntu Wayland incident involved an unidentified “Unknown” window and a silent Hub process. Hub was restarted gracefully after confirming the Editor had exited. Ownership of the unknown window was never established.

## Repeatable checks and remaining playtest work

1. Start from an untitled scene using ordinary Play. Drag each slider while unfocused and focused. Try 0.25, 0.50, and 0.75 cycles and check that the preview changes smoothly between discrete settings.
2. Compare the opening shapes of several replays. For a repeatable arithmetic check in a test, seed Unity's random generator before restart, reconstruct the three draws from the same seed, and enter their parameters. Apply +0.35 offset on the first launch only and allow the clock to run out. Expect near-zero final RMS.
3. In a second run, deliberately overshoot the first amplitude. Check that excess produces a negative residual where expected. Watch the depth movement finish before the change of gain.
4. Verify one block of travel after 15 seconds, both idle and while firing. A run can contain zero shots or more than ten. Confirm the reveal occurs at the front and credits an in-flight shot. Replay should clear history, sum, score progression, and tuning state.
5. Inspect the score screen at two Game view sizes. Verify ten traces on a common scale, all right-column metrics, and an accessible replay button. Check that the ten-row layout reads well on the intended minimum resolution.
6. Check the green and red cores at the far and near stations and the residual immediately after impact. Ask a player to predict the sign of the residual and explain what changed during magnification. This is the original playtest question.
7. On a representative low-spec machine, time the final effects during flight and impact and test the interactive controls end to end. The historical automated checks used temporary isolated Editor projects, exercised the stage transitions, and sampled rendered camera pixels. They verified ten playable positions and the presence of red and green in the impact frame. They did not establish visual quality, interactive frame rate, or final screen legibility.

Randomized-target verification in Unity confirmed different opening profiles on replay, an unchanged original across ten launches, and final RMS 2.58e-8 for a seeded reconstruction.

The earlier fixed-target Play mode check set all four slider values through UI Toolkit, completed ten launches, reached the reveal on the second frame, and measured final RMS of 2.51e-8 for the known solution. The faint aiming trace still needs a visual check in the Game view.

## Appendix — original reference file

Source: [OBSESSION_Core_Mechanic_Plan.md](OBSESSION_Core_Mechanic_Plan.md). The complete source follows verbatim.

---

# OBSESSION - Harmonic Silhouette Matching

## Scope note for Codex / Unity

**Status:** Raw concept / exploratory core-mechanic preview.

This document is a starting plan for prototyping one potential gameplay element in Unity through Codex. It is not a specification for the full game, a finalized game design document, or a commitment to any particular technical implementation. The working title, scoring, progression, tutorials, visual presentation, and mathematical implementation suggestions remain open to experimentation.

The immediate objective is a small, playable proof of concept: a first-person perspective corridor, a target silhouette, a harmonic with adjustable amplitude, frequency and phase, a launch and impact sequence, residual calculation, movement of the residual toward the player, visible vertical magnification, and an end-of-sequence decomposition reveal. Prioritize testing how this interaction feels and how clearly it communicates harmonic decomposition. Retain residual values in original mathematical units; treat visual magnification as a display transformation only. Other ideas in the discussion below are exploratory and do not need to be implemented in the first preview.

---

## Original brainstorming response (full text)

Yes — this is now a **real core mechanic**, not just a theme.

What you’ve defined is strong because it combines:

- **mathematical decomposition**
- **visual metaphor**
- **progressive refinement**
- **player obsession through shrinking residuals**

The really elegant part is this:

> The player does not directly “draw the silhouette.”
> They repeatedly contribute a harmonic component, and the game turns the remaining error into the next target.

That is a very clean ludic expression of **Fourier-style approximation**.

---

# Formalized core mechanic

## High concept

A **silhouette-matching game using harmonics**.

The player stands in a **first-person corridor** facing a distant target silhouette. On each turn, they construct a harmonic component by adjusting:

- **Amplitude**
- **Frequency**
- **Phase**

They then **launch** this harmonic shape down the corridor toward the target silhouette.

When the launched shape reaches the target:

1. it is compared against the current target,
2. the **residual** is computed,
3. that residual becomes the **new target**,
4. the new target appears **one corridor block closer** to the player,
5. the camera / framing **zooms vertically** to emphasize that the player is now matching finer detail.

This repeats until the corridor steps are exhausted.

At the end, the game reveals:

- the **original silhouette**
- the **stack of all harmonics used**
- the **sum of those harmonics**
- the **difference between original and reconstruction**
- the **final score**

---

# Why this works

This mechanic is strong for several reasons.

## 1. The player performs decomposition indirectly

The player is not told “now do Fourier decomposition.”

Instead, they experience the logic of it:

- choose a component,
- subtract it from the target,
- examine what remains,
- repeat.

That is exactly the right educational structure.

## 2. Residual-as-target is brilliant

This is the most original part.

Instead of just showing “your current approximation vs target,” the game converts the mismatch into the next objective.

That means the game loop itself teaches:

- coarse approximation first,
- fine approximation later,
- remaining error becomes increasingly local / subtle,
- obsession emerges because the player is literally chasing smaller and smaller flaws.

## 3. The corridor gives the abstraction a physical form

The corridor does a lot of work:

- far distance = big, coarse structure
- near distance = detail / scrutiny
- blocks = decomposition stages
- forward progression = approximation progress
- approaching residual = psychologically intense endgame

This gives the math a **spatial metaphor**, which is great for memorability.

---

# Recommended formal definition

## Representation

You probably want to define the silhouette as a **1D height function** over horizontal position, rather than a fully arbitrary filled 2D image.

So for a width-normalized coordinate $x \in [0,1]$:

$$
S(x)
$$

is the target silhouette profile.

Each launched harmonic is:

$$
H_n(x)=A_n \sin(2\pi f_n x + \phi_n)
$$

or possibly cosine/sine equivalent.

The running reconstruction after $k$ shots is:

$$
R_k(x)=\sum_{n=1}^{k} H_n(x)
$$

The residual is:

$$
E_k(x)=S(x)-R_k(x)
$$

And **the next target is $E_k(x)$**, visually re-centered / rescaled for readability.

That gives you a very clear formal system.

---

# Core loop

## Per turn

1. **Observe current target**
   - displayed as a silhouette at the end of the corridor

2. **Tune one harmonic**
   - amplitude
   - frequency
   - phase

3. **Preview shape**
   - player sees the waveform/silhouette generated by the chosen harmonic

4. **Launch**
   - harmonic travels down corridor as a visible moving shape

5. **Impact / compare**
   - on contact with target, game computes residual

6. **Refinement transition**
   - target transforms into residual
   - residual moves one block closer
   - visible zoom or magnification occurs
   - next turn begins

7. **Repeat until corridor exhausted or score threshold reached**

---

# Strong visual interpretation

To make this legible, I would suggest the following:

## The target silhouette
Displayed as a vertical profile panel or “energy wall” at the corridor’s end.

## The launched harmonic
Appears as a luminous waveform / silhouette-strip traveling forward.

## The hit moment
On impact, show:

- harmonic overlay
- matched regions briefly flash
- unmatched regions remain highlighted
- then the residual peels away and becomes the next target

## Zoom
This is important:
the new residual should not merely move closer — it should also be **visibly vertically magnified**, so the player feels:

> “Now I’m dealing with smaller details.”

That is crucial to the obsession fantasy.

---

# Suggested terminology

You already have the right words, but here’s a clean vocabulary set:

- **Target** = current silhouette to match
- **Harmonic** = one player-generated component
- **Launch** = committing the harmonic
- **Residual** = difference after subtraction
- **Step** or **Block** = one refinement stage in corridor
- **Reconstruction** = cumulative sum of launched harmonics
- **Final Fit** = total match score at end

Possible game verbs:

- **Tune**
- **Launch**
- **Resolve**
- **Refine**

---

# Important design choice: subtraction or best-fit projection?

There are two versions of the mechanic.

## Version A: literal subtraction
Whatever harmonic the player creates is subtracted directly from the current target.

### Pros
- intuitive
- tactile
- feels like “I am contributing this exact piece”

### Cons
- can be messy if player choices are poor
- residual may become chaotic or harder to read
- less mathematically “clean”

## Version B: projected harmonic extraction
The player proposes a harmonic family, and the game computes the best-fit coefficient for that chosen frequency/phase basis.

For example, the player chooses frequency and phase, and the game determines the effective amplitude contribution automatically or semi-automatically.

### Pros
- cleaner pedagogy
- more stable residual progression
- easier balancing

### Cons
- less direct authorship
- may feel less toy-like

## My recommendation
Use a hybrid:

- player controls **amplitude, frequency, phase**
- but the game may softly **snap** or **quantize** near effective values
- residual is slightly normalized for readability after each step

That preserves agency without making the system unreadable.

---

# What the corridor progression really means

Each corridor block is a **refinement stage**.

You can think of the stages like this:

## Stage 1: Coarse shape
Large-scale form dominates.
Low-frequency harmonics matter most.

## Stage 2: Structural correction
Main bumps, slopes, and asymmetries become obvious.

## Stage 3: Secondary details
More local deviations stand out.

## Stage 4+: Fine details
Tiny imperfections remain.
Player becomes fixated on very small corrections.

This is exactly where the **obsession theme** becomes gameplay.

The player will feel:
- “That’s close enough.”
- then immediately:
- “But I can still improve it.”

That’s the whole game.

---

# End-of-level reveal

Your proposed ending screen is excellent and should absolutely stay.

## Final reveal screen should show:

### 1. Original silhouette
The initial target.

### 2. All harmonics used
Displayed stacked vertically in order of use.

This is both beautiful and educational.

### 3. Cumulative reconstruction
The sum of all launched harmonics.

### 4. Overlay comparison
Original vs reconstruction.

### 5. Final residual
What remains unmatched.

### 6. Score breakdown
Accuracy, efficiency, elegance, etc.

This end-state gives the player:
- satisfaction,
- clarity,
- post-hoc learning,
- a feeling of “I built this from components.”

---

# Scoring ideas

You asked about scoring “somehow” — here’s a good structure.

## Primary score dimensions

### 1. Match Accuracy
How close the final reconstruction is to the original silhouette.

Possible metric:
- normalized mean squared error
- normalized absolute error
- overlap score

### 2. Efficiency
How much accuracy was achieved with how few harmonics.

Reward:
- fewer steps
- smaller total amplitude budget
- lower total complexity

### 3. Elegance
Encourages meaningful decomposition instead of brute force.

Possible bonuses:
- using lower frequencies effectively
- smooth progression from coarse to fine
- avoiding redundant harmonics

### 4. Consistency
Reward players whose residual shrinks steadily across steps.

This helps avoid random flailing.

---

## Example score formula

A simple version:

$$
\text{Score} = 1000 \cdot \text{Accuracy} + 300 \cdot \text{Efficiency} + 200 \cdot \text{Elegance}
$$

Where:

- **Accuracy** = based on final residual size
- **Efficiency** = fewer harmonics / fewer corridor steps used
- **Elegance** = lower complexity cost, smoother frequency progression, fewer “wasted” shots

You could also grade:
- **S / A / B / C**
- with sublabels like:
  - Precise
  - Efficient
  - Elegant
  - Obsessive

---

# A very important UX challenge

This concept is good, but the biggest risk is:

> Players may not understand what changing amplitude, frequency, and phase is doing fast enough.

So the game needs extremely strong feedback.

## You probably need all of these:

### Live harmonic preview
Before launch, show the waveform clearly.

### Overlay preview
Show how the chosen harmonic would affect the current target.

### Ghost residual prediction
Optionally preview expected remaining error.

### Immediate visual explanation
When a harmonic hits:
- show what got corrected
- show what got worse
- show the new residual

Without this, players may experience the game as arbitrary.

---

# Suggestions to make it playable

## 1. Limit one parameter complexity at a time
Early levels should isolate learning.

### Tutorial progression
- Level 1: amplitude only
- Level 2: amplitude + phase
- Level 3: fixed harmonic set
- Level 4: full amplitude/frequency/phase control

## 2. Give a constrained harmonic palette
Instead of any real-valued frequency, use a curated set.

For example:
- 1x
- 2x
- 3x
- 5x
- 8x

That makes the game more strategic and less like freeform signal fitting.

## 3. Normalize residual display
Since later residuals are smaller, visually magnify them so the player can still work meaningfully.

You already proposed vertical zoom — that’s the right move.

## 4. Consider sign / inversion cues
Residuals can be positive or negative relative to the silhouette. The player should clearly understand whether they over-added or under-added.

Color coding could help:
- one color for excess
- another for missing regions

---

# Possible level structure

## Level objective types

### 1. Reach minimum residual
Standard puzzle objective.

### 2. Match within limited launches
Tests efficiency.

### 3. Use restricted harmonic set
Tests understanding.

### 4. Hit target score
Mixes accuracy and elegance.

### 5. Perfect decomposition challenge
For advanced players.

---

# Strong thematic interpretation

Even though this is math-heavy, it still fits **obsession** very well.

Because the game rhythm becomes:

- identify a mismatch,
- reduce it,
- reveal a smaller mismatch,
- zoom in,
- reduce that,
- reveal something smaller,
- continue until only minute defects remain.

This is obsession made mechanical.

The player is trained to think:

> “The shape is basically solved…
> but I can still remove that remaining defect.”

That’s exactly the emotional structure you wanted.

---

# Best short pitch for the concept

Here’s a concise version you could keep as the official pitch:

> A first-person silhouette-matching game where the player reconstructs a target shape by launching harmonic components down a corridor. Each impact subtracts the chosen harmonic from the target, and the remaining residual becomes a new, closer target, magnified to reveal finer detail. Over several refinement steps, the player builds a full harmonic reconstruction of the original silhouette. At the end, the game reveals the original shape, all harmonics used, their sum, and a score based on accuracy, efficiency, and elegance.

---

# My honest assessment

This is a **distinctive and potentially beautiful core mechanic**.

Its strongest features are:

- the **residual becoming the next target**
- the **corridor as staged refinement**
- the **final decomposition reveal**

Those three ideas together are the identity of the game.

The only real danger is usability:
the relationship between parameter changes and silhouette outcome must feel learnable and satisfying.

If you solve that, the concept has real originality.

---

If you want, the next useful step would be one of these:

1. **turn this into a one-page game design document**
2. **design the exact scoring formula and win conditions**
3. **design the UI / screen layout**
4. **work out the math model for silhouette representation and residual calculation**
5. **prototype the first 5 tutorial levels**
