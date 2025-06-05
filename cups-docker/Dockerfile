FROM debian:stable AS builder

RUN apt update && apt upgrade --fix-missing -y

RUN apt update && apt install -y autoconf build-essential \
    avahi-daemon libavahi-client-dev \
    libssl-dev libkrb5-dev libnss-mdns libpam-dev \
    libsystemd-dev libusb-1.0-0-dev zlib1g-dev \
    openssl

RUN apt update && apt install -y git

RUN git clone -b v2.4.12 --depth 1 https://github.com/OpenPrinting/cups
RUN mkdir /usr/lib64/fakeroot && cd ./cups && ./configure && make clean && make && make BUILDROOT=/cups/dist install

FROM debian:stable
RUN apt update && apt install -y libavahi-client3 procps rsync && rm -rf /var/lib/apt/lists/*
COPY --from=builder /cups/dist /install
RUN rsync -a /install/ / && rm -rf /install
COPY ./docker-entrypoint.sh /docker-entrypoint.sh
EXPOSE 631
ENTRYPOINT [ "/docker-entrypoint.sh" ]
