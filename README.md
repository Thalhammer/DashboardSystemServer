# Dashboard System Server

This small application provides extended capabilities to a local website via websockets.

By connecting to this server, a web application can
* Capture KeyboardEvents (even when not in focus)
* Suppress the default action of KeyboardEvents

In my particular setup a Windows 8 tablet with disabled lockscreen, autologin and autostart of chrome in kiosk mode serves as a cheap controlpanel for homeautomation.
However pressing the hardware windows button allowed to open other apps, etc.
This application allows the website to suppress this behaviour and in addition handle the key press it self (move webapp to home screen or something else).
This server will be extended to provide a way to change system behaviour like
* WLAN configuration
* Bluetooth configuration
* Display brightness
* Filesystemaccess
