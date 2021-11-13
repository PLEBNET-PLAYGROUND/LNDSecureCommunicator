# LND Secure Communicator

Background daemon which will establish secure out-of-band communication channel between LND nodes

Uses existing node pubkeys to build a shared private AES key to secure communications between nodes and send messages or files.

Nodes can find each other's endpoints by sending a formatted keysend with the daemon's onion endpoint.
Because the traffic is sent via TorV3 onion network you can send message and large files for no cost.





