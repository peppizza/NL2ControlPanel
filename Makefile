GO    := $(shell which go)
ARCH  := arm
OOS   := linux

.PHONY: clean check all
all: example

clean:
		rm -f example

check: example
		./example

example: example.go
		GOOS=$(OOS) GOARCH=&(ARCH) $GO build $<