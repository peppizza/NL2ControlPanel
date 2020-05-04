using System;
using Unosquare.WiringPi;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace TCPClient
{
    public class GPIOController
    {
        public int Pin { get; set; }
        public void TestSwitch()
        {
            Pi.Init<BootstrapWiringPi>();
            var switchPin = Pi.Gpio[Pin];
            switchPin.PinMode = GpioPinDriveMode.Input;
            var isOn = switchPin.Read();
            Console.WriteLine(isOn);
        }
    }
}