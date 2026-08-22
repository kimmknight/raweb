---
title: Waking a device over the network (Wake-on-LAN)
nav_title: Wake a device
---

A device that is asleep or powered off cannot accept a remote desktop connection. Wake-on-LAN lets you start that device from RAWeb without being in the same room as it.

When you attempt to wake a device, RAWeb asks the server to broadcast a small network message, called a _magic packet_, that tells the device's network adapter to power the machine on.

Waking a device is a separate step from connecting to it. Wake the device first, give it a minute or two to finish starting up, and then connect as you normally would.

## Waking a device

1. Go to the **Devices** or **Apps** page and find the resource you want to wake.
2. Click the **more options** button (•••) on the resource card to open the context menu.
3. Click **Wake up**.

RAWeb tells you whether the signal was sent successfully. A confirmation only means that RAWeb broadcast the _magic packet_ onto the network, not whether the device actually woke up. Devices do not send a message back to RAWeb when they wake. If the device is configured to wake from the network, it will usually be ready to accept connections within a minute or two.

<InfoBar>

If you connect too soon and the connection fails, wait a little longer and try again. A device that has been fully powered off takes longer to become available than one that was only asleep.

</InfoBar>

## When the Wake up option is available

The **Wake up** option only appears for resources your administrator has published as managed resources and for which they have configured a MAC address. It appears for both devices and apps, because an app still runs on a machine that may need to be woken before you can use it.

You will not see the option on every resource. If it is missing for a device you would like to wake, ask your administrator whether Wake-on-LAN can be configured for it.

## If the device does not wake up

RAWeb can only report whether it managed to send the signal, so most problems show up either as an error message or as a device that stays unreachable. Common causes are:

- **The device is not configured to wake from the network.** Wake-on-LAN has to be enabled in the device's firmware and network adapter settings. This is something your administrator sets up on the device itself.
- **The device is on a different network than the RAWeb server.** The wake-up signal is a broadcast, and broadcasts usually do not travel between networks. A device in another office or behind another router often cannot be reached this way.
- **The device is fully disconnected from power.** Wake-on-LAN needs the network adapter to keep a small amount of power available. It cannot start a device that is unplugged, nor a laptop that is shut down and out of battery.
- **The configured MAC address is wrong.** The signal is addressed to a specific network adapter. If the device has been replaced, or the address was entered for the wrong adapter, the signal reaches the network but nothing responds to it.

If waking a device consistently fails, contact your administrator. They can check the [Wake-on-LAN configuration](/docs/publish-resources/#wake-on-lan) for the resource.
