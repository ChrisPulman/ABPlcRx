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
            _disposables.Add(Observable.Delay(
                Observable.Start(
                () =>
                    {
                        // Create PLC
                        var microLogix = new ABPlcRx(PlcType.SLC, "172.16.17.4", TimeSpan.FromMilliseconds(500));
                        _disposables.Add(microLogix);

                        // Add tags to PLC
                        microLogix.AddUpdateTagItem<short>("B3:3", "Default");

                        // Subscribe to tag updates
                        _disposables.Add(microLogix.Observe<bool>("B3:3", 0).Subscribe(value => Console.WriteLine($"B3:3/0 = {value}")));

                        _disposables.Add(Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ =>
                        {
                            // Update tag value (will be sent to PLC)
                            var current = !microLogix.Value<bool>("B3:3", 0);
                            microLogix.Value<bool>("B3:3", current, 0);
                            Console.Out.WriteLine($"Written {current} to PLC B3:3/0");
                        }));
                    }),
                TimeSpan.FromSeconds(1))
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
