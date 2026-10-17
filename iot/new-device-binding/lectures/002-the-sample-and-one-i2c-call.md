# Lecture 002: The sample, object by object, and one I2C call from C# to the wire

> **Prompted by:** Timothy, 2026-10-17: "walk me through everything that gets created in the sample code, and how an
> I2C call actually gets performed for this new device"
> **Code:** your fork, `feature/bmp3xx-binding` @ `8d23d3d4`; `System.Device.Gpio` from the same tree (dotnet/iot
> `main` @ `336e4696`). File references below are relative to `iot/src/`.
> **Prerequisite:** [lecture 001](001-the-bmp3xx-binding-end-to-end.md) §1–§3 (the cast; one `Read()`). This lecture
> goes one level **below** the binding.
> **Status of claims:** everything above the `ioctl` line is read directly from the code (✅ code). Everything the Linux
> kernel does below it is standard Linux I2C behaviour, **not verified on your Pi yet**: §7 gives you the commands to
> verify it yourself (⚠️ until you run them).

---

## How to use this lecture

| If you have… | Read |
|---|---|
| **15 minutes** | §1 (the map), §4 (one I2C call, the table), §5 (what it looks like on the wire) |
| **45 minutes** | add §2 (every object the sample creates), §3 (the Linux plumbing), §6 (writes and burst reads) |
| **A session at the Pi** | do §7: watch the real system calls with `strace`, then make the same transactions by hand with `i2ctransfer` |

Every section starts with its **one-line rule** in bold.

---

## 1. The map: five layers between `sensor.Read()` and the copper

**Rule: the binding never touches hardware; it calls `I2cDevice`, which calls Linux, which drives the I2C controller in the Pi's chip.**

```
 ┌─ YOUR PROCESS (C#, managed) ──────────────────────────────────────────────────────────────┐
 │  Program.cs (sample)          sensor.Read()                                               │
 │        │                                                                                  │
 │  Bmp390 / Bmp3xxBase          ReadRegisters(Data, 6 bytes)          ◄─ lecture 001         │
 │        │  Device.WriteRead(...)                                                           │
 │  I2cDevice (abstract)  ──is──► UnixI2cDevice   (remembers: which bus, which address 0x77) │
 │        │  _bus.WriteRead(0x77, ...)                                                       │
 │  UnixI2cBus                   (remembers: the open file /dev/i2c-1 = file descriptor)     │
 │        │  builds two i2c_msg structs, calls Interop.ioctl(fd, I2C_RDWR, &msgs)            │
 │  Interop.ioctl  ── P/Invoke ──► libc.so  ioctl()                                          │
 └────────┼──────────────────────────────────────────────────────────────────────────────────┘
          │  system call: the CPU switches from your program into the kernel
 ┌─ LINUX KERNEL ─────────────────────────────────────────────────────────────────────────────┐
 │  i2c-dev driver    (the thing behind /dev/i2c-1)                                           │
 │  i2c core          (checks the messages, locks the bus)                                    │
 │  i2c-bcm2835       (the Pi's I2C controller driver: writes the controller's registers)     │
 └────────┼───────────────────────────────────────────────────────────────────────────────────┘
          │
   BSC1 I2C controller in the Pi's SoC ──► GPIO2 (SDA) / GPIO3 (SCL) ──► wires ──► BMP390
```

The left-hand names are real types. Their files:

| Layer | Type | File |
|---|---|---|
| Binding | `Bmp3xxBase` | `devices/Bmp3xx/Bmp3xxBase.cs` |
| Device handle | `I2cDevice` (abstract), `UnixI2cDevice` | `System.Device.Gpio/System/Device/I2c/I2cDevice.cs`, `…/I2c/Devices/UnixI2cDevice.cs` |
| Bus handle | `UnixI2cBus` (and a fallback, `UnixI2cFileTransferBus`) | `…/I2c/UnixI2cBus.cs`, `…/I2c/UnixI2cFileTransferBus.cs` |
| Native glue | `Interop.open`, `Interop.ioctl`, `i2c_msg`, `i2c_rdwr_ioctl_data` | `System.Device.Gpio/Interop/Unix/Libc/Interop.open.cs`, `Interop.ioctl.cs` |

**What `System.Device.Gpio` does *not* do** (F7): it doesn't toggle SDA/SCL itself and doesn't know the I2C clock
speed. It hands complete messages to Linux, and the kernel driver plus the controller hardware generate every
START, address byte, ACK and STOP. The one thing it *does* decide is **how the messages are grouped**, and that
decides whether you get a repeated START or two separate transactions (§5).

---

## 2. Everything the sample creates, in order

**Rule: the sample builds a chain (settings → bus → device → sensor), uses it, and the `using` at the top tears the whole chain down at the end.**

Here is `samples/Program.cs`, the part that creates things, with what each line leaves behind.

### 2.1 Before anything: the program itself

`Program.cs` has no `class Program` and no `Main`. That's **top-level statements**: the compiler wraps the file's
statements in a generated `Main` method for you. The `static string Format(...)` at the bottom is a *local function*
inside that generated `Main`. (In C terms: the file *is* the body of `main()`.)

### 2.2 The object table

| # | Line in `Program.cs` | What gets created | Kind | Owns / holds | Lives until |
|---|---|---|---|---|---|
| 1 | `I2cConnectionSettings settings = new(busId, Bmp3xxBase.DefaultI2cAddress);` | `I2cConnectionSettings { BusId = 1, DeviceAddress = 0x77 }` | class, immutable (get-only properties) | nothing: just two numbers | end of program (garbage-collected) |
| 2 | `I2cDevice i2cDevice = I2cDevice.Create(settings);` | **(a)** an open Linux file: `/dev/i2c-1`, as an integer *file descriptor* | OS resource | — | `close()` in step 9 |
| | | **(b)** a `UnixI2cBus` holding that descriptor + `BusId = 1` + an empty `ConcurrentDictionary` of devices | class (internal) | **owns** the file descriptor | disposed in step 9 |
| | | **(c)** a `UnixI2cDevice` holding the bus, `0x77`, and `shouldDisposeBus: true` | class (internal), returned as `I2cDevice` | **owns** the bus | disposed in step 9 |
| 3 | `using Bmp3xxBase sensor = new Bmp390(i2cDevice);` | a `Bmp390` object (its fields live in `Bmp3xxBase`); during construction **about 11 I2C transactions** happen (§2.3) | class | **owns** the `I2cDevice` | end of `Main` (the `using`) |
| | | inside it: a `Bmp3xxCalibrationData` with 14 `double`s | class (internal) | nothing | with the sensor |
| 4 | `sensor.PressureSampling = …X8;` (and the next 2 lines) | nothing new; each setter does one read + one write of a register | — | — | — |
| 5 | `sensor.GetMeasurementDuration()` | an `int` (19 for ×8/×1 on a BMP390) | value | — | — |
| 6 | `Bmp3xxReadResult result = sensor.Read();` (10×) | a `Bmp3xxReadResult` each time, holding a `Temperature?` and a `Pressure?` | class + two nullable **structs** | — | next loop iteration (garbage-collected) |
| 7 | `sensor.TryReadAltitude(out Length altitude)` | a `Length` value (UnitsNet struct) | struct | — | the loop iteration |
| 8 | normal mode: `TryReadPressure(out Pressure pressure)` (5×) | a `Pressure` value each time | struct | — | the loop iteration |
| 9 | end of the program | `sensor.Dispose()` → `i2cDevice.Dispose()` → `bus.Dispose()` → `close(fd)` | — | — | — |

**Three kinds of thing in that table, and why it matters:**

| Kind | Examples | Who cleans it up |
|---|---|---|
| **Class objects** | `I2cConnectionSettings`, `UnixI2cBus`, `Bmp390`, `Bmp3xxReadResult` | the garbage collector, eventually, when nothing refers to them |
| **Structs (value types)** | `Temperature`, `Pressure`, `Length` (UnitsNet), `Span<byte>` | nobody: they're copied around like `int`s and vanish with their scope |
| **OS resources** | the file descriptor for `/dev/i2c-1` | **must be released explicitly** with `Dispose()` → `close()`. The garbage collector doesn't know a file is open |

That last row is the whole reason `IDisposable` exists. In C you'd write `int fd = open(...)` and later `close(fd)`; in
C# the object that holds the `fd` (here `UnixI2cBus`) implements `Dispose()`, and **ownership** (F10) says who calls it.
Where C# differs from C (F1): forgetting `close` in C leaks until the process exits; forgetting `Dispose` in C# does
the same, the garbage collector does *not* close it for you here.

### 2.3 Step 2 in detail: what `I2cDevice.Create` actually does

From `I2cDevice.cs` and `UnixI2cBus.cs` (✅ code):

```csharp
public static I2cDevice Create(I2cConnectionSettings settings)
{
    if (Environment.OSVersion.Platform == PlatformID.Win32NT) throw new PlatformNotSupportedException(...);
    return new UnixI2cDevice(UnixI2cBus.Create(settings.BusId), settings.DeviceAddress, shouldDisposeBus: true);
}
```

`UnixI2cBus.Create(1)`, step by step:

| # | Code | What it does | Afterwards |
|---|---|---|---|
| 1 | `string deviceFileName = $"{DefaultDevicePath}-{busId}";` | builds `"/dev/i2c-1"` | — |
| 2 | `lock (s_initializationLock) { … }` | only one thread at a time may create a bus | — |
| 3 | `Interop.open(deviceFileName, FileOpenFlags.O_RDWR)` | calls the C library's `open("/dev/i2c-1", O_RDWR)` | an integer, e.g. `3`, the **file descriptor** (fd). Negative = failure → `IOException("Can not open I2C device file ...")` |
| 4 | `Interop.ioctl(fd, I2C_FUNCS, &functionalityFlags)` | asks the kernel "what can this I2C adapter do?" | a bit mask |
| 5 | `if ((flags & I2C_FUNC_I2C) != 0) return new UnixI2cBus(fd, busId);` | the adapter supports **plain I2C messages** → the normal bus class | |
| 5' | `else return new UnixI2cFileTransferBus(fd, busId);` | adapter only supports simpler transfers → a fallback class (§5.3) | |

The Pi's controller supports plain I2C, so you get a **`UnixI2cBus`** (⚠️ verify with `i2cdetect -F 1` in §7).

Then `new UnixI2cDevice(bus, 0x77, shouldDisposeBus: true)` stores three fields: `_bus`, `_deviceAddress = 0x77`,
`_shouldDisposeBus = true`. `shouldDisposeBus: true` means "when this device is disposed, close the bus too",
because `Create` made a bus just for this device and nobody else holds it.

*Side detail:* a bus can also hand out devices itself (`bus.CreateDevice(0x77)`), and then it keeps a list of open
addresses to refuse duplicates. `I2cDevice.Create` takes the shortcut and doesn't register the device in that list.
That's fine here (one device, one bus), and it's not something the binding depends on.

### 2.4 Step 3 in detail: the I2C conversation inside `new Bmp390(i2cDevice)`

Lecture 001 §3.1 has the register-level table. Counted as **I2C transactions** (each one = one `ioctl` call, §4):

| # | Purpose | Binding call | Transaction shape |
|---|---|---|---|
| 1 | read chip ID | `ReadRegister(ChipId)` | write 1 byte, read 1 byte |
| 2 | is the chip ready for a command? | `ReadStatus()` inside `WaitFor` | write 1, read 1 (once, if ready: your Pi said `0x10`) |
| 3 | clear old "reset happened" flag | `ReadRegister(Event)` | write 1, read 1 |
| 4 | soft reset | `WriteRegister(Command, 0xB6)` | write 2 |
| — | `Thread.Sleep(2)` | | no bus traffic |
| 5 | did the command fail? | `ReadErrors()` | write 1, read 1 |
| 6 | reset finished? | `ReadRegister(Event)` inside `WaitFor` | write 1, read 1 (repeats until set) |
| 7–10 | defaults: OSR, CONFIG, ODR, PWR_CTRL | `ApplySettings()` → 4× `WriteRegister` | write 2, four times |
| 11 | calibration | `ReadRegisters(CalibrationData, 21 bytes)` | write 1, read 21 |

So creating the sensor costs **~11 transactions plus a 2 ms sleep**: a few milliseconds in total. §7.1 lets you count
them on the Pi.

### 2.5 Step 9 in detail: the teardown chain

`using Bmp3xxBase sensor = …;` is a **using declaration**. Expanded (F9):

```csharp
Bmp3xxBase sensor = new Bmp390(i2cDevice);
try
{
    // ... the rest of the program ...
}
finally
{
    sensor.Dispose();      // runs even if an exception was thrown
}
```

And `Dispose` cascades down the ownership chain (✅ code):

```
sensor.Dispose()                         Bmp3xxBase.Dispose(true): _i2cDevice.Dispose(); _i2cDevice = null
  └─ UnixI2cDevice.Dispose(true)         _shouldDisposeBus is true → _bus.Dispose()
       └─ UnixI2cBus.Dispose(true)       Interop.close(BusFileDescriptor); BusFileDescriptor = -1
            └─ libc close(3)             the kernel releases /dev/i2c-1 for this process
```

Notice the sample never disposes `i2cDevice` itself: it **handed ownership** to the sensor on line 3. That's the
repo convention (lecture 001 §7, U7): an `I2cDevice` passed to a binding belongs to the binding.

---

## 3. The Linux plumbing: file descriptors, `ioctl`, and P/Invoke

**Rule: on Linux the I2C bus is a file; you `open` it once, then send whole transactions to it with one `ioctl` each.**

### 3.1 Three words

| Word | Meaning | Here |
|---|---|---|
| **file descriptor (fd)** | a small integer the kernel gives you when you `open` something; every later call names the thing by that number | `UnixI2cBus.BusFileDescriptor`, e.g. `3` |
| **`ioctl`** ("I/O control") | the catch-all system call for "do a device-specific operation on this fd". Its second argument says *which* operation, the third points at its data | `ioctl(fd, I2C_RDWR = 0x0707, &msgset)` |
| **P/Invoke** | .NET's way to call a C function in a native library | `[DllImport("libc")] static extern int ioctl(int fd, uint request, IntPtr argp);` |

### 3.2 P/Invoke, the part you'll recognise

From `Interop/Unix/Libc/Interop.ioctl.cs` (✅ code):

```csharp
internal partial class Interop
{
    [DllImport(LibcLibrary, SetLastError = true)]          // LibcLibrary = "libc"
    internal static extern int ioctl(int fd, uint request, IntPtr argp);
}
```

- `extern` + `[DllImport("libc")]`: "the body of this method is the C function `ioctl` in `libc.so`". When .NET first
  calls it, it loads `libc` and finds the symbol, then calls it with the C calling convention. It's the same thing
  your production service does with its own native library.
- `SetLastError = true`: right after the call, .NET saves C's `errno`, so the error message
  (`ExceptionHelper.GetLastErrorMessage()`) can say *why* it failed (e.g. "No such device or address" when nothing
  ACKs at 0x77).
- `IntPtr argp`: a raw pointer, passed straight through to C.

### 3.3 The two C structs, mirrored in C#

Linux's header `<linux/i2c.h>` defines these; dotnet/iot copies their layout (✅ code):

```csharp
[StructLayout(LayoutKind.Sequential)]       // fields in this order, C padding rules: same as the C struct
internal unsafe struct i2c_msg
{
    public ushort addr;                     // 7-bit device address, e.g. 0x77
    public I2cMessageFlags flags;           // ushort: 0 = write, I2C_M_RD (0x0001) = read
    public ushort len;                      // number of bytes
    public byte* buf;                       // pointer to the bytes to send / the buffer to fill
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct i2c_rdwr_ioctl_data
{
    public i2c_msg* msgs;                   // pointer to an array of messages
    public uint nmsgs;                      // how many
}
```

**The 32-bit detail that connects to your Pi (and to issue #2600):** `buf` is a pointer, so its size depends on the
process. On your Pi's **32-bit userland** (`armhf`), a pointer is 4 bytes and `i2c_msg` is 2+2+2 (+2 padding) +4 =
**12 bytes**. On a 64-bit process it's 2+2+2 (+2 padding) +8 = **16 bytes**. Because the C# struct uses a real
pointer type (`byte*`), .NET lays it out correctly on both, exactly like the kernel expects. The bug in #2600 was the
opposite case: a C `unsigned long` modelled as a C# `ulong` (always 8 bytes), which is wrong on 32-bit ARM where C's
`long` is 4 bytes. Same lesson: **the C# mirror must have the C sizes on every platform.**

---

## 4. One I2C call, step by step: reading the chip ID

**Rule: one `WriteRead` = two `i2c_msg`s in one `ioctl` = one transaction on the wire with a repeated START between the two halves.**

This is the very first bus access the binding makes: `ReadRegister(Bmp3xxRegister.ChipId)` in the constructor.
The table shows **what exists after each step** (F8).

| # | Where | Code (simplified) | State afterwards |
|---|---|---|---|
| 1 | `Bmp3xxBase.ReadRegister` | `Span<byte> value = stackalloc byte[1]; ReadRegisters(ChipId, value);` | a 1-byte buffer on the stack, value 0 |
| 2 | `Bmp3xxBase.ReadRegisters` | `Device.WriteRead(stackalloc byte[] { 0x00 }, values);` | a second 1-byte buffer holding the register address `0x00` |
| 3 | `Bmp3xxBase.Device` (property) | `ThrowIfDisposed(); return _i2cDevice!;` | if the sensor was disposed: `ObjectDisposedException` here |
| 4 | `UnixI2cDevice.WriteRead` | `_bus.WriteRead(_deviceAddress /*0x77*/, writeBuffer, readBuffer);` | the address joins the call |
| 5 | `UnixI2cBus.WriteRead` | checks: address 0…65535, read buffer not empty, lengths ≤ 65535 | invalid input → `ArgumentException` *before* touching the bus |
| 6 | same | `fixed (byte* w = writeBuffer) fixed (byte* r = readBuffer) { WriteReadCore(0x77, w, r, 1, 1); }` | two raw pointers, **pinned** (§4.1) |
| 7 | `UnixI2cBus.WriteReadCore` | `i2c_msg* msgs = stackalloc i2c_msg[2];` | room for two messages on the stack |
| 8 | same | `msgs[0] = { flags = I2C_M_WR, addr = 0x77, len = 1, buf = w };` | message 0: "write the byte `0x00` to 0x77" |
| 9 | same | `msgs[1] = { flags = I2C_M_RD, addr = 0x77, len = 1, buf = r };` | message 1: "read 1 byte from 0x77 into r" |
| 10 | same | `var msgset = new i2c_rdwr_ioctl_data { msgs = msgs, nmsgs = 2 };` | the envelope |
| 11 | same | `int result = Interop.ioctl(BusFileDescriptor, I2C_RDWR /*0x0707*/, new IntPtr(&msgset));` | **your thread is now inside the kernel**, waiting (§5) |
| 12 | kernel | performs both messages as one transaction; copies the received byte into `r` | `r[0] = 0x60` |
| 13 | same | `if (result < 0) throw new IOException($"Error {...} performing I2C data transfer.");` | e.g. nothing ACKed → `IOException` |
| 14 | back up the stack | `ReadRegister` returns `value[0]` | `0x60` → the constructor compares it with the expected chip ID |

### 4.1 Three pieces of syntax from that table (F9)

**`unsafe` and `byte*`**: C-style pointers, allowed only inside code marked `unsafe`. Same meaning as in C. Where C#
differs (F1): normally you never see them; `Span<byte>` is the safe, bounds-checked version, and the code only drops to
pointers right at the boundary to C.

**`fixed`**: the garbage collector is allowed to **move** objects in memory (to compact the heap). If the kernel were
writing into a buffer while it moved, the kernel would write to the old address. `fixed (byte* p = buffer) { … }`
**pins** the buffer for the duration of the block, so the address handed to the kernel stays valid:

```csharp
fixed (byte* p = buffer) { use(p); }
// means roughly:
//   pin buffer so the GC can't move it;
//   p = address of buffer[0];
//   use(p);
//   unpin buffer;          (also if use() throws)
```

C has no equivalent because C memory never moves. (Here the buffers came from `stackalloc` and the stack never
moves either, but `UnixI2cBus` can't know that: a caller could pass a slice of a normal array, so it always pins.)

**Object initializer on a struct** (`new i2c_msg() { flags = …, addr = … }`): same as C's designated initializer
`(struct i2c_msg){ .flags = …, .addr = … }`.

### 4.2 Why the address isn't in the buffer

In the binding, the bytes you write are just `{ 0x00 }` (the *register* address). The *device* address `0x77` lives in
`UnixI2cDevice` and is put into each `i2c_msg.addr`. That separation is why the same binding works for 0x76 or 0x77:
only the `I2cConnectionSettings` changes.

---

## 5. What that looks like on the wire

**Rule: the kernel turns two messages in one `ioctl` into START … repeated START … STOP; the chip sees one uninterrupted conversation.**

### 5.1 The chip-ID read, bit by bit (⚠️ standard I2C; see it yourself with a logic analyzer)

The 7-bit address `0x77` = `111 0111`. The first byte on the wire is the address shifted left by one with the
read/write bit at the end: write → `0xEE`, read → `0xEF`.

```
 SDA/SCL events       S   [0xEE]  A   [0x00]  A   Sr  [0xEF]  A   [0x60]  N   P
 who drives it       Pi    Pi    chip   Pi   chip  Pi   Pi   chip  chip   Pi  Pi
 meaning           start addr+W  ack  reg 0x00 ack  rep. addr+R ack  data  nack stop
                                                     start              (=chip ID)
                   └──────── message 0 (write) ─────┘└──────── message 1 (read) ───────┘
                   └────────────────────── one ioctl(I2C_RDWR), one transaction ───────────┘
```

- **S** = START, **Sr** = repeated START (a START without a STOP before it), **P** = STOP.
- **A** = acknowledge (the receiver pulls SDA low for one clock). **N** = not-acknowledge: the Pi says "that was the
  last byte I want".
- If nothing answers at `0x77`, the first **A** is missing; the kernel aborts and `ioctl` returns −1. The `errno`
  depends on the controller driver: typically `ENXIO` ("No such device or address") or, on the Pi's driver,
  `EREMOTEIO` ("Remote I/O error") → step 13's `IOException`.

**Why the repeated START matters:** the BMP390 remembers the register address you wrote and starts the read from
there. With a STOP in between, most register-based chips still work (they keep the register pointer; ⚠️ not verified
for the BMP390), but the bus is released between the two halves and another master could talk in between. One `I2C_RDWR` with two messages keeps it
atomic.

**Timing at the Pi's default 100 kHz clock:** each byte is 9 clock cycles (8 bits + ack). The chip-ID read is 4 bytes
≈ 36 cycles + START/STOP ≈ **0.4 ms** on the wire. The 21-byte calibration read is about **2.3 ms**. The default
speed comes from the Pi's device tree (`dtparam=i2c_arm_baudrate`, default 100000), not from dotnet/iot.

### 5.2 Writes and burst reads

**A register write** (`WriteRegister(PowerControl, 0x13)` → `Device.Write({ 0x1B, 0x13 })` → `WriteReadCore` with
only a write buffer → **one message**):

```
 S  [0xEE] A  [0x1B] A  [0x13] A  P          "register 0x1B := 0x13"  (start a forced measurement)
```

**The 6-byte data read** (`ReadRegisters(Data, 6)`): same shape as §5.1, with six data bytes. The chip
auto-increments its register pointer after each byte, which is why one transaction delivers `0x04…0x09`:

```
 S [0xEE] A [0x04] A  Sr [0xEF] A [p_xlsb] A [p_lsb] A [p_msb] A [t_xlsb] A [t_lsb] A [t_msb] N  P
```

The Pi ACKs every data byte except the last, then NACKs and stops: that's how the reader tells the chip "enough".

**Settings setters** (`PressureSampling = X8`) are a read *then* a write: two separate `ioctl`s (one `WriteRead` to
read OSR, one `Write` to write it back). Read-modify-write is in the binding, not in the bus layer.

### 5.3 The fallback you don't use: `UnixI2cFileTransferBus`

If an adapter couldn't do plain I2C messages (`I2C_FUNC_I2C` unset), `UnixI2cBus.Create` would return
`UnixI2cFileTransferBus` instead. Its `WriteReadCore` does three separate calls (✅ code):

```
ioctl(fd, I2C_SLAVE_FORCE, 0x77)    "talk to 0x77 from now on"
write(fd, buf, 1)                   →  S [0xEE] A [0x00] A P
read(fd, buf, 1)                    →  S [0xEF] A [0x60] N P
```

That's **two transactions with a STOP in between**, no repeated START. It would most likely work for the BMP390 (⚠️ unverified: it depends on the chip keeping its register pointer
across the STOP) but isn't atomic. You can confirm your Pi uses the normal path with §7.2.

---

## 6. Putting it together: one `sensor.Read()` as bus traffic

**Rule: one `Read()` is five transactions and one sleep; the measurement itself happens inside the chip while the Pi sleeps.**

With the sample's settings (×8 / ×1, filter 3), after the constructor:

| # | Binding step | Transaction on the wire | Bytes | Chip state afterwards |
|---|---|---|---|---|
| 1 | `ReadPowerMode()` | `S EE A 1B A Sr EF A [03] N P` | 4 | sleep |
| 2 | `SetPowerMode(Forced)`: read PWR_CTRL | same as #1 | 4 | sleep |
| 3 | `SetPowerMode(Forced)`: write | `S EE A 1B A 13 A P` | 3 | **measuring** |
| 4 | `Thread.Sleep(19)` | none | 0 | measurement finishes (~19 ms), data registers updated, ready bits set, back to sleep |
| 5 | `IsDataReady()` | `S EE A 03 A Sr EF A [70] N P` | 4 | unchanged |
| 6 | `ReadResult()` | `S EE A 04 A Sr EF A [6 bytes] N P` | 9 | ready bits cleared |
| 7 | compensation + range check | none (pure math in C#) | 0 | — |

Five transactions (#1, 2, 3, 5, 6), about **24 bytes** on the wire (~2.5 ms at 100 kHz), and **19 ms** of waiting.
The sample's loop then calls `TryReadAltitude`, which adds a sixth transaction (another 6-byte data read). Almost all of `Read()`'s time is the
chip measuring, not the bus.

---

## 7. See it for yourself on the Pi (30 minutes)

**Rule: two Linux tools let you verify every claim above without changing any code.**

### 7.1 Watch the real system calls with `strace`

`strace` prints every system call a program makes. On the Pi:

```bash
sudo apt install -y strace
cd ~/bmp3xx-sample
strace -f -e trace=openat,ioctl,close ./Bmp3xx.Samples 2>&1 | grep -E 'i2c|0x707|I2C_RDWR' | head -40
```

What to look for (⚠️ until you see it):

- `openat(…, "/dev/i2c-1", O_RDWR) = 3` (or another small number): **step 2 of §2.3**. That number is the fd.
- `ioctl(3, I2C_FUNCS, …)`: the capability check.
- A burst of `ioctl(3, I2C_RDWR, …)` lines: **one per transaction**. Count the ones before the first reading appears:
  about 11 for the constructor (§2.4), plus 6 more for the sample's three setter lines (read + write each), then
  6 per loop iteration (5 for `Read()`, 1 for `TryReadAltitude`, §6).
- At the end, `close(3)`: the teardown chain of §2.5.

(`-f` follows all of .NET's threads. Without the `grep` you'd see thousands of lines from the runtime starting up.
Older `strace` versions may print `0x707` instead of `I2C_RDWR`.)

### 7.2 Ask the adapter what it can do

```bash
i2cdetect -F 1
```

Look for `I2C   yes`: that's the `I2C_FUNC_I2C` bit, meaning you get `UnixI2cBus` (§2.3 step 5), not the fallback.

### 7.3 Make the binding's transactions by hand with `i2ctransfer`

`i2ctransfer` (part of i2c-tools, already on your Pi) sends exactly the same `I2C_RDWR` messages as `WriteReadCore`.
`w1@0x77 0x00` = "message: write 1 byte to 0x77"; `r1` = "message: read 1 byte".

```bash
i2ctransfer -y 1 w1@0x77 0x00 r1          # chip ID read, §4/§5.1: expect 0x60
i2ctransfer -y 1 w1@0x77 0x31 r21         # the 21 calibration bytes (your chip's own!)
i2ctransfer -y 1 w2@0x77 0x1C 0x00        # OSR := x1/x1
i2ctransfer -y 1 w2@0x77 0x1B 0x13        # PWR_CTRL := press_en | temp_en | forced  (start a measurement)
sleep 0.01
i2ctransfer -y 1 w1@0x77 0x03 r1          # STATUS: expect 0x70 (cmd_rdy + both data-ready)
i2ctransfer -y 1 w1@0x77 0x04 r6          # the 6 data bytes: pressure xlsb lsb msb, temperature xlsb lsb msb
i2ctransfer -y 1 w1@0x77 0x03 r1          # STATUS again: expect 0x10 (reading the data cleared the ready bits)
```

You've just done by hand what `Read()` does (§6), and the 21 calibration bytes are the real test vectors the plan
calls V1–V5. **Save the output** (`… | tee ~/i2c-by-hand.txt`) and give it to the CLI: it's most of Step 3 already.

Assemble the raw values the way `ReadResult` does (least significant byte first):
`raw = xlsb | lsb << 8 | msb << 16`. For example, bytes `0x00 0x4C 0x6C` → `0x6C4C00` = 7 097 344.

### 7.4 Optional: see the bits

With a USB logic analyzer on SDA, SCL and GND and PulseView's I2C decoder, the chip-ID read shows exactly the
`S EE A 00 A Sr EF A 60 N P` line from §5.1. Not needed, but it's the definitive proof, and a nice image for the PR.

---

## 8. Common mistakes and gotchas

| Mistake | What happens | Why |
|---|---|---|
| Disposing the `I2cDevice` yourself *and* the sensor | the second `Dispose` is harmless here, but using the sensor after you disposed the device throws `ObjectDisposedException` | ownership went to the sensor (§2.5) |
| Creating two `I2cDevice`s for the same address with `I2cDevice.Create` | both work; each opens `/dev/i2c-1` separately | `Create` doesn't use the bus's duplicate check (§2.3 side detail) |
| `Permission denied` opening `/dev/i2c-1` | `IOException` in `UnixI2cBus.Create` step 3 | your user isn't in the `i2c` group |
| Nothing at the address | `IOException: Error … performing I2C data transfer` ("Remote I/O error" or "No such device or address") on the **first** transaction, i.e. in the constructor | no ACK after the address byte (§5.1) |
| Thinking `Write` + `Read` equals `WriteRead` | works for the BMP390, but it's two transactions with a STOP in between | only `WriteRead` puts both messages in one `ioctl` (§4, §5.3) |
| Running a 64-bit build on a 32-bit userland | "No such file or directory" for a file that exists | the launcher needs the 64-bit loader (your Oct 11 lesson; `dpkg --print-architecture`, not `uname -m`) |

---

## 9. Interview relevance

- "Walk me through what happens when your code reads a sensor register" is the hardware/software-boundary version of
  "what happens when you type a URL". The layers here (managed code → native interop → system call → kernel driver
  → controller → wire) show you understand the whole stack, not just one layer.
- **P/Invoke and struct layout across architectures** (§3.3) is a real .NET skill: `StructLayout`, pointer sizes,
  `SetLastError`/`errno`. Your #2600 study and this lecture are two concrete examples.
- **Ownership and `IDisposable`** (§2.2, §2.5) comes up in every .NET code review, not just IoT.

---

## Check yourself

1. Which object owns the file descriptor for `/dev/i2c-1`, and which chain of `Dispose` calls finally closes it?
2. Why does `I2cDevice.Create` pass `shouldDisposeBus: true`?
3. In `WriteReadCore`, what's in `msgs[0]` and `msgs[1]` for a chip-ID read? What would change for the 21-byte calibration read?
4. Why does `UnixI2cBus` use `fixed` before calling the kernel?
5. Draw the wire sequence for `WriteRegister(Command, 0xB6)`. Where are the ACKs, and who sends them?
6. `i2c_msg` is 12 bytes on your Pi and 16 bytes on a 64-bit Pi OS. Why, and why doesn't the C# code need to care?
7. Your `strace` shows 6 `I2C_RDWR` calls per loop iteration of the sample. Name each one.

## Teach-back checklist

Start with the first one, in your own words:

1. **The layers:** binding → `I2cDevice` → `UnixI2cBus` → `ioctl` in libc → kernel i2c-dev → the Pi's I2C controller → wires. Only the bottom two touch hardware.
2. **What the sample creates:** settings (two numbers) → `I2cDevice.Create` opens `/dev/i2c-1` (an fd) inside a `UnixI2cBus` inside a `UnixI2cDevice` → the sensor owns the device → structs for every reading.
3. **Ownership and teardown:** `using` disposes the sensor, which disposes the device, which disposes the bus, which `close`s the fd. The garbage collector would not close it.
4. **One transaction:** `WriteRead` = two `i2c_msg`s (write register address, read N bytes) in one `ioctl(I2C_RDWR)` = START … repeated START … STOP on the wire.
5. **Address vs register:** the device address (0x77) lives in `UnixI2cDevice` and goes in `i2c_msg.addr`; the register address is the first byte you write.
6. **P/Invoke:** `[DllImport("libc")] extern int ioctl(...)`; structs mirror the C layout with `StructLayout(Sequential)` and real pointer types, so they're right on 32- and 64-bit.
7. **Pinning:** `fixed` stops the GC from moving a buffer while the kernel reads or writes it.
8. **On the wire:** `0x77` becomes `0xEE`/`0xEF`; ACK after every byte the chip receives; the Pi NACKs the last byte it reads.
9. **One `Read()`:** five transactions and ~24 bytes; nearly all its time is the chip measuring while the Pi sleeps.
10. **How to verify all of it:** `strace` for the system calls, `i2cdetect -F` for the capability, `i2ctransfer` to make the same transactions by hand.
