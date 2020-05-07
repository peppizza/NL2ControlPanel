using System;
using TCPClient;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;
using System.Collections.Generic;
using DotNetEnv;

namespace TCPConsole
{
    class Program
    {
        private static int Port = 15151;
        private static bool blinkleftAmber, blinkrightAmber, blinkleftGreen, blinkrightGreen = false;
        private static bool stop;
        private static uint speed;
        public static void Main(string[] args)
        {
            Env.Load();
            string Server = Env.GetString("IP");
            Pi.Init<BootstrapWiringPi>();
            Dictionary<string, IGpioPin> buttons = new Dictionary<string, IGpioPin>
            {
                {"amberleftbutton", Pi.Gpio[25]},
                {"amberrightbutton", Pi.Gpio[5]},
                {"greenleftbutton", Pi.Gpio[12]},
                {"greenrightbutton", Pi.Gpio[6]},
                {"auto", Pi.Gpio[19]},
                {"man", Pi.Gpio[20]}
            };
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
            while (!buttons["auto"].Read() || !buttons["man"].Read())
            {
                if (buttons["auto"].Read())
                {
                    automan = true;
                    speed = 1000;
                    break;
                }

                if (buttons["man"].Read())
                {
                    speed = 100;
                    break;
                }
            }
            Intro(new NL2TelemetryClient(Server, Port), automan);

            //LEDBlink(automan);
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

        //static void LEDBlink(bool automan)
        //{
        //private Dictionary<string, IGpioPin> lights = new Dictionary<string, IGpioPin>
        //{
        //    {"amberleftlight", Pi.Gpio[21]},
        //    {"amberrightlight", Pi.Gpio[12]},
        //    {"greenleftlight", Pi.Gpio[16]},
        //    {"greenrightlight", Pi.Gpio[5]}
        //};
        //    Pi.Init<BootstrapWiringPi>();
        //    var isOn = false;
        //    while (!stop)
        //    {
        //        if (blinkleftAmber)
        //        {
        //            amberleftlight.Write(isOn);
        //        }

        //        if (blinkrightAmber)
        //        {
        //            amberrightlight.Write(isOn);
        //        }

        //        if (blinkleftGreen)
        //        {
        //            greenleftlight.Write(isOn);
        //        }

        //        if (blinkrightGreen)
        //        {
        //            greenrightlight.Write(isOn);
        //        }

        //        isOn = !isOn;
        //        Pi.Timing.SleepMilliseconds(speed);
        //    }
        //}
    }
}