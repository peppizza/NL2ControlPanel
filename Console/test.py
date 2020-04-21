import socket

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.settimeout(3)
s.connect(('172.27.153.71', 15151))
idle = bytearray([78, 0, 18, 0, 0, 0, 0, 0, 8, 0, 0, 0, 0, 0, 0, 0, 1, 76])
s.send(idle)
data = s.recv(1024)
print(len(data))
print(data)
print(data[13])
