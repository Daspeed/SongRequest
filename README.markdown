# SongRequest

## Introduction

SongRequest is a small music player that can be operated by several users from the browser. The player is designed to play music in an office environment.

## Features

* Light weight
* High performance
* Fair queue system
* Supports very large music libraries
* Built-in support for VLC and Windows media player
* Cross-platform (Tested on Linux, Windows and MacOS with Mono)

## Configuration

When SongRequest is started for the first time, a config file is created. This config file contains all default settings. Most settings can be changed without restarting the application.

* server.port contains the port the server is listening at
* server.clients contains a ; separated list with all allowed clients, or 'all' if everyone is allowed to control SongRequest
* library.path contains a ; separated list with all paths that contain music for the library
* library.minutesbetweenscans contains the number of minutes between each library scan
* library.extensions contains a ; separated list with all file extensions that are included in the library
