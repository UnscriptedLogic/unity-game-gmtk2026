FROM ubuntu:22.04

WORKDIR /game

COPY ./Builds/LinuxServer/ .

RUN chmod +x "./gmtk2026_server.x86_64"

EXPOSE 7777/udp

ENTRYPOINT ["./gmtk2026_server.x86_64", "-batchmode", "-nographics"]
