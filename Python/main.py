import RPi.GPIO as gpio
import sys
import random
import socket
import asyncio
from time import sleep
from pprint import pprint

gpio.setmode(gpio.BCM)
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.settimeout(3)
loop = asyncio.get_event_loop()

amberleftlight = 21
amberleftbutton = 26
amberrightlight = 12
amberrightbutton = 6
greenleftlight = 16
greenleftbutton = 13
greenrightlight = 5
greenrightbutton = 7
auto = 19
man = 20
blinkleftAmber = blinkrightAmber = blinkleftGreen = blinkrightGreen = False
stop = False

gpio.setup(auto, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(man, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(amberleftbutton, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(amberrightbutton, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(greenleftbutton, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(greenrightbutton, gpio.IN, pull_up_down=gpio.PUD_DOWN)
gpio.setup(amberleftlight, gpio.OUT)
gpio.setup(amberrightlight, gpio.OUT)
gpio.setup(greenleftlight, gpio.OUT)
gpio.setup(greenrightlight, gpio.OUT)
server_address = ('172.27.153.71', 15151)

def intro():
    global automan
    global stop
    stop = False
    while True:
        if gpio.input(auto) == True:
            print('starting up')
            try:
                s.connect(server_address)
            except Exception as e:
                pass
                # raise e
            print('done!')
            automan = 1
            break
        elif gpio.input(man) == True:
            print('entering manual mode')
            try:
                s.connect(server_address)
            except Exception as e:
                pass
                # raise e
            print('done!')
            automan = 0.1
            break


async def LEDBlink(speed):
    while stop != True:
        if blinkleftAmber == True:
            gpio.output(amberleftlight, False)
        if blinkrightAmber == True:
            gpio.output(amberrightlight, False)
        if blinkleftGreen == True:
            gpio.output(greenleftlight, False)
        if blinkrightGreen == True:
            gpio.output(greenrightlight, False)
        await asyncio.sleep(speed)
        if blinkleftAmber == True:
            gpio.output(amberleftlight, True)
        if blinkrightAmber == True:
            gpio.output(amberrightlight, True)
        if blinkleftGreen == True:
            gpio.output(greenleftlight, True)
        if blinkrightGreen == True:
            gpio.output(greenrightlight, True)
        if stop == True:
            break
        await asyncio.sleep(speed)
        
async def main(automan):
    global blinkleftAmber
    global blinkleftGreen
    global blinkrightAmber
    global blinkrightGreen
    global stop
    stop = False
    asyncio.ensure_future(LEDBlink(automan))
    blinkleftAmber = blinkrightAmber = blinkleftGreen = blinkrightGreen = True
    while gpio.input(auto) == True or gpio.input(man) == True:
        await asyncio.sleep(0.00001)
        if gpio.input(amberleftbutton) == True:
            print('opening gates..', end='\r')
        elif gpio.input(amberrightbutton) == True:
            print('closing gates..', end='\r')
        if gpio.input(greenleftbutton) == True and gpio.input(greenrightbutton) == False or gpio.input(greenrightbutton) == True and gpio.input(greenleftbutton) == False:
            print('press both to dispatch', end='\r')
        elif gpio.input(greenleftbutton) == True and gpio.input(greenrightbutton) == True:
            print('dispatching..', end='\r')
            blinkleftGreen = blinkrightGreen = False
            while gpio.input(greenleftbutton) == True and gpio.input(greenrightbutton) == True:
                await asyncio.sleep(0.00001)
                gpio.output(greenleftlight, False)
                gpio.output(greenrightlight, False)
            gpio.output(greenrightlight, True)
            gpio.output(greenleftlight, True)
            await asyncio.sleep(3)
            blinkleftGreen = blinkrightGreen = True
    gpio.output(amberleftlight, True)
    gpio.output(amberrightlight, True)
    gpio.output(greenleftlight, True)
    gpio.output(greenrightlight, True)
    blinkleftGreen = blinkrightGreen = blinkleftAmber = blinkrightAmber = False
    stop = True
    gpio.cleanup()
    s.close()
    sys.exit()

    
if __name__ == '__main__':
    try:
        gpio.output(amberleftlight, True)
        gpio.output(amberrightlight, True)
        gpio.output(greenleftlight, True)
        gpio.output(greenrightlight, True)
        if gpio.input(auto) == True or gpio.input(man) == True:
            print('TURN OFF PANEL BEFORE STARTING')
            gpio.cleanup()
            sys.exit()
        intro()
        loop.run_until_complete(main(automan))
    except KeyboardInterrupt:
        blinkleftAmber = False
        blinkrightAmber = False
        blinkleftGreen = False
        blinkrightGreen = False
        gpio.cleanup()
        s.close()
        sys.exit()
