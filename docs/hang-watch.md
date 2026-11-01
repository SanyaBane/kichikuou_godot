# The frame watch: what a frozen game leaves behind

Owner, 2026-11-01: the game froze on the Poco in a battle and nothing in its logs was left to say why. The Android build
now watches its own frames and keeps its log in a file, so the next freeze says where it stood, even when the player closes
the game at once.

## If the game freezes

Keep it open if you can, connect the phone and run `pull-android-logs.cmd` at the root (`tools/android-logs.ps1`): with the
process alive it first saves what the threads are doing and asks for their stacks, then the reports, Godot's logs, the
logcat, how the process ended and the audio players' events, into a new folder under
`%LOCALAPPDATA%\KichikuouGodot\android\logs`, and last the stacks (`stacks-trace_NN.txt`, from a bug report, ~3 minutes;
`-NoStack` leaves them out; the device keeps a few such dumps, each names its process and time at its head). Closed already, the same command still gets the reports and Godot's logs (the logcat rolls
over within minutes of ordinary use, so sooner is better).

- The reports: `Android/data/org.kichikuou.port/files/hang-reports/hang-<time>.txt` (a PC: `user://hang-reports`, only
  with `--hang-watch`).
- Godot's log: `user://logs/godot.log` and its last 9 launches (`project.godot`, `debug/file_logging`), on Android in the
  app's internal folder, which `run-as` reads in the debug build. Each print is flushed
  (`application/run/flush_stdout_on_print`), so a killed process loses nothing: the launch's random seed and the frame times
  are there.

## How it works

`Engine/Diagnostics/FrameWatch` runs on a thread of its own and looks twice a second whether a new frame has begun. The
main thread only writes, without a lock (`godot/Strategy/HangWatch`, on Android and with `--hang-watch`):

- **the frame's beat and places** (`At`): `godot/Strategy/FrameBeat`, two nodes on the root, the first and the last
  processed, and Godot's `frame_pre_draw` and `frame_post_draw`; `GameScreen._Process` and `BattleBoardView` in between;
- **what the game did last** (`Note`): every script event as it reaches `GameScreen.ShowAsync`, each key wait, pause and
  menu, a start of the damage forecasts;
- **a pause on purpose**: `NOTIFICATION_APPLICATION_PAUSED` and `RESUMED`. Only the main thread can deliver them, so a
  frozen game in the background still counts as frozen.

No frame for 8 s opens a report; 10, 15, 60 and 300 s add samples of the threads (`Diagnostics/ProcThreads`, from
`/proc/self/task`: state, CPU time, the kernel's wait and the system call); a frame again closes it with the time it took.
A short stall that recovered is a report too. Nothing counts before the first frame: the boot runs in the frame before it,
~6 s on the Poco at a launch, which is also why 8 s and not less.

## Reading a report

The game's line says the week, the battle's cursors, whether the message skip was on and which game screen of the launch it
was (more than one: a Restart went before). Then the last of **where the frames were**, the seconds before the report:

| The last place | The main thread stands in |
| --- | --- |
| `frame`, `game screen`, `present`, `battle screen`, `board: …` | that part of our `_Process` |
| `game screen done`, `board: its process` | another node's `_Process` |
| `nodes processed` | a node's `_Draw`, or the engine before the drawing |
| `draw` | Godot's rendering: Vulkan, the GPU's driver |
| `drawn` | the script's code, after the frame (C# continuations run after the drawing), or the audio's step |
| `script waits` | Godot's main loop outside our code |

The main thread in the thread list is **`VkThread`** on Android, the one with the CPU time (the GPU's driver names helper
threads the same), and the samples tell what the place cannot:

- `R` and its CPU ticks growing by ~100 a second: a loop (seen with `--hang-spin`: +202 over 2.3 s);
- `S` and `futex_wait_queue`: waiting on a lock, a fence or the collector (`--hang-test` too: Mono's sleep is a futex);
- a `wait` in the GPU's driver: the driver.

**No report at all from a frozen game** means the watch's thread stopped too: every managed thread is suspended, the mark of
a collection that never ended (the Mono runtime, `libmonosgen`). A report that ends before its `collections:` line, the same
through the collector's lock. Godot's log tells that the launch had the watch on.

## Seeing one on purpose

`--hang-test N` and `--hang-spin N` stop the main thread N seconds 120 frames into the launch, asleep or busy, to see a
report. On the Poco a launch's parameters do not reach the game (`am start --esa command_line_params` arrives without its
extras), so there the same words go into a file the next launch reads and removes:

```bash
adb shell "echo --hang-spin 40 > /storage/emulated/0/Android/data/org.kichikuou.port/files/hang-reports/test.txt; chmod 666 /storage/emulated/0/Android/data/org.kichikuou.port/files/hang-reports/test.txt"
```

To the background without touching the phone (`input keyevent` is refused on it, MIUI's setting):

```bash
adb shell am start -a android.intent.action.MAIN -c android.intent.category.HOME
```

Checked on the Poco, 2026-11-02: a launch writes no report, nor do 12 s in the background (the player's `paused` at once,
`started` on the way back); asleep and busy stalls report as above; a busy stall sent to the background has **no `paused`
until the stall ends**, then at once, which is what the freeze below left in the audio's log.

## The stacks of an open game

`debuggerd -b` answers "root is required", but `run-as … kill -3 <pid>` makes the runtime write every Java-attached thread's
native stack to `/data/anr/trace_NN`, and the game goes on; shell cannot read that file, a bug report carries it
(`FS/data/anr/`). `VkThread`'s frames name where it stands:

| The top frames | The main thread stands in |
| --- | --- |
| `<anonymous:…>` | our C#, JIT-compiled (the unwinder stops there: the report's places say which part) |
| `libmonosgen-2.0.so` (`sgen_…`, `mono_threads_…`) | the runtime, the collector above all |
| `libGLES_mali.so`, `libvulkan.so`, `vulkan.*` | the GPU's driver |
| `libgodot_android.so` alone | the engine |

Seen with `--hang-spin` on the Poco: `clock_gettime` called from `<anonymous>`, under `GodotLib.step`, state R.

## The freeze of 2026-11-01

On the Poco, in the build of 2026-10-31, process 13296: a game that loaded, was Restarted and played anew from the
prologue to the rebels' stage. The freeze came on an enemy's turn in a battle, message skip on: Ex (commander 1) attacking
Rance (unit 1) with Melee, the exchange's counters up. The screenshot is of 23:15:51, the game went to the background at
23:16:06, came back at 23:16:14 and was closed at 23:16:23. Loading the auto-save and playing on did not repeat it (the
random generator is not saved, `Strategy/EngineState`: a load draws differently).

What the system's logs prove (Godot's own lines had rolled out of the logcat by the time the phone was connected):

- **The main thread never left its frame.** `dumpsys audio`'s player events have no `paused` for that process; the next
  launch's came 21 ms after its activity paused. Godot pauses the audio from `VkThread`'s loop between two frames
  (`VkThread.kt`, `VkRenderer.onVkPause`, `main_loop_focusout`), so a wait in our script, with the frames going on, would
  have paused it. That also rules out a lost finger release (`GameScreen.ReleaseWaitAsync`).
- At least 32 s, not a long frame.
- No rotation before it (the surface unchanged from 23:00:52 to 23:16:14), no memory pressure in the system
  (`am_mem_factor` unchanged), though the process held 3.09 GB (`am_pss`; 478 MB of it swapped) at the end.

The battle's rule code after an exchange has no loop that could turn without a frame (read, not tested: `Pick`, `RollShots`,
`ApplyExchangeAsync`), and the loops of our battle screen's drawing that were read are bounded. Open: the runtime's
collector, Vulkan or the GPU's driver, or a loop not read. The next report decides; `docs/backlog.md`, "Android".
