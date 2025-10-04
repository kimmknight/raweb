---
title: Reverse proxy via Nginx
---

Nginx reverse proxy does not support proxying NTLM authentication by default. If you place RAWeb behind nginx, you will only be able to use the web interface.

Some recommendations on the internet suggest using `upstream` with `keepalive`. This is _**extremely dangerous**_ because keepalive connections are shared between all connections, which means that one user's NTLM authentication will apply to other users as well.

You may be able to enable NTLM authentication with nginx via nginx plus or by using [nginx-ntl-module](https://github.com/gabihodoroaga/nginx-ntlm-module).
