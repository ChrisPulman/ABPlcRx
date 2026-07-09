// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ABPlcRx;

/// <summary>Allen Bradley PLC processor family.</summary>
public enum PlcType
{
    /// <summary>ControlLogix / CompactLogix Control Systems.</summary>
    LGX,

    /// <summary>SLC / MicroLogix Controller.</summary>
    SLC,

    /// <summary>PLC-5 Controllers.</summary>
    PLC5,
}
