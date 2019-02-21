﻿PorTee
=========
A fast, transparent, cross-platform lightweight traffic replicator and aggregator.  A more flexible solution than multicasting or mkfifo & nc | tee.
Supports bidirectional or one-way traffic.

Universal binary is compatible with Linux/Windows/OSX

Cases
-
* Services that don't play well with multiple connections
* Consume remote high-traffic services only once and distribute it to multiple clients
* Aggregate client traffic to a host over a single connection

Usage
-
<pre>
Required:
  -h|--host=        Remote Host
  -p|--port=        Remote Port
Optional:
  -a|--localhost=   IP Address to listen on [default: 0.0.0.0]
  -l|--localport=   Port to listen on [default: same as remote port]
  -v|--verbose      Dump traffic in hex/ascii split view to stdout
  --read-only       Only allow connecting clients to consume traffic
  --write-only      Only allow connecting clients to send traffic
  --prompt          Prompt for input
  -?|--help         Show usage

Example:
portee.exe -h towel.blinkenlights.nl -p 23 &
nc localhost 23
</pre>