# Phase Panic

A red glitch is closing in. Tune and fire simple waves to quiet it, then zoom in on the finer signal each shot leaves behind. Play solo or let a second player join the same run.

[Play Phase Panic in your browser](https://angel-marchev.github.io/phase-panic/)

Open `Obsession/` in Unity Hub with Unity **6000.6.5f1**, enter Play mode, and switch to the Game view. Choose **Tutorial** to practice each wave control or **Start** to play a timed run. The game bootstraps from an empty scene.

The browser build is in `docs/`. To play locally, run `python3 -m http.server 8000 --bind 127.0.0.1 --directory docs` from the repository root and open [localhost:8000](http://localhost:8000/). To host it elsewhere, copy the contents of `docs/` to a static web directory, keeping the folder structure. Click inside the game to focus the controls and enable browser audio.

The [Unity handoff](OBSESSION_Unity_Handoff.md) covers the controls, scoring, hardware support, and tested Linux build. The [original design reference](OBSESSION_Core_Mechanic_Plan.md) explains the core mechanic.
