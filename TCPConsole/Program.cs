using System;
using TCPClient;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;
using System.Collections.Generic;
using DotNetEnv;
using static System.Console;

namespace TCPConsole
{
    class Program
    {
        private const int Port = 15151;
        private static bool _stop;

        private static readonly Dictionary<string, bool> BlinkLeds = new Dictionary<string, bool>
        {
            {"blinkleftAmber", true},
            {"blinkrightAmber", true},
            {"blinkleftGreen", true},
            {"blinkrightGreen", true}
        };

        private static uint _speed;
        private static NL2TelemetryClient _client;

        public static void Main(string[] args)
        {
            Env.Load();
            Pi.Init<BootstrapWiringPi>();
            _client = new NL2TelemetryClient(Env.GetString("IP"), Port);
            var buttons = new Dictionary<string, IGpioPin>
            {
                {"amberleftbutton", Pi.Gpio[26]},
                {"amberrightbutton", Pi.Gpio[6]},
                {"greenleftbutton", Pi.Gpio[13]},
                {"greenrightbutton", Pi.Gpio[7]},
                {"auto", Pi.Gpio[19]},
                {"man", Pi.Gpio[20]}
            };
            foreach (var button in buttons)
            {
                button.Value.PinMode = GpioPinDriveMode.Input;
            }

            if (buttons["auto"].Read() || buttons["man"].Read())
            {
                WriteLine("TURN OFF PANEL BEFORE STARTING");
                _client.Close();
                Environment.Exit(0);
            }

            var automan = false;
            while (!buttons["auto"].Read() || !buttons["man"].Read())
            {
                if (buttons["auto"].Read())
                {
                    automan = true;
                    _speed = 1000;
                    break;
                }

                if (buttons["man"].Read())
                {
                    _speed = 100;
                    break;
                }
            }

            Intro(automan);

            Pi.Threading.StartThread(LedBlink);
            Pi.Threading.StartThread(GetStationState);
            while (!_stop)
            {
                if (buttons["amberleftbutton"].Read())
                {
                    WriteLine("pressed left amber button");
                }

                if (buttons["amberrightbutton"].Read())
                {
                    WriteLine("pressed right amber button");
                }

                if (buttons["greenleftbutton"].Read())
                {
                    WriteLine("pressed left green button");
                }

                if (buttons["greenrightbutton"].Read())
                {
                    WriteLine("pressed right green button");
                }

                if (!buttons["auto"].Read() && !buttons["man"].Read())
                {
                    WriteLine("stopping");
                    _client.Close();
                    _stop = true;
                }
                Pi.Timing.SleepMilliseconds(_speed);
                Clear();
            }
        }

        static void Intro(bool automan)
        {
            if (automan)
            {
                WriteLine("starting up...");
                _client.SendCommand("idle");
            }
            else
            {
                WriteLine("Entering manual mode...");
                _client.SendCommand("idle");
            }

            WriteLine("done!");
        }

        static void LedBlink()
        {
            Pi.Init<BootstrapWiringPi>();
            var lights = new Dictionary<string, IGpioPin>
            {
                {"amberleftlight", Pi.Gpio[21]},
                {"amberrightlight", Pi.Gpio[12]},
                {"greenleftlight", Pi.Gpio[16]},
                {"greenrightlight", Pi.Gpio[5]}
            };

            foreach (var light in lights)
            {
                light.Value.PinMode = GpioPinDriveMode.Output;
            }

            var isOn = false;

            while (!_stop)
            {
                if (BlinkLeds["blinkleftAmber"])
                {
                    lights["amberleftlight"].Write(isOn);
                }

                if (BlinkLeds["blinkrightAmber"])
                {
                    lights["amberrightlight"].Write(isOn);
                }

                if (BlinkLeds["blinkleftGreen"])
                {
                    lights["greenleftlight"].Write(isOn);
                }

                if (BlinkLeds["blinkrightGreen"])
                {
                    lights["greenrightlight"].Write(isOn);
                }

                isOn = !isOn;
                Pi.Timing.SleepMilliseconds(_speed);
            }

            foreach (var light in lights)
            {
                WriteLine($"Turning off: {light.Key}");
                light.Value.Write(true);
            }
        }

        private static void GetStationState()
            {
                var states = new Dictionary<string, bool>();
                while (!_stop)
                {
                    states.Clear();
                    var bools = _client.SendCommand("gss");
                    states.Add("EStop", Convert.ToBoolean(bools[0]));
                    states.Add("Manual", Convert.ToBoolean(bools[1]));
                    states.Add("Dispatch", Convert.ToBoolean(bools[2]));
                    states.Add("CloseGates", Convert.ToBoolean(bools[3]));
                    states.Add("OpenGates", Convert.ToBoolean(bools[4]));
                    states.Add("CloseHarness", Convert.ToBoolean(bools[5]));
                    states.Add("OpenHarness", Convert.ToBoolean(bools[6]));
                    states.Add("RaisePlatfor", Convert.ToBoolean(bools[7]));
                    states.Add("LowerPlatform", Convert.ToBoolean(bools[8]));
                    states.Add("LockFlyer", Convert.ToBoolean(bools[9]));
                    states.Add("UnlockFlyer", Convert.ToBoolean(bools[10]));
                    states.Add("InStation", Convert.ToBoolean(bools[11]));
                    //WriteLine(string.Join(" ", states));
                    Pi.Timing.SleepMilliseconds(10000);
                }
            }
    }
}
