using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace SoftKVM
{
    class Program
    {
        private static readonly String keyboardDeviceId = @"USB\VID_05AC&PID_024F&MI_00\8&1145D480&0&0000";
        private static readonly Head thisHead = new Head("HDMI (MHL) 1", "HDMI (MHL) 1");
        private static readonly Head otherHead = new Head("DP", "mDP");
        private static Head currentHead = thisHead;
        private static readonly ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Keyboard");

        static void Main(string[] args)
        {
            var behaviorSubject = new BehaviorSubject<int>(4);
            var disposable = behaviorSubject
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(foo =>
                {
                    if (keyboardExists())
                    {
                        if (currentHead == thisHead)
                        {
                            return;
                        }
                        Console.WriteLine("changing display back to our screen");
                        thisHead.switchTo();
                        currentHead = thisHead;
                    }
                    else
                    {
                        if (currentHead == otherHead)
                        {
                            return;
                        }
                        Console.WriteLine("changing display back to other screen");
                        otherHead.switchTo();
                        currentHead = otherHead;
                    }
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

        private static bool keyboardExists()
        {
            foreach (var managementBaseObject in searcher.Get())
            {
                if ((string) managementBaseObject.Properties["DeviceId"].Value == keyboardDeviceId)
                {
                    return true;
                }
            }
            
            return false;
        }
    }

    class Head
    {
        public Head(string screen1InputName, string screen2InputName)
        {
            this.screen1InputName = screen1InputName;
            this.screen2InputName = screen2InputName;
        }

        private String screen1InputName { get; }
        private String screen2InputName { get; }

        public void switchTo()
        {
            Process.Start(@"C:\Program Files (x86)\Dell\Dell Display Manager\ddm.exe",
                String.Format("/1:SetActiveInput {0} /2:SetActiveInput {1}", screen1InputName, screen2InputName));
        }
    }
}