import RPi.GPIO as gpio
import sys
import threading
import random
import socket
from time import sleep
from pprint import pprint

gpio.setmode(gpio.BCM)
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.settimeout(3)

amberleftlight = 21
amberleftbutton = 26
amberrightlight = 12
amberrightbutton = 6
greenleftlight = 16
greenleftbutton = 13
auto = 19
man = 20
blinkleftAmber = False
blinkrightAmber = False
blinkleftGreen = False

gpio.setup(auto, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(man, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(amberleftbutton, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(amberrightbutton, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(greenleftbutton, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(amberleftlight, gpio.OUT)
gpio.setup(amberrightlight, gpio.OUT)
gpio.setup(greenleftlight, gpio.OUT)
server_address = ('172.27.153.71', 15151)

def intro():
    global automan
    while True:
        if gpio.input(auto) == True and gpio.input(man) == False:
            print('starting up')
            try:
                s.connect(server_address)
            except Exception as e:
                pass
                # raise e
            print('done!')
            automan = 1
            break
        elif gpio.input(man) == True and gpio.input(auto) == False:
            print('entering manual mode')
            try:
                s.connect(server_address)
            except Exception as e:
                pass
                # raise e
            print('done!')
            automan = 0.1
            break

def flashleftAmber(speed):
    while blinkleftAmber == True:
        gpio.output(amberleftlight, False)
        sleep(speed)
        gpio.output(amberleftlight, True)
        sleep(speed)

def flashrightAmber(speed):
    while blinkrightAmber == True:
        gpio.output(amberrightlight, False)
        sleep(speed)
        gpio.output(amberrightlight, True)
        sleep(speed)

def flashleftGreen(speed):
    while blinkleftGreen == True:
        gpio.output(greenleftlight, False)
        sleep(speed)
        gpio.output(greenleftlight, True)
        sleep(speed)
        

def main(automan):
    global blinkleftAmber
    global blinkleftGreen
    global blinkrightAmber
    flashleftamber = threading.Thread(target=flashleftAmber, args=(automan, ), daemon=True)
    flashrightamber = threading.Thread(target=flashrightAmber, args=(automan, ), daemon=True)
    flashleftgreen = threading.Thread(target=flashleftGreen, args=(automan, ), daemon=True)
    blinkleftAmber = True
    blinkrightAmber = True
    blinkleftGreen = True
    flashleftamber.start()
    flashrightamber.start()
    flashleftgreen.start()
    sleep(0.1)
    while True:
        if gpio.input(auto) == False and gpio.input(man) == False:
            gpio.output(amberleftlight, True)
            gpio.output(amberrightlight, True)
            gpio.output(greenleftlight, True)
            blinkleftGreen = False
            blinkleftAmber = False
            blinkrightAmber = False
            flashleftamber.join()
            flashrightamber.join()
            flashleftgreen.join()
            gpio.cleanup()
            s.close()
            sys.exit()
        if gpio.input(amberleftbutton) == True:
            print('opening gates..', end='\r')
        elif gpio.input(amberrightbutton) == True:
            print('closing gates..', end='\r')
        if gpio.input(greenleftbutton) == True:
            print('press both to dispatch', end='\r')

if __name__ == '__main__':
    try:
        gpio.output(amberleftlight, True)
        gpio.output(amberrightlight, True)
        gpio.output(greenleftlight, True)
        if gpio.input(auto) == True or gpio.input(man) == True:
            print('TURN OFF PANEL BEFORE STARTING')
            gpio.cleanup()
            sys.exit()
        intro()
        main(automan)
    except KeyboardInterrupt:
        blinkleftAmber = False
        blinkrightAmber = False
        blinkleftGreen = False
        gpio.cleanup()
        s.close()
        sys.exit()
