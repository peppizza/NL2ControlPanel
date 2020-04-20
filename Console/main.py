import RPi.GPIO as gpio
from time import sleep
import sys
import threading
import random

gpio.setmode(gpio.BCM)

amberlight = 21
amberbutton = 26
auto = 19
man = 20
blinkAmber = False

gpio.setup(amberbutton, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(auto, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(man, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(amberlight, gpio.OUT)

def intro():
    global automan
    while True:
        if gpio.input(auto) == True and gpio.input(man) == False:
            print('starting up')
            sleep(5)
            print('done!')
            automan = 1
            break
        elif gpio.input(man) == True and gpio.input(auto) == False:
            print('entering manual mode')
            sleep(5)
            print('done!')
            automan = 0.1
            break

def flashAmber(speed):
    print('flashing led...')
    print(blinkAmber)
    while blinkAmber == True:
        gpio.output(amberlight, False)
        sleep(speed)
        gpio.output(amberlight, True)
        sleep(speed)
    else:
        gpio.output(amberlight, True)

def main(automan):
    x = threading.Thread(target=flashAmber, args=(automan, ))
    global blinkAmber
    blinkAmber = True
    x.start()
    while True:
        if gpio.input(amberbutton) == True:
            print('stopping..')
            gpio.output(amberlight, True)
            blinkAmber = False
            x.join()

if __name__ == '__main__':
    try:
        gpio.output(amberlight, True)
        intro()
        main(automan)
    except KeyboardInterrupt:
        blinkAmber = False
        gpio.cleanup()
        sys.exit()
