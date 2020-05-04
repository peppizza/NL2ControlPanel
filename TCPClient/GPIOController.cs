using System;
using Unosquare.WiringPi;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace TCPClient
{
    public class GpioController
    {
        public int Pin { get; set; }
        public void LedBlink()
        {
            Pi.Init<BootstrapWiringPi>();
            var blinkingPin = Pi.Gpio[4];
            blinkingPin.PinMode = GpioPinDriveMode.Output;
            var isOn = false;
            for (var i = 0; i < 20; i++)
            {
                isOn = !isOn;
                blinkingPin.Write(isOn);
                System.Threading.Thread.Sleep(500);
            }
        }
    }
}