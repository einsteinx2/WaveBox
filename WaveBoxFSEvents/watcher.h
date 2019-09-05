//
//  watcher.h
//  WaveBoxFSEvents
//
//  Created by Benjamin Baron on 7/18/13.
//  Copyright (c) 2013 Einstein Times Two Software. All rights reserved.
//

#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <signal.h>
#include <errno.h>
#include <sys/sysctl.h>
#include <CoreServices/CoreServices.h>

// External callback definition
typedef void (*WatchCallback)(char **changedPaths, size_t numberOfPaths, long *eventFlags, bool fileEventsEnabled);
typedef void (*SimpleWatchCallback)(void);

// Exported functions
void WatchPaths(char **paths, size_t numberOfPaths, double latency, WatchCallback callback);
void StopWatchingPaths(void);
