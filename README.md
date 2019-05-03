# SyrusLeap

Syrus Leap Motion to Hololens bridge

## Build Requirements
* Visual Studio 2017
* Windows SDK 14393
* Leap Motion SDK v2.3.1

## SyrusLeapServer

This project is intended to run on a small, battery powered, bluetooth enabled, Windows PC, such as an Intel Compute Stick. The PC should be connected to a Leap Motion, this software will read information from the Leap Motion and send it to the Hololens over bluetooth.

## SyrusLeapClient

This is the client side of the project. This project will simply receive Leap Motion data from SyrusLeapServer.


