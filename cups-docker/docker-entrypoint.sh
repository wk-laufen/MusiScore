#!/bin/bash

export LD_LIBRARY_PATH="/usr/lib64:$LD_LIBRARY_PATH" && ldconfig

echo "LD_LIBRARY_PATH: $LD_LIBRARY_PATH"
ls -la /usr/lib64
echo "/etc/ld.so.conf:"
cat /etc/ld.so.conf
echo "/etc/ld.so.conf.d/*:"
cat /etc/ld.so.conf.d/*

/usr/sbin/cupsd \
&& while [ ! -f /var/run/cups/cupsd.pid ]; do sleep 1; done \
&& cupsctl --remote-admin --remote-any --share-printers \
&& kill $(cat /var/run/cups/cupsd.pid) \
&& echo "ServerAlias *" >> /etc/cups/cupsd.conf \
&& service cups start \
&& /usr/sbin/cupsd -f