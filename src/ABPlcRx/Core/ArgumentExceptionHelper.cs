// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ABPlcRx;

/// <summary>Cross-target argument guard helpers.</summary>
internal static class ArgumentExceptionHelper
{
    /// <summary>Throws when an argument is null.</summary>
    /// <typeparam name="T">The argument type.</typeparam>
    /// <param name="argument">The argument value.</param>
    /// <param name="paramName">The parameter name.</param>
    public static void ThrowIfNull<T>(T? argument, string paramName)
        where T : class
    {
        if (argument is not null)
        {
            return;
        }

        throw new ArgumentNullException(paramName);
    }

    /// <summary>Throws when a string argument is null, empty, or whitespace.</summary>
    /// <param name="argument">The argument value.</param>
    /// <param name="paramName">The parameter name.</param>
    public static void ThrowIfNullOrWhiteSpace(string? argument, string paramName)
    {
        if (argument?.Trim().Length > 0)
        {
            return;
        }

        throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
    }
}
