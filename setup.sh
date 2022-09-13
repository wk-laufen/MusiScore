#!/usr/bin/env bash

# 1. Run Raspberry Pi Imager to install Raspberry Pi OS Lite
#   * Set host name, enable SSH, set username/password, configure WIFI, set locale settings

sudo apt update
sudo apt install --no-install-recommends -y git

curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER
rm get-docker.sh
