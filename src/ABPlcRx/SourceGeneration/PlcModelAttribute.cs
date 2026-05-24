// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ABPlcRx.SourceGeneration;

/// <summary>
/// Marks a partial type as a PLC reactive stream model.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PlcModelAttribute : Attribute
{
}
