---
title: Testing Wake-on-LAN
nav_title: Test Wake-on-LAN
---

RAWeb cannot confirm that a device woke up. A device that is asleep or powered off does not send anything back, so a success message only means that RAWeb sent the magic packet onto the network. To confirm that Wake-on-LAN works, you must either wake a real device or watch for the magic packet with a monitoring tool.

## Wake a real device

1. Configure a device for Wake-on-LAN. See [Configure Wake-on-LAN](/docs/publish-resources/#wake-on-lan).
2. Shut down the device or put it to sleep.
3. In RAWeb, click the **more options** button (•••) on the resource card and choose **Wake up**.
4. Wait a minute or two for the device to finish starting up.

This is the only test that covers every part of the feature. It is slow to repeat, because each attempt requires a shutdown and a startup, and it does not tell you which part failed when a device does not wake.

## Watch for the magic packet

[Wake on LAN Monitor](https://www.depicus.com/wake-on-lan/wake-on-lan-monitor) from Depicus listens for magic packets and reports the MAC address in each packet that it receives. Use it to confirm that RAWeb sent a packet and that the packet contains the correct MAC address without shutting down a device.

<InfoBar severity="caution" title="Change the UDP port">

Wake on LAN Monitor listens on UDP port 4343 by default. RAWeb does not send magic packets to that port, so the monitor will not report anything until you change the port to **9** or **7**.

</InfoBar>

1. Download and run Wake on LAN Monitor on a device on the same subnet as the RAWeb server. You may run it on the RAWeb server itself.
2. Set the UDP port to **9**.
3. Start listening for packets.
4. In RAWeb, click the **more options** button (•••) on a resource card that has a MAC address configured and choose **Wake up**.
5. Confirm that the monitor reports a packet and that the MAC address in the packet matches the address configured for the resource.

<InfoBar>

Wake on LAN Monitor may report more than one packet for a single **Wake up**. RAWeb sends the magic packet to several broadcast addresses, and a monitor on the same subnet may receive more than one of them.

</InfoBar>

### Confirm that the RAWeb server can reach a device

Administrators can use Wake on LAN Monitor to check whether the RAWeb server is able to reach a device's network. Run the monitor on a device on the same network as the device you want to wake, and then choose **Wake up** in RAWeb.

If the monitor reports the packet, the network path works, and any remaining problem is in the device's firmware or network adapter settings. If the monitor does not report the packet, the packet is not reaching the network. This usually means that a router between the RAWeb server and the device does not forward broadcasts.

## Create resources to use for testing

The **Wake up** option only appears for managed file resources that have a MAC address configured. See [Configure Wake-on-LAN](/docs/publish-resources/#wake-on-lan) for instructions on how to configure one.

To test the terminal server picker, you need a resource that is published by more than one terminal server, with a MAC address configured for more than one of them. Create two managed file resources for the same RemoteApp whose RDP file contents are identical except for the `full address:s:` property. RAWeb omits that property when it calculates the resource ID for a RemoteApp, so both resources will have the same ID and will appear as a single resource with two terminal servers.

<InfoBar severity="attention">

Resources are only combined when combined terminal servers mode is enabled, and only for RemoteApps. RAWeb includes the address when it calculates the resource ID for a desktop, so two desktop resources will always appear separately.

</InfoBar>

## Magic packet details

A magic packet contains six `0xFF` bytes followed by the six-byte MAC address of the target device, repeated sixteen times. RAWeb builds the packet in `SendMagicPacket` in `dotnet/RAWeb.Server/src/api/resources/WakeDesktop.cs`.

RAWeb sends the packet over UDP to ports **7** and **9** at each of the following addresses:

- the limited broadcast address, `255.255.255.255`, and
- the directed broadcast address of every active IPv4 network that the server is attached to, such as `192.168.1.255` for a server on `192.168.1.0/24`.

RAWeb sends to every address because routers and virtual switches do not always forward the limited broadcast address, and a server with several network adapters may send it from the wrong adapter. Sending to a network's directed broadcast address causes the packet to leave the adapter for that network. A device that is already awake ignores the additional packets.
