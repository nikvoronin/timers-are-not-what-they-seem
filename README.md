# Timers Are Not What They Seem

Analysis of the .NET timers with 1ms tick size:

- [System.Timers.Timer](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/doc/01-system-timers-timer.ipynb)
- [System.Threading.Timer](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/doc/02-system-threading-timer.ipynb)
- [Multimedia Timer](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/doc/03-multimedia-timer--winmm-dll.ipynb) imported from `winmm.dll`
- [TwinCAT 3. Soft Real-Time System](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/doc/04-twincat3-soft-realtime-system--notifications.ipynb)
- Dedicated Thread
  - [Thread.Sleep(1)](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/doc/05-dedicated-thread--thread-sleep-1ms.ipynb)
  - [SpinWait 10000us](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/doc/06-dedicated-thread--spinwait-1ms.ipynb)
  - [Thread.Sleep(0)](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/doc/07-dedicated-thread--thread-sleep-0.ipynb)
  - [Without idle between ticks](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/doc/08-dedicated-thread--no-idle-no-action.ipynb)
  - [Interval by Stopwatch](https://github.com/nikvoronin/timers-are-not-what-they-seem/blob/master/doc/09-dedicated-thread--interval-by-stopwatch-timer.ipynb)

## Results

- `System.Timers.Timer` and `System.Threading.Timer` are limited with 15.6 ms by default.
- `Multimedia Timer`. Always stable but legacy.
- TwinCAT3 notifications. Not good at client side.
- Independed threads. Not stable at heavy load.

**Conclusion.** Should use legacy `Multimedia Timer` or modern real-time systems like TC3.

## Goodreads

- [Acquiring high-resolution time stamps](https://docs.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps) -- 31-05-2018

- [Results of some quick research on timing in Win32 by Ryan Geiss](http://www.geisswerks.com/ryan/FAQS/timing.html) -- 16 August 2002 (...with updates since then)

- [Real-Time Systems with Microsoft Windows CE 2.1](https://docs.microsoft.com/en-us/previous-versions/ms834197(v=msdn.10)?redirectedfrom=MSDN#realtime21_latency) -- 06/29/2006

- `pdf` [A Real Time Operating Systems (RTOS) Comparison](http://csbc2009.inf.ufrgs.br/anais/pdf/wso/st04_03.pdf)

- `docx` [Timers, Timer Resolution, and Development of Efficient Code](http://download.microsoft.com/download/3/0/2/3027d574-c433-412a-a8b6-5e0a75d5b237/timer-resolution.docx) -- June 16, 2010

## CPU Burner

Loads all cpu's cores w/ 100%

```c#
while (!token.IsCancellationRequested) {
    Parallel.For( 0, Environment.ProcessorCount, ( i ) => {
        double a = 1000.0;
        for (int j = 0; j < 10_000_000; j++) {
            if (token.IsCancellationRequested)
                break;

            a /= j * (i + 1);
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

## TwinCAT 3. Soft Real-Time System

### RT-system Side

System tick and PLC-Runtime both are 1 millisecond.

```c#
PROGRAM MAIN
VAR
    tick: BOOL;
END_VAR

tick := NOT tick;

END_PROGRAM
```

### Client Side

```csharp
AdsClient client = new AdsClient();
client.Connect( AmsPort.R0_RTS + 1 );

client.AdsNotification += (s,e) => DoTick();
var notiSets = new NotificationSettings( AdsTransMode.OnChange, 1, 0 );
var h_tick = client.AddDeviceNotification( "MAIN.tick", 1, notiSets, null );
```
