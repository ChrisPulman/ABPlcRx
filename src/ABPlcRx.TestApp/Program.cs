// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ConsoleTools;

namespace ABPlcRx.TestApp
{
    internal static class Program
    {
        private static CompositeDisposable _disposables = new();

        private static void Main(string[] args)
        {
            new ConsoleMenu(args, level: 0)
               .Add("MicroLogix", MicroLogix)
               .Add("Close", ConsoleMenu.Close)
               .Configure(config =>
               {
                   config.Title = "MicroLogix ABPlcRx Example";
                   config.EnableWriteTitle = true;
                   config.WriteHeaderAction = () => Console.WriteLine("Please select a mode:");
               })
               .Show();
        }

        private static void MicroLogix()
        {
            _disposables.Add(Observable.Start(
                () =>
                    {
                        // Create PLC
                        var microLogix = new ABPlcRx(PlcType.SLC, "172.16.17.4", TimeSpan.FromMilliseconds(500));
                        _disposables.Add(microLogix);

                        // Disable Auto Write NOTE: defaults to true.
                        microLogix.AutoWriteValue = false;

                        // Add tags to PLC - Variable can be any name and is used as a Key for further functions.
                        //                 - TagName can be any valid AB tag relevant to the PLC Type connectedz.
                        //                 - The Tag Type must be a short to read a 16 bit array of bool, use the bit to specify which bit to use.
                        //                 - TagGroup can be any value to group tags together providing the ability to read or write a group of tags.
                        microLogix.AddUpdateTagItem<short>("Variable1", "B3:3", "Default");

                        // Subscribe to tag updates.
                        _disposables.Add(microLogix.Observe<bool>("Variable1", 0).Subscribe(value => Console.WriteLine($"B3:3/0 = {value}")));

                        _disposables.Add(Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ =>
                        {
                            // Update tag value (will be sent to PLC)
                            var current = !microLogix.Value<bool>("Variable1", 0);
                            microLogix.Value<bool>("Variable1", current, 0);

                            // Write all tags to PLC.
                            if (!microLogix.AutoWriteValue)
                            {
                                microLogix.Write();
                            }

                            Console.Out.WriteLine($"Written {current} to PLC B3:3/0");
                        }));
                    }).Delay(TimeSpan.FromSeconds(1))
                .Subscribe());
            WaitForExit();
        }

        private static void WaitForExit(string? message = null, bool clear = true)
        {
            if (clear)
            {
                Console.Clear();
            }

            if (message != null)
            {
                Console.WriteLine(message);
            }

            Console.WriteLine("Press 'Escape' or 'E' to exit.");
            Console.WriteLine();

            while (Console.ReadKey(true).Key is ConsoleKey key && !(key == ConsoleKey.Escape || key == ConsoleKey.E))
            {
                Thread.Sleep(1);
            }

            _disposables.Dispose();
            _disposables = new();
        }
    }
}
