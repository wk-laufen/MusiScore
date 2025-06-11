FROM debian:stable AS builder

RUN apt update && apt upgrade --fix-missing -y

RUN apt update && apt install -y autoconf build-essential \
    avahi-daemon libavahi-client-dev \
    libssl-dev libkrb5-dev libnss-mdns libpam-dev \
    libsystemd-dev libusb-1.0-0-dev zlib1g-dev \
    openssl

RUN apt update && apt install -y autopoint gettext ghostscript git libexif-dev libfontconfig-dev liblcms2-dev libpoppler-cpp-dev libqpdf-dev libtiff-dev libtool mupdf-tools poppler-utils

RUN git clone -b v2.4.12 --depth 1 https://github.com/OpenPrinting/cups /cups
RUN mkdir /usr/lib64/fakeroot && cd /cups && ./configure && make clean && make && make BUILDROOT=/cups/dist install

RUN git clone -b v2.2.12 --depth 1 https://github.com/OpenPrinting/libcups /libcups
RUN cd /libcups && ./configure && sed -i 's/\(^OPTIM\s*=\s*\)-Werror /\1/g' Makedefs && make && make install

RUN git clone -b 2.1.1 --depth 1 https://github.com/OpenPrinting/libcupsfilters /libcupsfilters
RUN cd /libcupsfilters && ./autogen.sh && ./configure && make && make install

RUN git clone -b 2.1.1 --depth 1 https://github.com/OpenPrinting/libppd /libppd
RUN cd /libppd && ./autogen.sh && ./configure && make && make install

RUN git clone -b 2.0.1 --depth 1 https://github.com/OpenPrinting/cups-filters /cups-filters
RUN cd /cups-filters && ./autogen.sh && ./configure && make && make DESTDIR=/cups-filters/dist install

FROM debian:stable
RUN apt update && apt install -y libavahi-client3 rsync && rm -rf /var/lib/apt/lists/*
COPY --from=builder /cups/dist /cups
RUN rsync -a /cups/ / && rm -rf /cups
RUN /usr/sbin/cupsd && \
    while [ ! -f /var/run/cups/cupsd.pid ]; do sleep 1; done && \
    cupsctl --remote-admin --remote-any --share-printers && \
    kill $(cat /var/run/cups/cupsd.pid) && \
    echo "ServerAlias *" >> /etc/cups/cupsd.conf && \
    mv /etc/cups /etc/cups-default
RUN apt update && apt install -y ghostscript && rm -rf /var/lib/apt/lists/*
COPY --from=builder /usr/lib /libppd
RUN rsync -a /libppd/ /usr/lib/
COPY --from=builder /cups-filters/dist /cups-filters
RUN rsync -a /cups-filters/ / && rm -rf /cups-filters
VOLUME /etc/cups
RUN groupadd lpadmin && \
    adduser --no-create-home --shell /bin/bash --disabled-password admin && \
    adduser admin sudo && \
    adduser admin lp && \
    adduser admin lpadmin && \
    echo 'admin ALL=(ALL:ALL) ALL' >> /etc/sudoers && \
    sed -i '/SystemGroup sys root$/ s/$/ sudo/' /etc/cups-default/cups-files.conf
ENV ADMIN_PASSWORD=admin
COPY ./docker-entrypoint.sh /docker-entrypoint.sh
EXPOSE 631
ENTRYPOINT [ "/docker-entrypoint.sh" ]
