import socket
import re

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.settimeout(3)
s.connect(('172.27.153.68', 13000))

test = input('message: ')
test = bytearray(test, encoding='UTF-8')
s.send(test)
txt = str(s.recv(1024))
txt = re.sub(r'[b, \']', r'', txt)
print(txt)