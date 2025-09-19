![License](https://img.shields.io/github/license/ChrisPulman/ABPlcRx.svg) [![Build](https://github.com/ChrisPulman/ABPlcRx/actions/workflows/BuildOnly.yml/badge.svg)](https://github.com/ChrisPulman/ABPlcRx/actions/workflows/BuildOnly.yml) ![Nuget](https://img.shields.io/nuget/dt/ABPlcRx?color=pink&style=plastic) [![NuGet](https://img.shields.io/nuget/v/ABPlcRx.svg?style=plastic)](https://www.nuget.org/packages/ABPlcRx)

![Alt](https://repobeats.axiom.co/api/embed/24d527be4f32c7d50e5e907b50687874772158ee.svg "Repobeats analytics image")

<p align="left">
  <a href="https://github.com/ChrisPulman/ABPlcRx">
    <img alt="ABPlcRx" src="https://github.com/ChrisPulman/ABPlcRx/blob/main/Images/logo.png" width="200"/>
  </a>
</p>

# ABPlcRx

A reactive Allen‑Bradley PLC client built on top of [libplctag](https://github.com/libplctag/libplctag). It provides a simple, high‑performance, reactive API for reading/writing tags from Rockwell/Allen‑Bradley controllers.

Warning – Disclaimer
PLCs control equipment. Mistakes can cause loss of property, production, or life. Use extreme caution. No warranty of suitability is provided.

Supported PLC families (via libplctag)
- ControlLogix/CompactLogix (LGX) over CIP EtherNet/IP
- Micro800 family where supported by libplctag
- PLC‑5, SLC 500, MicroLogix (Ethernet/ENI/DH+ bridging where supported)
- Additional families supported by libplctag may be usable

Core features
- Create tags and group them for bulk operations
- Reactive APIs (IObservable) for on‑change updates
- Read/write primitives: 8/16/32/64‑bit signed/unsigned, 32/64‑bit float
- Bit addressing helpers for coil/word bits
- String and structure support (libplctag style)
- Bulk read/write across groups
- Health monitoring (Ping/ObservePing)

Getting started
Installation
- Install the NuGet package:
  - Package Manager: Install-Package ABPlcRx
  - .NET CLI: dotnet add package ABPlcRx
- ABPlcRx depends on libplctag; the NuGet dependency brings required bindings.

Basic concepts
- Variable: your app’s key for a tag (free‑form string)
- TagName: the PLC’s address/name for the tag (e.g., B3:3, N7:0, MyTag)
- TagGroup: logical group to batch operations (e.g., “Default”, “Motion”)
- Types and bits: to read/write a bit, create a tag as short (Int16) and use bit index 0‑15

Quick start
```csharp
using ABPlcRx;
using System;
using System.Reactive.Disposables;

var disposables = new CompositeDisposable();

// SLC/PLC5/MicroLogix example (500ms scan)
var slc = new ABPlcRx(PlcType.SLC, "192.168.1.50", TimeSpan.FromMilliseconds(500));
disposables.Add(slc);

// Create a word tag and use bit addressing (B3:3/0)
slc.AddUpdateTagItem<short>("LightOn", "B3:3", "Default");

// Observe changes (bool via bit 0)
disposables.Add(
    slc.Observe<bool>("LightOn", bit: 0)
       .Subscribe(v => Console.WriteLine($"LightOn = {v}"))
);

// Toggle the bit and write
var current = !slc.Value<bool>("LightOn", bit: 0);
slc.Value("LightOn", current, bit: 0); // AutoWriteValue=true writes immediately
Console.WriteLine($"Wrote {current} -> B3:3/0");
```

ControlLogix/CompactLogix (LGX) example
```csharp
// For LGX you must provide a path (default "1,0" = backplane, slot 0)
var lgx = new ABPlcRx(PlcType.LGX, "192.168.1.60", TimeSpan.FromMilliseconds(200),
                      timeOut: TimeSpan.FromSeconds(2), path: "1,0");

// Controller tag named MyDINT
lgx.AddUpdateTagItem<int>("Counter", "MyDINT", "Default");

// Observe numeric values
glx.Observe<int>("Counter").Subscribe(v => Console.WriteLine($"Counter={v}"));

// Increment and write
glx.Value("Counter", lgx.Value<int>("Counter") + 1);
```

Reactive API highlights
- Observe<T>(variable, bit = -1): stream values on change, supports late‑added tags
- ObserveMany(params string[] variables): latest values as a dictionary
- ObserveGroup(groupName): emits tag objects in a group when they change
- ObserveSampled<T>(variable, sampleInterval, bit, scheduler): sampled stream for rate limiting
- ObserveErrors(): only tag operations that returned an error
- CreateWriter<T>(variable, bit): returns an IObserver<T> that writes on OnNext

Examples
Observe multiple variables
```csharp
// Emits { "LightOn": true, "Counter": 42 }
slc.ObserveMany("LightOn", "Counter")
   .Subscribe(dict => Console.WriteLine(string.Join(", ", dict.Select(kv => $"{kv.Key}={kv.Value}"))));
```

Group operations and bulk I/O
```csharp
// Group creation is implicit via AddUpdateTagItem
slc.AddUpdateTagItem<short>("Alarm", "B3:10", "Safety");
slc.AddUpdateTagItem<short>("Guard", "B3:11", "Safety");

// Bulk read/write across all groups
var results = slc.Read();
var wrote = slc.Write();
```

Health monitoring
```csharp
// One‑off ping
var ok = slc.Ping();

// Observe ping results every 2 seconds
slc.ObservePing(TimeSpan.FromSeconds(2))
   .Subscribe(alive => Console.WriteLine($"PLC reachable: {alive}"));
```

Advanced: writing with an observer
```csharp
var writer = slc.CreateWriter<bool>("LightOn", bit: 0);
writer.OnNext(true);  // writes and commits
```

Configuration and options
- ScanEnabled: enable/disable background scanning by group
- AutoWriteValue: when true (default), setting Value(tag) writes immediately
- Timeout: communications timeout (ms) via constructor timeOut
- Groups: use the tagGroup parameter to logically separate tags

API surface (high level)
- ABPlcRx (implements IABPlcRx)
  - AddUpdateTagItem<T>(variable, tagName, tagGroup = "Default")
  - Observe<T>(variable, bit = -1)
  - ObserveMany(params string[] variables)
  - ObserveGroup(groupName)
  - ObserveSampled<T>(variable, sampleInterval, bit = -1, scheduler = null)
  - ObserveErrors()
  - CreateWriter<T>(variable, bit = -1)
  - Value<T>(variable, bit = -1) / Value<T>(variable, value, bit = -1)
  - Read()/Read(variable) and Write()/Write(variable)
  - Ping(bool echo = false), PingAsync(...), ObservePing(interval,...)

Data types and bit access
- To treat a single bit as a boolean, create the tag as `short` and use the `bit` parameter (0‑15).
- For numeric tags use C# primitive types: sbyte/byte/short/ushort/int/uint/long/ulong/float/double.
- Strings and structure types are supported where the PLC and libplctag support them.

Error handling
- Each read/write yields a PlcTagResult with StatusCode (see PlcTagStatus).
- If `FailOperationRaiseException` is set true on the underlying controller, failed operations will throw `PlcTagException`.

Performance notes
- Tags are grouped internally; bulk `Read()`/`Write()` iterate groups for fewer round trips.
- Tag lookups are cached; prefer consistent `variable` keys.
- Use `ObserveSampled` to reduce update rates to UI or logs.

Troubleshooting
- LGX controllers require a valid `path` (e.g., "1,0" for backplane/slot0).
- Ensure your PLC networking, firewall, and CIP routes are reachable from your app host.
- Use `Ping()`/`ObservePing()` to monitor reachability.

License
MIT. See LICENSE.

---

**ABPlcRx** - Empowering Industrial Automation with Reactive Technology ⚡🏭
