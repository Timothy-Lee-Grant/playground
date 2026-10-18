// conf-diag: how does the real BMP390 react to a normal-mode request whose settings are too fast?
// (hw-check (b) found: no conf_err when read right after the write, yet the chip ends in sleep.)
// Raw I2C only, no binding. Usage: ./conf-diag [address]   (default 0x77). Takes about a minute.
using System.Diagnostics;
using System.Device.I2c;

int address = args.Length > 0 ? Convert.ToInt32(args[0], 16) : 0x77;
using var dev = I2cDevice.Create(new I2cConnectionSettings(1, address));

byte Read(byte register)
{
    var buffer = new byte[1];
    dev.WriteRead(new[] { register }, buffer);
    return buffer[0];
}

void Write(byte register, byte value) => dev.Write(new[] { register, value });

void Configure(int osrP, int osrT, int odr)
{
    Write(0x1B, 0x00); // sleep
    Thread.Sleep(5);
    Read(0x02); // clear ERR (cleared on read)
    Write(0x1C, (byte)(osrP | (osrT << 3)));
    Write(0x1D, (byte)odr);
}

// The binding's measurement time (BMP390 datasheet section 3.9.2), in microseconds.
int DurationMicroseconds(int osrP, int osrT) => 234 + 392 + ((1 << osrP) * 2020) + 163 + ((1 << osrT) * 2020);

Console.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}, address 0x{address:X2}, chip 0x{Read(0x00):X2}");

// Part 1: x32/x32 at 5 ms. Write normal, then poll PWR_CTRL, ERR and STATUS as fast as possible, then at intervals.
Console.WriteLine("Part 1: x32/x32 at 5 ms; PWR_CTRL / ERR / STATUS over time after writing normal (0x33)");
Configure(5, 5, 0);
var sw = Stopwatch.StartNew();
Write(0x1B, 0x33);
foreach (int at in new[] { 0, 0, 0, 1, 2, 5, 10, 20, 50, 100, 200, 500 })
{
    while (sw.Elapsed.TotalMilliseconds < at) { }
    double t = sw.Elapsed.TotalMilliseconds;
    Console.WriteLine($"  t={t,7:F2} ms  PWR_CTRL 0x{Read(0x1B):X2}  ERR 0x{Read(0x02):X2}  STATUS 0x{Read(0x03):X2}");
}

// Part 2: the same, but ERR is read only once, at 500 ms (ERR clears on read, so Part 1's reads could hide a late flag).
Console.WriteLine("Part 2: same settings, ERR read only once, 500 ms after the write");
Configure(5, 5, 0);
Write(0x1B, 0x33);
Thread.Sleep(500);
Console.WriteLine($"  PWR_CTRL 0x{Read(0x1B):X2}  ERR 0x{Read(0x02):X2}");

// Part 3: which combinations does the chip accept? Accepted = still in normal mode 300 ms later.
Console.WriteLine("Part 3: osrP osrT ODR(ms) | binding duration (us) | fits? | chip: PWR_CTRL ERR -> accepted?");
int[] odrMs = { 5, 10, 20, 40, 80 };
int mismatches = 0;
for (int odr = 0; odr < odrMs.Length; odr++)
{
    for (int osrP = 0; osrP <= 5; osrP++)
    {
        for (int osrT = 0; osrT <= 5; osrT++)
        {
            int us = DurationMicroseconds(osrP, osrT);
            bool fits = us <= odrMs[odr] * 1000;
            if (us > (odrMs[odr] * 1000 * 2) || us < (odrMs[odr] * 1000 / 4))
            {
                continue; // far from the boundary: skip to keep the run short
            }

            Configure(osrP, osrT, odr);
            Write(0x1B, 0x33);
            Thread.Sleep(300);
            byte pwr = Read(0x1B);
            byte err = Read(0x02);
            bool accepted = (pwr & 0x30) == 0x30;
            mismatches += accepted == fits ? 0 : 1;
            Console.WriteLine($"  x{1 << osrP,-2} x{1 << osrT,-2} {odrMs[odr],3} | {us,6} | {(fits ? "yes" : "no "),3} | 0x{pwr:X2} 0x{err:X2} -> {(accepted ? "yes" : "no ")}{(accepted == fits ? "" : "   <-- MISMATCH")}");
        }
    }
}

Console.WriteLine($"Part 3 mismatches between the binding's rule and the chip: {mismatches}");
Write(0x1B, 0x00);
Write(0x1C, 0x00);
Write(0x1D, 0x00);
