// Copyright (c) 2022-2026 Chris Pulman. All rights reserved.
// Chris Pulman licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using ConsoleTools;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Signals;

namespace ABPlcRx.TestApp;

/// <summary>Console sample application entry point.</summary>
internal static class Program
{
    /// <summary>Sample PLC polling interval in milliseconds.</summary>
    private const int PollIntervalMilliseconds = 500;

    /// <summary>Tracks sample application subscriptions.</summary>
    private static MultipleDisposable _disposables = new();

    /// <summary>Runs the sample application.</summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        new ConsoleMenu(args, level: 0)
           .Add(nameof(MicroLogix), MicroLogix)
           .Add("Close", ConsoleMenu.Close)
           .Configure(config =>
           {
               config.Title = "MicroLogix ABPlcRx Example";
               config.EnableWriteTitle = true;
               config.WriteHeaderAction = () => Console.Out.WriteLine("Please select a mode:");
           })
           .Show();
    }

    /// <summary>Runs the MicroLogix sample.</summary>
    private static void MicroLogix()
    {
        _disposables.Add(Signal.Timer(TimeSpan.FromSeconds(1)).Subscribe(initialTick =>
        {
            // Create PLC
            var microLogix = new ABPlcRx(PlcType.SLC, "172.16.17.4", TimeSpan.FromMilliseconds(PollIntervalMilliseconds));
            _disposables.Add(microLogix);

            // Disable Auto Write NOTE: defaults to true.
            microLogix.AutoWriteValue = false;

            // Add tags to PLC - Variable can be any name and is used as a Key for further functions.
            //                 - TagName can be any valid AB tag relevant to the PLC Type connectedz.
            //                 - The Tag Type must be a short to read a 16 bit array of bool, use the bit to specify which bit to use.
            //                 - TagGroup can be any value to group tags together providing the ability to read or write a group of tags.
            microLogix.AddUpdateTagItem<short>("Variable1", "B3:3", "Default");

            // Subscribe to tag updates.
            _disposables.Add(microLogix.Observe<bool>("Variable1", 0).Subscribe(value => Console.Out.WriteLine($"B3:3/0 = {value}")));

            _disposables.Add(Signal.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1)).Subscribe(writeTick =>
            {
                // Update tag value (will be sent to PLC)
                var current = !microLogix.Value<bool>("Variable1", 0);
                microLogix.Value<bool>("Variable1", current, 0);

                // Write all tags to PLC.
                if (!microLogix.AutoWriteValue)
                {
                    GC.KeepAlive(microLogix.Write());
                }

                Console.Out.WriteLine($"Written {current} to PLC B3:3/0");
            }));
        }));
        WaitForExit();
    }

    /// <summary>Waits for the user to exit the sample.</summary>
    /// <param name="message">Optional message to print.</param>
    /// <param name="clear">True to clear the console before waiting.</param>
    private static void WaitForExit(string? message = null, bool clear = true)
    {
        if (clear)
        {
            Console.Clear();
        }

        if (message is not null)
        {
            Console.Out.WriteLine(message);
        }

        Console.Out.WriteLine("Press 'Escape' or 'E' to exit.");
        Console.Out.WriteLine();

        while (Console.ReadKey(true).Key is ConsoleKey key && !(key == ConsoleKey.Escape || key == ConsoleKey.E))
        {
            Thread.Sleep(1);
        }

        _disposables.Dispose();
        _disposables = new();
    }
}
