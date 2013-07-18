//
//  watcher.c
//  WaveBoxFSEvents
//
//  Created by Benjamin Baron on 7/18/13.
//  Copyright (c) 2013 Einstein Times Two Software. All rights reserved.
//

#include "watcher.h"

// Internal state
void *callbackInfo = NULL;
FSEventStreamRef eventStream = NULL;
WatchCallback externalCallback = NULL;
bool isFileEventsEnabled = false;
CFRunLoopRef runLoop = NULL;

// Internal callback function that calls out to the external callback
void InternalCallback(ConstFSEventStreamRef streamRef, void *clientCallBackInfo, size_t numEvents, void *eventPaths, const FSEventStreamEventFlags eventFlags[], const FSEventStreamEventId eventIds[])
{
    // Call the external callback to inform them of the path updates
    if (externalCallback != NULL)
    {
        externalCallback(eventPaths, numEvents, (long *)eventFlags, isFileEventsEnabled);
    }
}

// Blocks until runloop finishes, should be called from a dedicated thread
void WatchPaths(char **paths, int numberOfPaths, double latency, WatchCallback callback)
{     
    // Check if file events is enabled. File events are only available for OS X 10.7 and higher.
    // Note that this function retrieves the kernel version, and kernel version 11 is OS X 10.7
    char str[256];
    size_t size = sizeof(str);
    if (!sysctlbyname("kern.osrelease", str, &size, NULL, 0))
    {
        char *majorVersion = strtok(str, ".");
        if (majorVersion)
        {
            isFileEventsEnabled = atoi(majorVersion) >= 11;
        }
    }
    
    // Save a reference to the external callback function
    externalCallback = callback;
    
    // Stop any existing file system watching
    StopWatchingPaths();
    
    // Parse the paths into CFStringRefs in a CFArrayRef to pass to the FSEvents API
    CFMutableArrayRef pathsToWatch = CFArrayCreateMutable(NULL, numberOfPaths, NULL);
    for (int i = 0; i < numberOfPaths; i++)
    {        
        CFStringRef pathString = CFStringCreateWithCString(NULL, paths[i], kCFStringEncodingUTF8);
        CFArrayAppendValue(pathsToWatch, pathString);
        CFRelease(pathString);
    }
    
    // Start the FSEvent stream
    eventStream = FSEventStreamCreate(NULL,
                                      &InternalCallback,
                                      callbackInfo,
                                      pathsToWatch,
                                      kFSEventStreamEventIdSinceNow,
                                      latency,
                                      isFileEventsEnabled ? kFSEventStreamCreateFlagFileEvents : kFSEventStreamCreateFlagNone);
    FSEventStreamScheduleWithRunLoop(eventStream, CFRunLoopGetCurrent(), kCFRunLoopDefaultMode);
    FSEventStreamStart(eventStream);
    CFRelease(pathsToWatch);
    
    // Start the run loop
    runLoop = CFRunLoopGetCurrent();
    CFRunLoopRun();
}

// Stops the event stream and run loop
void StopWatchingPaths()
{
    // Close any existing stream
    if (eventStream != NULL)
    {
        // Clean up the stream
        FSEventStreamStop(eventStream);
        FSEventStreamUnscheduleFromRunLoop(eventStream, CFRunLoopGetCurrent(), kCFRunLoopDefaultMode);
        FSEventStreamInvalidate(eventStream);
        FSEventStreamRelease(eventStream);
        eventStream = NULL;
    }
        
    // Stop the run loop
    if (runLoop != NULL)
    {
        CFRunLoopStop(runLoop);
        runLoop = NULL;
    }
}