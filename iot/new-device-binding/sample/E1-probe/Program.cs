// E1 probe (plan Step 3): raw bytes from the BMP390/BMP388, using only System.Device.I2c (no binding).
// Usage: ./E1-probe [address]   (default 0x77)
using System.Device.I2c;

int address = args.Length > 0 ? Convert.ToInt32(args[0], 16) : 0x77;
using var dev = I2cDevice.Create(new I2cConnectionSettings(1, address));

byte[] Read(byte register, int count)
{
    var buffer = new byte[count];
    dev.WriteRead(new[] { register }, buffer);
    return buffer;
}

void Write(byte register, byte value) => dev.Write(new[] { register, value });
string Hex(byte[] bytes) => string.Join(" ", bytes.Select(b => b.ToString("X2")));

Console.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}, bus 1, address 0x{address:X2}");
Console.WriteLine($"Chip ID (0x00): 0x{Read(0x00, 1)[0]:X2}");
Console.WriteLine($"ERR     (0x02): 0x{Read(0x02, 1)[0]:X2}");
Console.WriteLine($"STATUS  (0x03): 0x{Read(0x03, 1)[0]:X2}");
Console.WriteLine($"Calibration (0x31, 21 bytes): {Hex(Read(0x31, 21))}");

Write(0x1C, 0x00); // OSR: x1 pressure, x1 temperature
for (int i = 1; i <= 5; i++)
{
    Write(0x1B, 0x13); // PWR_CTRL: press_en | temp_en | forced
    Thread.Sleep(10);
    byte status = Read(0x03, 1)[0];
    byte[] d = Read(0x04, 6);
    int rawPressure = d[0] | (d[1] << 8) | (d[2] << 16);
    int rawTemperature = d[3] | (d[4] << 8) | (d[5] << 16);
    Console.WriteLine($"Reading {i}: STATUS 0x{status:X2}, data {Hex(d)}, raw P {rawPressure}, raw T {rawTemperature}");
    Thread.Sleep(200);
}

// Mode-transition check (plan entry 2026-10-05, finding 3): normal, then forced directly, then read PWR_CTRL back.
Write(0x1D, 0x03); // ODR 40 ms, so normal mode is valid with x1/x1
Write(0x1B, 0x33); // normal
Thread.Sleep(50);
Console.WriteLine($"After normal:            PWR_CTRL 0x{Read(0x1B, 1)[0]:X2}, ERR 0x{Read(0x02, 1)[0]:X2}");
Write(0x1B, 0x13); // forced, straight from normal
Thread.Sleep(50);
Console.WriteLine($"After forced (direct):   PWR_CTRL 0x{Read(0x1B, 1)[0]:X2}");

Write(0x1B, 0x00); // sleep
Write(0x1D, 0x00); // ODR back to its reset value
Console.WriteLine($"After sleep:             PWR_CTRL 0x{Read(0x1B, 1)[0]:X2}");
