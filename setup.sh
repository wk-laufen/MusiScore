#!/usr/bin/env bash

# 1. Run Raspberry Pi Imager to install Raspberry Pi OS Lite
#   * Set host name, enable SSH, set username/password, configure WIFI, set locale settings

sudo apt update
sudo apt install -y git

curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER
rm get-docker.sh

sudo apt update
sudo apt install -y python3 python3-pip
sudo sh -c 'curl https://sh.rustup.rs -sSf | sh'
sudo /bin/bash -c 'source $HOME/.cargo/env'
sudo pip install docker-compose
