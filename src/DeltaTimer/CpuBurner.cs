using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaTimer
{
    public static class CpuBurner
    {
        public static void Fire(CancellationToken token)
        {
            Task.Factory.StartNew( () => {
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
            } );
        }
    }
}
