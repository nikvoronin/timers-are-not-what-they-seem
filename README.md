# Timers Are Not What They Seem

Analysis of the .NET timers with 1ms tick size:

- `System.Timers.Timer`
- `System.Threading.Timer`
- `Multimedia Timer` import from `winmm.dll`
- TwinCAT 3. Notifications
- Independed thread w/
  - `Thread.Sleep(1)`
  - `SpinWait` over 10000us = 1ms
- Supplement
  - Thread w/ `Thread.Sleep(0)`
  - No idle thread

See data and graphics in [delta-analysis.ipynb](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/delta-analysis.ipynb). If you don't have Jupyter Notebooks, see [delta-analysis.html](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/delta-analysis.html).

## Results

- `System.Timers.Timer` and `System.Threading.Timer` are limited with 15.6 ms. Can't tick faster.
- `Multimedia Timer`. Stable at 1ms on idle or heavy load but legacy.
- TwinCAT3 notifications. Not good at client side.
- Independed threads. Not stable at heavy load.

Conclusion. Still use legacy `Multimedia Timer` or modern real-time systems like TC3.

## Goodreads

[Acquiring high-resolution time stamps](https://docs.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps) 31-05-2018

- QPC support in Windows versions
- Guidance for acquiring time stamps
- Virtualization
- Direct TSC usage
- Examples for acquiring time stamps
- General FAQ about QPC and TSC
- FAQ about programming with QPC and TSC
- Low-level hardware clock characteristics
- Absolute Clocks and Difference Clocks
- Resolution, Precision, Accuracy, and Stability
- Hardware timer info

## CPU Burner

Loads all cpu's cores w/ 100%

```c#
while (!token.IsCancellationRequested) {
    Parallel.For( 0, Environment.ProcessorCount, ( i ) => {
        double a = 1000.0;
        for (int j = 0; j < 10_000_000; j++) {
            if (token.IsCancellationRequested)
                break;

            a /= j * i;
        }
    } );
}
```

See [CpuBurner.cs](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/src/DeltaTimer/CpuBurner.cs)

## Multimedia Timer

Legacy functions from early versions of Windows

```c#
[DllImport( "winmm.dll", SetLastError = true, EntryPoint = "timeSetEvent" )]
internal static extern UInt32 TimeSetEvent( UInt32 msDelay, UInt32 msResolution, MultimediaTimerCallback callback, ref UInt32 userCtx, UInt32 eventType );

[DllImport( "winmm.dll", SetLastError = true, EntryPoint = "timeKillEvent" )]
internal static extern void TimeKillEvent( UInt32 uTimerId );
```

See [MultimediaTimer.cs](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/src/DeltaTimer/MultimediaTimer.cs)

## TwinCAT 3. Notifications

### RealTime system side

System tick and PLC-Runtime both are 1 millisecond.

```c#
PROGRAM MAIN
VAR
    tick: BOOL;
END_VAR

tick := NOT tick;

END_PROGRAM
```

### Client side

```csharp
AdsClient client = new AdsClient();
client.Connect( AmsPort.R0_RTS + 1 );

client.AdsNotification += (s,e) => DoTick();
var notiSets = new NotificationSettings( AdsTransMode.OnChange, 1, 0 );
var h_tick = client.AddDeviceNotification( "MAIN.tick", 1, notiSets, null );
```
