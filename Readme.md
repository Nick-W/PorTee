PorTee
=========
A Cross-platform lightweight traffic replicator and aggregator.  A more flexible solution than mkfifo & nc | tee.
Supports bidirectional or one-way traffic

Uses
-
* Buggy services that don't play well with multiple connections
* Consume remote high-traffic services only once and distribute it to multiple clients
* Aggregate traffic to a host (Be mindful of your data! We're kind of violating the principles of TCP/IP here)