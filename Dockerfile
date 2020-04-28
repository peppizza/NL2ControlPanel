FROM sedden/rpi-raspbian-qemu:wheezy

RUN apt-get update && apt-get install -y make golang
RUN mkdir -p /home/au
WORKDIR /home/au
ADD . /home/au