default:
		cake -target=default

deps:
		cake -target=deps

dependencies:
		cake -target=deps

clean:
		cake -target=clean

.PHONY: default deps dependencies clean
