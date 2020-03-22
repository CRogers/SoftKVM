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
        private static readonly Head thisHead = new Head("Desktop", "HDMI (MHL) 1", "HDMI (MHL) 1");
        private static readonly Head otherHead = new Head("Macbook", "DP", "mDP");
        private static Head currentHead = thisHead;
        private static readonly ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Keyboard");

        static void Main(string[] args)
        {
            ManagementEventWatcher watcher = new ManagementEventWatcher();
            watcher.EventArrived += (sender, eventArgs) =>
            {
                switchToCorrectMonitor();
            };
            watcher.Query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");
            watcher.Start();

            Console.WriteLine("Awaiting USB keyboard additions/removals...");
            while (true)
            {
                Thread.Sleep(TimeSpan.FromDays(1));
            }
        }

        private static void switchToCorrectMonitor()
        {
            switchTo(keyboardExists() ? thisHead : otherHead);
        }

        private static void switchTo(Head head)
        {
            if (currentHead == head)
            {
                return;
            }

            head.switchTo();
            currentHead = head;
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
        public Head(string name, string screen1InputName, string screen2InputName)
        {
            this.screen1InputName = screen1InputName;
            this.screen2InputName = screen2InputName;
            this.name = name;
        }

        private String name { get;  }
        private String screen1InputName { get; }
        private String screen2InputName { get; }

        public void switchTo()
        {
            Console.WriteLine("Switching to " + name);
            Process.Start(@"C:\Program Files (x86)\Dell\Dell Display Manager\ddm.exe",
                String.Format("/1:SetActiveInput {0} /2:SetActiveInput {1}", screen1InputName, screen2InputName));
        }
    }
}