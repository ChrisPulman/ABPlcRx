// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVELIST_REACTIVE
namespace ABPlcRx.Reactive;
#else
namespace ABPlcRx;
#endif

/// <summary>Interface Tag.</summary>
/// <typeparam name="TType">The type of the type.</typeparam>
/// <seealso cref="IPlcTag" />
public interface IPlcTag<TType> : IPlcTag
{
    /// <summary>Gets or sets the value.</summary>
    /// <value>
    /// The value.
    /// </value>
    new TType? Value { get; set; }
}
