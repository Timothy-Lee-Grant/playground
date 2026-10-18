// sleep-diag: what happens when normal mode is left while a measurement is running?
// conf-diag showed conf_err works at once, but 3 combinations "fitted" yet ended in sleep with ERR 0x00, each right after
// a trial that left the chip measuring in normal mode. Hypothesis: a sleep request takes effect only when the running
// measurement finishes, and writes in that window are lost. Raw I2C only. Usage: ./sleep-diag [address]. About 30 s.
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

void Wait(Stopwatch sw, double ms)
{
    while (sw.Elapsed.TotalMilliseconds < ms)
    {
    }
}

// Long measurement: x32 pressure, x1 temperature = 67.4 ms, at an 80 ms period.
void StartLongNormal()
{
    Write(0x1B, 0x00);
    Thread.Sleep(150); // anything running has surely finished
    Read(0x02);
    Write(0x1C, 0x05);
    Write(0x1D, 0x04);
    Write(0x1B, 0x33);
}

Console.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}, address 0x{address:X2}, chip 0x{Read(0x00):X2}");

Console.WriteLine("A: sleep written 10 ms into a 67 ms measurement; PWR_CTRL and STATUS afterwards");
StartLongNormal();
var sw = Stopwatch.StartNew();
Wait(sw, 10);
Write(0x1B, 0x00);
double sleepAt = sw.Elapsed.TotalMilliseconds;
foreach (double after in new[] { 0.0, 1, 5, 20, 40, 55, 60, 65, 70, 90 })
{
    Wait(sw, sleepAt + after);
    Console.WriteLine($"  +{sw.Elapsed.TotalMilliseconds - sleepAt,6:F1} ms after sleep: PWR_CTRL 0x{Read(0x1B):X2} STATUS 0x{Read(0x03):X2}");
}

Console.WriteLine("B: right after sleep, write OSR = 0x00; read OSR back at once and 100 ms later");
StartLongNormal();
Thread.Sleep(10);
Write(0x1B, 0x00);
Write(0x1C, 0x00);
Console.WriteLine($"  at once: OSR 0x{Read(0x1C):X2}");
Thread.Sleep(100);
Console.WriteLine($"  +100 ms: OSR 0x{Read(0x1C):X2}   (0x00 = the write stuck, 0x05 = lost)");

Console.WriteLine("C: right after sleep, write normal again (x1/x1 @ 80 ms, valid); PWR_CTRL and ERR 200 ms later, 5 tries");
for (int i = 0; i < 5; i++)
{
    StartLongNormal();
    Thread.Sleep(5 + (i * 12));
    Write(0x1B, 0x00);
    Write(0x1C, 0x00);
    Write(0x1B, 0x33);
    Thread.Sleep(200);
    Console.WriteLine($"  try {i + 1} (sleep at +{5 + (i * 12)} ms): PWR_CTRL 0x{Read(0x1B):X2} ERR 0x{Read(0x02):X2} OSR 0x{Read(0x1C):X2}");
}

Console.WriteLine("D: same as C, but wait 70 ms (> the 67.4 ms measurement) after the sleep write, 5 tries");
for (int i = 0; i < 5; i++)
{
    StartLongNormal();
    Thread.Sleep(5 + (i * 12));
    Write(0x1B, 0x00);
    Thread.Sleep(70);
    Write(0x1C, 0x00);
    Write(0x1B, 0x33);
    Thread.Sleep(200);
    Console.WriteLine($"  try {i + 1} (sleep at +{5 + (i * 12)} ms): PWR_CTRL 0x{Read(0x1B):X2} ERR 0x{Read(0x02):X2} OSR 0x{Read(0x1C):X2}");
}

Write(0x1B, 0x00);
Thread.Sleep(100);
Write(0x1C, 0x00);
Write(0x1D, 0x00);
