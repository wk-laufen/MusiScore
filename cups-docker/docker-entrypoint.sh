#!/bin/bash

/usr/sbin/cupsd \
&& while [ ! -f /var/run/cups/cupsd.pid ]; do sleep 1; done \
&& cupsctl --remote-admin --remote-any --share-printers \
&& kill $(cat /var/run/cups/cupsd.pid) \
&& echo "ServerAlias *" >> /etc/cups/cupsd.conf \
&& service cups start \
&& /usr/sbin/cupsd -f