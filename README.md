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

// Add tags to PLC
microLogix.AddUpdateTagItem<short>("B3:3", "Default");

// Subscribe to tag updates
_disposables.Add(microLogix.Observe<bool>("B3:3", 0).Subscribe(value => Console.WriteLine($"B3:3/0 = {value}")));

// Update tag value (will be sent to PLC)
var current = !microLogix.Value<bool>("B3:3", 0);
microLogix.Value<bool>("B3:3", current, 0);
Console.Out.WriteLine($"Written {current} to PLC B3:3/0");
```