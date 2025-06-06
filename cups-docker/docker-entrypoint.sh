#!/bin/bash

if [ ! -f /etc/cups/cupsd.conf ]; then
    echo '/etc/cups/cupsd.conf not found - using default config'
    cp -r /etc/cups-default/* /etc/cups/
else
    echo 'Found /etc/cups/cupsd.conf'
fi

service cups start \
&& /usr/sbin/cupsd -f