# Hed #

Hed (Echo in Hebrew) is a proxy server based on the Switchboard proxy (https://github.com/niik/switchboard).

The Hed proxy server was built to provide broken network support, in other words, instead of being a reliable proxy, it will do strange things like accept a request and then just discard it, or send multiple requests downstream or just respond very slowly.

You configure the Hed proxy using topologies. A topology is made from multiple 