using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace DeltaTimer
{
    class Program
    {
        static long lastStamp = 0;
        static long[] deltas = new long[10000 * 1000 * 60]; // 1 us - 1 min
        static long lastDeltasIndex = 0;
        static long startStamp = 0;

        static void Main( string[] args )
        {
            startStamp = Stopwatch.GetTimestamp();

            //Do_TwincatNotification( burn: true );
            //Do_ThreadingTimer(1, burn:true);
            //Do_TimersTimer(interval: 1, burn: false);
            //Do_MultimediaTimer(1, burn: true);
            Do_ThreadSleepN(burn: true);

            using Stream csv = File.OpenWrite( $"th-nosleep-load.csv" );
            using TextWriter writer = new StreamWriter( csv );
            writer.WriteLine("load");

            for(long i = 0; i < lastDeltasIndex; i++)
                writer.WriteLine( $"{deltas[i] / 10000.0:F04}" );

            Wait_PressEnterToStop();
        }


        public static void DoTick()
        {
            if (lastDeltasIndex >= 10000)
                return;

            long now = Stopwatch.GetTimestamp();
            long delta = now - lastStamp;

            if (now - startStamp > 20 * 1000 * 10000) // 20 s warmup
                deltas[lastDeltasIndex++] = delta;

            if (lastDeltasIndex >= 10000)
                Console.WriteLine("DONE");

            lastStamp = now;
        }

        public static void Do_ThreadSleepN(bool burn)
        {
            var cts = new CancellationTokenSource();
            new Thread( () => Worker( cts.Token ) ).Start();

            if (burn)
                CpuBurner.Fire( cts.Token );

            Wait_PressEnterToStop();

            cts.Cancel();
        }

        //static long lastt = 0;
        public static void Worker(CancellationToken token)
        {
            while (!token.IsCancellationRequested) {
                DoTick();

                //SpinWait.SpinUntil( () => {
                //    long now = Stopwatch.GetTimestamp();
                //    long delta = now - lastt;
                //    lastt = now;

                //    return delta > 10000;
                //} );

                //Thread.Sleep( 0 );
            }
        }

        public static void Do_MultimediaTimer(int interval, bool burn)
        {
            var cts = new CancellationTokenSource();

            var mTimer = new MultimediaTimer() {
                Interval = interval,
                Resolution = 0 // ms, maximum possible resolution = 0
            };

            mTimer.Elapsed += ( s, e ) => DoTick();
            mTimer.Start();

            if (burn)
                CpuBurner.Fire( cts.Token );

            Wait_PressEnterToStop();

            cts.Cancel();
            mTimer.Stop();
        }

        public static void Do_TimersTimer(double interval, bool burn)
        {
            var cts = new CancellationTokenSource();

            var timer = new System.Timers.Timer( interval );
            timer.Elapsed += ( s, e ) => DoTick();
            timer.Start();

            if (burn)
                CpuBurner.Fire(cts.Token);

            Wait_PressEnterToStop();

            cts.Cancel();
            timer.Stop();
        }

        public static void Do_ThreadingTimer( int interval, bool burn )
        {
            var cts = new CancellationTokenSource();
            var timer = new System.Threading.Timer( (o) => DoTick(), null, 1000, interval );

            if (burn)
                CpuBurner.Fire( cts.Token );

            Wait_PressEnterToStop();

            cts.Cancel();
            timer.Change( System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite );
            timer.Dispose();
        }

        public static void Do_TwincatNotification(bool burn)
        {
            var cts = new CancellationTokenSource();

            AdsClient client = new AdsClient();
            client.Connect( AmsPort.R0_RTS + 1 );

            client.AdsNotification += (s,e) => DoTick();
            var notiSets = new NotificationSettings( AdsTransMode.OnChange, 1, 0 );
            var h_tick = client.AddDeviceNotification( "MAIN.tick", 1, notiSets, null );

            if (burn)
                CpuBurner.Fire( cts.Token );

            Wait_PressEnterToStop();

            cts.Cancel();
            client.DeleteDeviceNotification( h_tick );
            client.Disconnect();
        }

        private static void Wait_PressEnterToStop()
        {
            Console.WriteLine( "Press [Enter] to stop..." );
            Console.ReadLine();
        }
    }
}
