# Song Request

## Introduction

Song Request is a small music player that can be by several users from the browser. The player is designed to play music in an office environment.

## Features

* Light weight
* High performance
* Fair queue system
* Supports very large music libraries

## Configuration

When Song Request is started for the first time, a config file is created. This config file contains all default settings. Most settings can be changed without restarting the application.

* server.port contains the port the server is listening at
* server.clients contains a ; seperated list with all allowed clients, or 'all' if everyone is allowed to control song request
* library.path contains a ; seperated list with all paths that contains music for the library
* library.minutesbetweenscans contains the number of minutes between each library scan
* library.extensions contains a ; seperated list with all file extensions that are included in the library