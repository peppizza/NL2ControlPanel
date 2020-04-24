import socket
import asyncio
import RPi.GPIO as gpio

gpio.setmode(gpio.BCM)
gpio.setup(20, gpio.IN, pull_up_down=gpio.PUD_DOWN)

# s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
# s.settimeout(3)
# s.connect(('172.27.153.71', 15151))
# idle = bytearray([78, 0, 18, 0, 0, 0, 0, 0, 8, 0, 0, 0, 0, 0, 0, 0, 1, 76])
# s.send(idle)
# data = s.recv(1024)
# print(len(data))
# print(data)
# print(data[13])

# async def print_letters():
#     for letter in ['A', 'B', 'C', 'D']:
#         print(letter)
#         await asyncio.sleep(1)

# async def print_numbers(loop):
#     for number in range(1, 7):
#         if number == 3:
#             asyncio.ensure_future(print_letters())
#         print(number)
#         await asyncio.sleep(1)

# loop = asyncio.get_event_loop()
# loop.run_until_complete(print_numbers(loop))
# print('End')

while True:
    if gpio.input(20) == True:
        print('on')
    else:
        print('off')
