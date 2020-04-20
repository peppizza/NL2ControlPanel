import RPi.GPIO as gpio
from time import sleep
import sys

gpio.setmode(gpio.BCM)

gpio.setup(21, gpio.OUT)

try:
    while True:
        gpio.output(21, True)
        sleep(1)
        gpio.output(21, False)
        sleep(1)
except KeyboardInterrupt:
    gpio.cleanup()
    sys.exit()
