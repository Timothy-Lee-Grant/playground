// Hardware checks for the binding's power-mode handling (built against the fork's branch).
// (a) Normal mode with the constructor defaults (x1/x1, 5 ms = 200 Hz) is accepted and produces readings.
// (b) Normal -> sleep -> x32/x32 at 5 ms -> normal must throw InvalidOperationException and leave the sensor asleep.
// (c) Normal -> forced, at random points of the running measurement: Read() must return values (the forced write is not lost).
// (d) Normal -> sleep -> normal must end in normal mode.
// (e) As (c), worst case: x32/x32 (131 ms measurement) at 160 ms, so the margin of the wait is tested.
using System.Diagnostics;
using System.Device.I2c;
using Iot.Device.Bmp3xx;

using var sensor = new Bmp390(I2cDevice.Create(new I2cConnectionSettings(1, Bmp3xxBase.DefaultI2cAddress)));
var random = new Random();
int failures = 0;
Console.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");

void Report(string name, int passed, int total)
{
    failures += total - passed;
    Console.WriteLine($"{name}: {passed}/{total} {(passed == total ? "PASS" : "FAIL")}");
}

void Settings(Bmp3xxOversampling pressure, Bmp3xxOversampling temperature, Bmp3xxOutputDataRate rate)
{
    sensor.SetPowerMode(Bmp3xxPowerMode.Sleep);
    sensor.PressureSampling = pressure;
    sensor.TemperatureSampling = temperature;
    sensor.OutputDataRate = rate;
}

// (a)
Settings(Bmp3xxOversampling.X1, Bmp3xxOversampling.X1, Bmp3xxOutputDataRate.Period5Milliseconds);
sensor.SetPowerMode(Bmp3xxPowerMode.Normal);
Thread.Sleep(100);
int ok = 0;
for (int i = 0; i < 10; i++)
{
    ok += sensor.TryReadPressure(out var p) && p.Hectopascals > 300 ? 1 : 0;
    Thread.Sleep(20);
}

Report("(a) normal mode at the defaults, valid pressure readings", ok, 10);

// (b)
ok = 0;
for (int i = 0; i < 10; i++)
{
    Settings(Bmp3xxOversampling.X1, Bmp3xxOversampling.X1, Bmp3xxOutputDataRate.Period5Milliseconds);
    sensor.SetPowerMode(Bmp3xxPowerMode.Normal);
    Thread.Sleep(random.Next(1, 20));
    Settings(Bmp3xxOversampling.X32, Bmp3xxOversampling.X32, Bmp3xxOutputDataRate.Period5Milliseconds);
    try
    {
        sensor.SetPowerMode(Bmp3xxPowerMode.Normal);
        Console.WriteLine($"    (b) try {i + 1}: did not throw, mode {sensor.ReadPowerMode()}");
    }
    catch (InvalidOperationException)
    {
        ok += sensor.ReadPowerMode() == Bmp3xxPowerMode.Sleep ? 1 : 0;
    }
}

Report("(b) too-fast settings after normal mode throw and leave the sensor asleep", ok, 10);

// (c) and (e)
void NormalThenForced(string name, Bmp3xxOversampling pressure, Bmp3xxOversampling temperature, Bmp3xxOutputDataRate rate, int periodMs, int tries)
{
    int passed = 0;
    long slowest = 0;
    for (int i = 0; i < tries; i++)
    {
        Settings(pressure, temperature, rate);
        sensor.SetPowerMode(Bmp3xxPowerMode.Normal);
        Thread.Sleep(periodMs + random.Next(0, periodMs));
        var sw = Stopwatch.StartNew();
        sensor.SetPowerMode(Bmp3xxPowerMode.Forced);
        var result = sensor.Read();
        slowest = Math.Max(slowest, sw.ElapsedMilliseconds);
        if (result.Pressure is null || result.Temperature is null)
        {
            Console.WriteLine($"    {name} try {i + 1}: no values (forced measurement lost)");
        }
        else
        {
            passed++;
        }
    }

    Report($"{name} (slowest SetPowerMode + Read: {slowest} ms)", passed, tries);
}

NormalThenForced("(c) normal -> forced, x1/x1 at 5 ms", Bmp3xxOversampling.X1, Bmp3xxOversampling.X1, Bmp3xxOutputDataRate.Period5Milliseconds, 5, 30);

// (d)
ok = 0;
for (int i = 0; i < 10; i++)
{
    Settings(Bmp3xxOversampling.X8, Bmp3xxOversampling.X1, Bmp3xxOutputDataRate.Period40Milliseconds);
    sensor.SetPowerMode(Bmp3xxPowerMode.Normal);
    Thread.Sleep(random.Next(40, 80));
    sensor.SetPowerMode(Bmp3xxPowerMode.Sleep);
    sensor.SetPowerMode(Bmp3xxPowerMode.Normal);
    ok += sensor.ReadPowerMode() == Bmp3xxPowerMode.Normal ? 1 : 0;
}

Report("(d) normal -> sleep -> normal ends in normal", ok, 10);

NormalThenForced("(e) normal -> forced, worst case x32/x32 at 160 ms", Bmp3xxOversampling.X32, Bmp3xxOversampling.X32, Bmp3xxOutputDataRate.Period160Milliseconds, 160, 15);

sensor.SetPowerMode(Bmp3xxPowerMode.Sleep);
Console.WriteLine(failures == 0 ? "ALL PASS" : $"{failures} FAILURE(S)");
