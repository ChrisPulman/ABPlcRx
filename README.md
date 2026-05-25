![License](https://img.shields.io/github/license/ChrisPulman/ABPlcRx.svg) [![Build](https://github.com/ChrisPulman/ABPlcRx/actions/workflows/BuildOnly.yml/badge.svg)](https://github.com/ChrisPulman/ABPlcRx/actions/workflows/BuildOnly.yml) ![Nuget](https://img.shields.io/nuget/dt/ABPlcRx?color=pink&style=plastic) [![NuGet](https://img.shields.io/nuget/v/ABPlcRx.svg?style=plastic)](https://www.nuget.org/packages/ABPlcRx)

![Alt](https://repobeats.axiom.co/api/embed/24d527be4f32c7d50e5e907b50687874772158ee.svg "Repobeats analytics image")

<p align="left">
  <a href="https://github.com/ChrisPulman/ABPlcRx">
    <img alt="ABPlcRx" src="Images/logo.png" width="200"/>
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
- Async reactive APIs (IObservableAsync) through ReactiveUI.Extensions on .NET 8+
- Read/write primitives: 8/16/32/64‑bit signed/unsigned, 32/64‑bit float
- Bit addressing helpers for coil/word bits
- String and structure support (libplctag style)
- Bulk read/write across groups
- Health monitoring (Ping/ObservePing)
- Source generator attributes for typed PLC stream models
- TUnit tests running on Microsoft Testing Platform

Getting started
Installation
- Install the NuGet package:
  - Package Manager: Install-Package ABPlcRx
  - .NET CLI: dotnet add package ABPlcRx
- The ABPlcRx package includes its source generator analyzer; no separate generator package is required for normal NuGet consumption.
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
lgx.Observe<int>("Counter").Subscribe(v => Console.WriteLine($"Counter={v}"));

// Increment and write
lgx.Value("Counter", lgx.Value<int>("Counter") + 1);

// Standard Logix BOOL tag named MachineReady
lgx.AddUpdateTagItem<bool>("Ready", "MachineReady", "Default");
lgx.Observe<bool>("Ready").Subscribe(v => Console.WriteLine($"Ready={v}"));

var ready = !lgx.Value<bool>("Ready");
lgx.Value("Ready", ready);
```

Reactive API highlights
- Observe<T>(variable, bit = -1): stream values on change, supports late‑added tags
- ObserveAsync<T>(variable, bit = -1): async-native stream using ReactiveUI.Extensions.Async on .NET 8+
- ObserveMany(params string[] variables): latest values as a dictionary
- ObserveManyAsync(params string[] variables): async-native latest value dictionaries
- ObserveGroup(groupName): emits tag objects in a group when they change
- ObserveGroupAsync(groupName): async-native group stream
- ObserveSampled<T>(variable, sampleInterval, bit, scheduler): sampled stream for rate limiting
- ObserveSampledAsync<T>(variable, sampleInterval, bit, scheduler): async-native sampled stream
- ObserveErrors(): only tag operations that returned an error
- ObserveErrorsAsync(): async-native error stream
- CreateWriter<T>(variable, bit): returns an IObserver<T> that writes on OnNext

Async observables
```csharp
using ReactiveUI.Extensions.Async;

var counter = lgx.ObserveAsync<int>("Counter");

// IObservableAsync<T> can use ReactiveUI.Extensions.Async operators,
// including Select, Where, Merge, CombineLatest, Retry, Timeout, Publish,
// ReplayLatest, ToObservable, and ToObservableAsync.
var activeCounter =
    counter
        .Where(value => value > 0)
        .Select(value => $"Counter={value}");
```

Source generated stream models
```csharp
using ABPlcRx.SourceGeneration;

[PlcModel]
[PlcTag(typeof(int), "Counter", "MyDINT")]
[PlcTag(typeof(bool), "LightOn", "B3:3", Bit = 0)]
public partial class MachineTags
{
}

var tags = new MachineTags();
using var binding = tags.AttachPlcStreams(slc);

tags.CounterObservable.Subscribe(value => Console.WriteLine($"Counter={value}"));
tags.LightOnObservable.Subscribe(value => Console.WriteLine($"LightOn={value}"));

// On .NET 8+ the generator also emits IObservableAsync<T> streams.
var asyncLightOn = tags.LightOnObservableAsync;
```

The generator creates a property for each class-level `PlcTag` attribute, registers tags in `AttachPlcStreams`, keeps the generated property updated from the observable subscription, and exposes both `PropertyNameObservable` and `PropertyNameObservableAsync` on .NET 8+. Boolean tags without a `Bit` value are registered as native `bool` tags. Boolean bit tags with `Bit` set are registered as `short` tags and exposed as `bool` values.

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
  - ObserveAsync<T>(variable, bit = -1) on .NET 8+
  - ObserveMany(params string[] variables)
  - ObserveManyAsync(params string[] variables) on .NET 8+
  - ObserveGroup(groupName)
  - ObserveGroupAsync(groupName) on .NET 8+
  - ObserveSampled<T>(variable, sampleInterval, bit = -1, scheduler = null)
  - ObserveSampledAsync<T>(variable, sampleInterval, bit = -1, scheduler = null) on .NET 8+
  - ObserveErrors()
  - ObserveErrorsAsync() on .NET 8+
  - CreateWriter<T>(variable, bit = -1)
  - Value<T>(variable, bit = -1) / Value<T>(variable, value, bit = -1)
  - Read()/Read(variable) and Write()/Write(variable)
  - Ping(bool echo = false), PingAsync(...), ObservePing(interval,...)
  - ObservePingAsync(interval,...) on .NET 8+

Testing
- Tests are in `src/ABPlcRx.Tests`.
- The suite uses TUnit with Microsoft Testing Platform; `global.json` sets `"test": { "runner": "Microsoft.Testing.Platform" }`.
- Run all tests with:
```bash
dotnet test src/ABPlcRx.sln
```

Data types and bit access
- Logix/CompactLogix/Micro800 standard `BOOL` tags can be created directly with `AddUpdateTagItem<bool>("Ready", "MachineReady")` and read or written with `Value<bool>("Ready")`.
- To treat a single bit in an SLC/PLC word as a boolean, create the tag as `short` and use the `bit` parameter (0‑15).
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
