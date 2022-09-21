# see https://www.cups.org/doc/admin.html
# lpadmin -p Brother-MFC-9120CN -E -L L201 -m openprinting-ppds:0/ppd/openprinting/Brother/BR9120_2_GPL.ppd -v ipp://MFC-9120CN:631/ipp
# lpadmin -p Ricoh-Aficio-3025 -E -L MuZi -m gutenprint.5.3://ricoh-afc_3025/expert -v ipp://192.168.2.10:631/ipp
# lpadmin -p Ricoh-Aficio-3025 -o PageSize-default=A4 # doesn't seem to work because UI still says "Letter" is the default
