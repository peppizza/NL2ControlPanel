using System;
using TCPClient;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;
using System.Collections.Generic;

namespace TCPConsole
{
    class Program
    {
        private static string Server = "172.27.153.68";
        private static int Port = 15151;
        private static bool blinkleftAmber, blinkrightAmber, blinkleftGreen, blinkrightGreen = false;
        private static bool stop;
        private static uint speed;
        private static readonly Dictionary<string, IGpioPin> buttons = new Dictionary<string, IGpioPin>
        {
            {"amberleftbutton", Pi.Gpio[26]},
            {"amberrightbutton", Pi.Gpio[6]},
            {"greenleftbutton", Pi.Gpio[13]},
            {"greenrightbutton", Pi.Gpio[7]},
            {"auto", Pi.Gpio[19]},
            {"man", Pi.Gpio[20]}
        };
        private static readonly Dictionary<string, IGpioPin> lights = new Dictionary<string, IGpioPin>
        {
            {"amberleftlight", Pi.Gpio[21]},
            {"amberrightlight", Pi.Gpio[12]},
            {"greenleftlight", Pi.Gpio[16]},
            {"greenrightlight", Pi.Gpio[5]}
        };

        public static void Main(string[] args)
        {
            Pi.Init<BootstrapWiringPi>();
            foreach (var button in buttons)
            {
                button.Value.PinMode = GpioPinDriveMode.Input;
            }

            if (buttons["auto"].Read() || buttons["man"].Read())
            {
                Console.WriteLine("TURN OFF PANEL BEFORE STARTING");
                Environment.Exit(0);
            }

            bool automan = false;
            while (!auto.Read() || !man.Read())
            {
                if (auto.Read())
                {
                    automan = true;
                    speed = 1000;
                    break;
                }

                if (man.Read())
                {
                    speed = 100;
                    break;
                }
            }
            Intro(new NL2TelemetryClient(Server, Port), automan);

            LEDBlink(automan);
        }
        static void Intro(NL2TelemetryClient client, bool automan)
        {
            stop = false;
            if (automan)
            {
                Console.WriteLine("starting up...");
                client.SendCommand("idle");
            } 
            else
            {
                Console.WriteLine("Entering manual mode...");
                client.SendCommand("idle");
            }
            Console.WriteLine("done!");
        }

        static void LEDBlink(bool automan)
        {
            Pi.Init<BootstrapWiringPi>();
            var isOn = false;
            while (!stop)
            {
                if (blinkleftAmber)
                {
                    amberleftlight.Write(isOn);
                }

                if (blinkrightAmber)
                {
                    amberrightlight.Write(isOn);
                }

                if (blinkleftGreen)
                {
                    greenleftlight.Write(isOn);
                }

                if (blinkrightGreen)
                {
                    greenrightlight.Write(isOn);
                }

                isOn = !isOn;
                Pi.Timing.SleepMilliseconds(speed);
            }
        }
    }
}