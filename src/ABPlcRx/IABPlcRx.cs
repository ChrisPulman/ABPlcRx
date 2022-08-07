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
        /// Gets or sets a value indicating whether [scan enabled].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [scan enabled]; otherwise, <c>false</c>.
        /// </value>
        bool ScanEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [automatic write value].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [automatic write value]; otherwise, <c>false</c>.
        /// </value>
        bool AutoWriteValue { get; set; }

        /// <summary>
        /// Adds the update tag item.
        /// </summary>
        /// <typeparam name="T">The tag type.</typeparam>
        /// <param name="tagName">Name of the PLC tag.</param>
        /// <exception cref="System.ArgumentNullException">tagName.</exception>
        /// <exception cref="System.Exception">Please use type of short, then use bool for other operations and set the bit number.</exception>
        void AddUpdateTagItem<T>(string tagName);

        /// <summary>
        /// Adds the update tag item.
        /// </summary>
        /// <typeparam name="T">The tag type.</typeparam>
        /// <param name="variable">The variable, this can be any non null name you wish to use.</param>
        /// <param name="tagName">Name of the plc tag.</param>
        /// <exception cref="System.ArgumentNullException">tagName.</exception>
        /// <exception cref="System.Exception">Please use type of short, then use bool for other operations and set the bit number.</exception>
        void AddUpdateTagItem<T>(string variable, string tagName);

        /// <summary>
        /// Adds the update tag item.
        /// </summary>
        /// <typeparam name="T">The tag type.</typeparam>
        /// <param name="variable">The variable, this can be any non null name you wish to use.</param>
        /// <param name="tagName">Name of the plc tag.</param>
        /// <param name="tagGroup">The tag group.</param>
        /// <exception cref="System.ArgumentNullException">tagName.</exception>
        /// <exception cref="System.Exception">Please use type of short, then use bool for other operations and set the bit number.</exception>
        void AddUpdateTagItem<T>(string variable, string tagName, string tagGroup);

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

        /// <summary>
        /// Writes all tags in this instance.
        /// </summary>
        /// <returns>A PlcTagResult.</returns>
        IEnumerable<PlcTagResult> Write();

        /// <summary>
        /// Writes the specified variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>A PlcTagResult.</returns>
        PlcTagResult? Write(string? variable);

        /// <summary>
        /// Reads all tags in this instance.
        /// </summary>
        /// <returns>A PlcTagResult.</returns>
        IEnumerable<PlcTagResult> Read();

        /// <summary>
        /// Reads the specified variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>A PlcTagResult.</returns>
        PlcTagResult? Read(string? variable);
    }
}
