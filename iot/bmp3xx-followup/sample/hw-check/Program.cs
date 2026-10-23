// Hardware checks for the binding's power-mode handling (built against the fork's branch).
// (a) Normal mode with the constructor defaults (x1/x1, 5 ms = 200 Hz) is accepted and produces readings.
// (b) Normal -> sleep -> x32/x32 at 5 ms -> normal must throw InvalidOperationException and leave the sensor asleep.
// (c) Normal -> forced, at random points of the running measurement: Read() must return values (the forced write is not lost).
// (d) Normal -> sleep -> normal must end in normal mode.
// (e) As (c), worst case: x32/x32 (131 ms measurement) at 160 ms, so the margin of the wait is tested.
// Review round 1 (2026-10-19):
// (f) R3: after a forced measurement that isn't read, STATUS shows both data-ready bits; a burst read of DATA clears
//     them (datasheet table 29). Raw register access through a second handle, so it's the chip, not the binding.
// (g) R2: normal x1/x1 at 160 ms, oversampling raised to x32 and lowered again after a random gap (so an x32
//     conversion is sometimes running), then sleep and Read(): the forced measurement must not be lost.
// (g0) R2 informational: the same sequence by raw register writes, waiting only the 6 ms the binding waited before
//     the fix. Lost forced writes here mean the chip shows the problem the fix addresses.
using System.Diagnostics;
using System.Device.I2c;
using Iot.Device.Bmp3xx;

using var sensor = new Bmp390(I2cDevice.Create(new I2cConnectionSettings(1, Bmp3xxBase.DefaultI2cAddress)));
using var raw = I2cDevice.Create(new I2cConnectionSettings(1, Bmp3xxBase.DefaultI2cAddress));
var random = new Random();
int failures = 0;
Console.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");

void Report(string name, int passed, int total)
{
    failures += total - passed;
    Console.WriteLine($"{name}: {passed}/{total} {(passed == total ? "PASS" : "FAIL")}");
}

byte ReadRaw(byte register)
{
    Span<byte> value = stackalloc byte[1];
    raw.WriteRead(new[] { register }, value);
    return value[0];
}

void WriteRaw(byte register, byte value) => raw.Write(new[] { register, value });

void ReadRawData()
{
    Span<byte> data = stackalloc byte[6];
    raw.WriteRead(new byte[] { 0x04 }, data);
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

// (f)
Settings(Bmp3xxOversampling.X1, Bmp3xxOversampling.X1, Bmp3xxOutputDataRate.Period5Milliseconds);
ok = 0;
for (int i = 0; i < 10; i++)
{
    sensor.SetPowerMode(Bmp3xxPowerMode.Forced);
    Thread.Sleep(20);
    byte before = ReadRaw(0x03);
    ReadRawData();
    byte after = ReadRaw(0x03);
    Console.WriteLine($"    (f) try {i + 1}: STATUS before DATA read 0x{before:X2}, after 0x{after:X2}");
    ok += (before & 0x60) == 0x60 && (after & 0x60) == 0 ? 1 : 0;
}

Report("(f) unread forced measurement: drdy set, cleared by reading DATA", ok, 10);

// (g)
void RaiseAndLower(string name, Action<Bmp3xxOversampling> setSampling, int tries)
{
    int passed = 0;
    long slowest = 0;
    for (int i = 0; i < tries; i++)
    {
        Settings(Bmp3xxOversampling.X1, Bmp3xxOversampling.X1, Bmp3xxOutputDataRate.Period160Milliseconds);
        sensor.SetPowerMode(Bmp3xxPowerMode.Normal);
        Thread.Sleep(random.Next(0, 160));
        setSampling(Bmp3xxOversampling.X32);
        Thread.Sleep(random.Next(0, 160));
        setSampling(Bmp3xxOversampling.X1);
        var sw = Stopwatch.StartNew();
        sensor.SetPowerMode(Bmp3xxPowerMode.Sleep);
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

    Report($"{name} (slowest SetPowerMode(Sleep) + Read: {slowest} ms)", passed, tries);
}

RaiseAndLower("(g) pressure x1 -> x32 -> x1 in normal mode, then sleep and Read", value => sensor.PressureSampling = value, 30);
RaiseAndLower("(g) temperature x1 -> x32 -> x1 in normal mode, then sleep and Read", value => sensor.TemperatureSampling = value, 15);

// (g0)
int lost = 0;
for (int i = 0; i < 30; i++)
{
    Settings(Bmp3xxOversampling.X1, Bmp3xxOversampling.X1, Bmp3xxOutputDataRate.Period160Milliseconds);
    WriteRaw(0x1B, 0x33); // normal
    Thread.Sleep(random.Next(0, 160));
    WriteRaw(0x1C, 0x05); // x32 pressure, x1 temperature
    Thread.Sleep(random.Next(0, 160));
    WriteRaw(0x1C, 0x00); // x1/x1
    WriteRaw(0x1B, 0x03); // sleep
    Thread.Sleep(6); // the wait before the fix: x1/x1 duration 5 ms + 10% + 1 ms
    ReadRawData(); // clear data-ready
    WriteRaw(0x1B, 0x13); // forced
    Thread.Sleep(15);
    lost += (ReadRaw(0x03) & 0x60) == 0x60 ? 0 : 1;
    Thread.Sleep(100); // let a still-running x32 conversion end, so it can't swallow the next try's writes
}

Console.WriteLine($"(g0) informational, raw writes with the old 6 ms wait: forced measurement lost {lost}/30");

sensor.SetPowerMode(Bmp3xxPowerMode.Sleep);
Console.WriteLine(failures == 0 ? "ALL PASS" : $"{failures} FAILURE(S)");
