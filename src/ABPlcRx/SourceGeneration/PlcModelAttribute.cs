// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ABPlcRx.SourceGeneration;

/// <summary>Marks a partial type as a PLC reactive stream model.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PlcModelAttribute : Attribute;
