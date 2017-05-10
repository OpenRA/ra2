# Red Alert 2 mod for OpenRA

[![Build Status](https://travis-ci.org/OpenRA/ra2.svg?branch=master)](https://travis-ci.org/OpenRA/ra2)

Consult the [wiki](https://github.com/OpenRA/ra2/wiki) for instructions on how to install and use this.

[![Bountysource](https://api.bountysource.com/badge/tracker?tracker_id=27677844)](https://www.bountysource.com/teams/openra/issues?tracker_ids=27677844)

### Building via [`cake`](http://cakebuild.net/)

You will need a copy of the [engine's source code](https://github.com/OpenRA/OpenRA/) to build this mod.

Set the environment variable `OPENRA_ROOT` to the path you cloned OpenRA into.

```shell
$ cp example.env .env
$ edit .env # this is where you will set OPENRA_ROOT
$ make && make version # this step requires 'cake'
```
