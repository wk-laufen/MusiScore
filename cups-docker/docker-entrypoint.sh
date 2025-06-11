#!/bin/bash

echo -e "${ADMIN_PASSWORD}\n${ADMIN_PASSWORD}" | passwd admin

if [ ! -f /etc/cups/cupsd.conf ]; then
    echo '/etc/cups/cupsd.conf not found - using default config'
    cp -r /etc/cups-default/* /etc/cups/
else
    echo 'Found /etc/cups/cupsd.conf'
fi

/usr/sbin/cupsd -f -C /etc/cups/cupsd.conf -s /etc/cups/cups-files.conf