ol3-bower
================

Bower for [Open Layers 3](http://openlayers.org)

This is the unofficial bower package for OpenLayers 3.

## Installation
`bower install ol3-bower (--save)`

Do you want to install a specific ol3 version?

`bower install ol3-bower#3.4.0`

Currently, these versions are supported: 3.2.1, 3.3.0, 3.4.0, 3.5.0, 3.6.0, 3.7.0, 3.8.2, 3.9.0, 3.10.0, 3.10.1.

## Steps to upgrade
* Clone and overwrite with new dist files
* Increment version number in bower.json
* `git commit -am "Release version 3.x.x"`
* `git tag -a 3.x.x -m "v3.x.x"`
* `git push origin master --tags`
