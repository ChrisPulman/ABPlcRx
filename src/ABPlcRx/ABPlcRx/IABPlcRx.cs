// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

namespace ABPlcRx
{
    /// <summary>
    /// IRxAB.
    /// </summary>
    /// <seealso cref="ICancelable" />
    public interface IABPlcRx : ICancelable
    {
        /// <summary>
        /// Gets the observe all.
        /// </summary>
        /// <value>
        /// The observe all.
        /// </value>
        IObservable<IPlcTag?> ObserveAll { get; }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        IObservable<string> Status { get; }

        /// <summary>
        /// Adds the update tag item.
        /// </summary>
        /// <typeparam name="T">The PLC type.</typeparam>
        /// <param name="tagName">Name of the tag.</param>
        /// <param name="tagGroup">The tag group.</param>
        void AddUpdateTagItem<T>(string? tagName, string tagGroup);

        /// <summary>
        /// Observes the specified variable.
        /// </summary>
        /// <typeparam name="T">The PLC type.</typeparam>
        /// <param name="variable">The variable.</param>
        /// <param name="bit">The bit.</param>
        /// <returns>
        /// A value of T.
        /// </returns>
        IObservable<T?> Observe<T>(string? variable, int bit = -1);

        /// <summary>
        /// Values the specified variable.
        /// </summary>
        /// <typeparam name="T">The PLC type.</typeparam>
        /// <param name="variable">The variable.</param>
        /// <param name="bit">The bit.</param>
        /// <returns>
        /// A value of T.
        /// </returns>
        T? Value<T>(string? variable, int bit = -1);

        /// <summary>
        /// Values the specified variable.
        /// </summary>
        /// <typeparam name="T">The PLC type.</typeparam>
        /// <param name="variable">The variable.</param>
        /// <param name="value">The value.</param>
        /// <param name="bit">The bit.</param>
        void Value<T>(string? variable, T? value, int bit = -1);
    }
}
