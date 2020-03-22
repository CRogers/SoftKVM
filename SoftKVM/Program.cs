using System;
using System.Management;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace SoftKVM
{
    class Program
    {
        static void Main(string[] args)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Keyboard");
            foreach (var managementBaseObject in searcher.Get())
            {
                foreach (var propertyData in managementBaseObject.Properties)
                {
                    Console.WriteLine("{0}: {1}", propertyData.Name, propertyData.Value);
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }

            var behaviorSubject = new BehaviorSubject<int>(4);
            var disposable = behaviorSubject
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(foo =>
                {
                    Console.WriteLine("debounced");
                });

            ManagementEventWatcher watcher = new ManagementEventWatcher();
            watcher.EventArrived += (sender, eventArgs) =>
            {
                Console.WriteLine("Recieved {0} from {1}", args, sender);
                behaviorSubject.OnNext(4);
            };
            watcher.Query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");
            watcher.Start();

            Thread.Sleep((int) Math.Pow(2, 31) - 1);
        }
    }
}