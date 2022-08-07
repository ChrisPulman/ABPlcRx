![License](https://img.shields.io/github/license/ChrisPulman/ABPlcRx.svg) [![Build](https://github.com/ChrisPulman/ABPlcRx/actions/workflows/BuildOnly.yml/badge.svg)](https://github.com/ChrisPulman/ABPlcRx/actions/workflows/BuildOnly.yml) ![Nuget](https://img.shields.io/nuget/dt/ABPlcRx?color=pink&style=plastic) [![NuGet](https://img.shields.io/nuget/v/ABPlcRx.svg?style=plastic)](https://www.nuget.org/packages/ABPlcRx)

<p align="left">
  <a href="https://github.com/ChrisPulman/ABPlcRx">
    <img alt="ABPlcRx" src="https://github.com/ChrisPulman/ABPlcRx/blob/main/Images/logo.png" width="200"/>
  </a>
</p>


# ABPlcRx

## A Reative Allen Bradley library Built on top of [libplctag](https://github.com/libplctag/libplctag)


## WARNING - DISCLAIMER

Note: **PLCs control many kinds of equipment and loss of property, production or even life can happen if mistakes in programming or access are made.  Always use caution when accessing or programming PLCs!**

We make no claims or warrants about the suitability of this code for any purpose.

Be careful!

#### PLC Support

- support for Rockwell/Allen-Bradley ControlLogix(tm) PLCs via CIP-EtherNet/IP (CIP/EIP or EIP).
  - read/write 8, 16, 32, and 64-bit signed and unsigned integers.
  - read/write single bits/booleans.
  - read/write 32-bit and 64-bit IEEE format (little endian) floating point.
  - raw support for user-defined structures (you need to pull out the data piece by piece)
  - read/write arrays of the above.
  - multiple-request support per packet.
  - packet size negotiation with newer firmware (version 20+) and hardware.
  - tag listing, both controller and program tags.
- support for Rockwell/Allen-Bradley Micro 850 PLCs.
  - Support as for ControlLogix where possible.
- support for older Rockwell/Allen-Bradley such as PLC-5 PLCs (Ethernet upgraded to support Ethernet/IP), SLC 500 and MicroLogix with Ethernet via CIP.
  - read/write of 16-bit INT.
  - read/write of 32-bit floating point.
  - read/write of arrays of the above (arrays not tested on SLC 500).
- support for older Rockwell/Allen-Bradley PLCs accessed over a DH+ bridge (i.e. a LGX chassis with a DHRIO module) such as PLC/5, SLC 500 and MicroLogix.
  - read/write of 16-bit INT.
  - read/write of 32-bit floating point.
  - read/write of arrays of the above.
- extensive example code.  Including
  - tag listing.
  - setting up and handling callbacks.
  - logging data from multiple tags.
  - reading and writing tags from the command line.
  - getting and setting individual bits as tags.
- Support for Omron NX/NJ series PLCs as for Allen-Bradley Micro800.
- Support for Modbus TCP.

```c#
// Create PLC
var microLogix = new ABPlcRx(PlcType.SLC, "172.16.17.4", TimeSpan.FromMilliseconds(500));
_disposables.Add(microLogix);

// Add tags to PLC - Variable can be any name and is used as a Key for further functions.
//                 - TagName can be any valid AB tag relevant to the PLC Type connectedz.
//                 - The Tag Type must be a short to read a 16 bit array of bool, use the bit to specify which bit to use.
//                 - TagGroup can be any value to group tags together providing the ability to read or write a group of tags.
microLogix.AddUpdateTagItem<short>("LightOn", "B3:3", "Default");

// Subscribe to tag updates
_disposables.Add(microLogix.Observe<bool>("LightOn", 0).Subscribe(value => Console.WriteLine($"B3:3/0 = {value}")));

// Update tag value (will be sent to PLC)
var current = !microLogix.Value<bool>("LightOn", 0);
microLogix.Value<bool>("LightOn", current, 0);
Console.Out.WriteLine($"Written {current} to PLC B3:3/0");
```
