# WaveBox API

## The basics:
Every call to the WaveBox API requires authentication data, whether it is the &quot;s&quot;
session key parameter sent in GET or POST parameters, or the wavebox_session cookie
issued by the server.  Unless otherwise specified, all other parameters are optional.
Each API call is prefixed by /api/ in the URL.  Responses are encoded in gzip or
deflate if requested, in order to compress large responses.

## Login
> Used to create a new session ID for an existing user. The session ID returned is used
> to access all other API calls.

**URL:** /api/login

**Example:** http://localhost:6500/api/login?u=test&p=test

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| u	| The username of the identity you would like to use to access the API	| &#x2713; |
| p | The password associated with the above username | &#x2713; |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| sessionId | The session id for this login session. |

## Logout
> Used to destroy the current session for an existing user. The session ID returned is the one which was destroyed.

**URL:** /api/logout

**Example:** http://localhost:6500/api/logout

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| n/a | n/a |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| sessionId | The session id destroyed by logging out. |

## Albums
> Used to return information about all albums known to WaveBox, or with the 'id' parameter, information about a specific album. On invalid id, a null album object will be sent.

**URL:** /api/albums/:id

**Example:** http://localhost:6500/api/albums/125

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The ID of the album you would like information about.  If null, information about all albums known by the server will be retrieved. |
| limit | Modifies the requested result set to only include a subset of results, just like SQL LIMIT x,y. |
| range | Fetches a list of all items within the comma-separated alphanumeric range. |
| s | The sessionId of the active session you would like to use to access the API. |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| albums | An array of album objects, which contain information about the album(s) requested.  The structure of the album object is discussed in the Data Structures segment. |
| songs | An array of song objects, which contain information about the songs in the requested album.  Null if ‘id’ is not specified.  The structure of the song object is discussed in the Data Structures segment. |

## Art
> Used to return a binary art file, which is tied to an album via an art ID.  With parameter ‘id’ specified, will attempt to fetch the album art with this ID.  Will return HTTP 404 error if no art found.
>
> Note that the Art API handler can use either ImageMagick or GDI to resize an image.  On Mac OSX and UNIX, ImageMagick will be tried, followed by GDI on failure.  On Windows, GDI will be used.
>
> In addition, note that the handler will send a HTTP Last-Modified header, so clients may cache art for a faster response.

**URL:** /api/art/:id

**Example:** http://localhost:6500/api/art/125?size=500

**Parameters:**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The art id for the art you would like to receive |  &#x2713; |
| size | The horizontal size of the image you would like to receive.  The aspect ratio will be maintained.  i.e. If an image is 600x500 and you specify size=300, then the resulting image will be 300x250.  If size is not specified, the original image file is returned. |
| s | The sessionId of the active session you would like to use to access the API. | &#x2713; |

**Returns**

| Name | Description |
| :--- | :---------- |
| Binary art file on success | Returns binary art file directly to client. Returns HTTP 404 error if art does not exist. |

## Artists
> Used to return information about all artists known to WaveBox, or with the ‘id’ parameter, information about a specific artist.  On invalid id, a null artist object will be sent.

**URL:** /api/artists

**Example:** http://localhost:6500/api/artists/125

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The ID of the artist you would like information about.  If null, information about all artists known by the server will be retrieved. | |
| includeSongs | If set to false, or not included, songs array will be empty. Otherwise, songs array will contain all associated songs for this artist.  Default: false | |
| lastfmInfo | Attempt to fetch additional information about this artist using the Last.fm API.  Default: false. | |
| limit | Modifies the requested result set to only include a subset of results, just like SQL LIMIT x,y. | |
| range | Fetches a list of all items within the comma-separated alphanumeric range. | |
| s | The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| artists | An array of artist objects, which contain information about the artist(s) requested.  The structure of the artist object is discussed in the Data Structures segment. |
| albums | An array of album objects, which contain information about the album(s) requested.  The structure of the album object is discussed in the Data Structures segment.  Null if no artist ID is supplied. |
| songs | An array of song objects, which contain information about the songs in the requested album.  Empty array if ‘id’ is not specified, includeSongs not specified, or includeSongs is false.  The structure of the song object is discussed in the Data Structures segment. |
| lastfmInfo | An object containing information about a specific artist, from the Last.fm API.  Null if not requested, or if no information returned. |

## Database

**URL:** /api/database

**Example:** http://localhost:6500/api/database/20000

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The query ID at which you wish to begin retrieving the query log.  Any queries occuring after this ID will be returned. | |
| s | The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| WaveBox media database | Returns the entire SQLite database file, with no ‘id’ specified.  Also returns special ‘WaveBox-LastQueryId’ HTTP header. |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| queries | An array of queries occuring after the specified ID, containing ‘id’, ‘query’, and ‘values’.  Used for differential update of remote database. |

## Folders

**URL:** /api/folders

**Example:** http://localhost:6500/api/folders/20000

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The ID of the folder you would like information about.  If null, information about all top-level folders known by the server will be retrieved. | |
| recursiveMedia | If a folder ID is specified, recursiveMedia=true will return all songs for all folders below the associated folder in the directory structure.  Default: false | |
| mediaFolders | If no id is specified, and this is true, return the media folders instead of the list of top level folders.  Default: false | |
| s | The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| folders | An array of folder objects, which contain information about the folder(s) requested.  The structure of the folder object is discussed in the Data Structures segment. |
| containingFolder | The parent folder object of the folder specified by this folder ID, or if at deepest folder level, the currently focused folder. |
| songs | An array of song objects, which contain information about the songs in the requested folder.  Empty array if ‘id’ is not specified, or no songs present in folder.  The structure of the song object is discussed in the Data Structures segment. |
| videos | An array of video objects, which contain information about the songs in the requested folder.  Empty array if ‘id’ is not specified, or no videos present in folder.  The structure of this object is discussed in the Data Structures segment. |

## Genres

**URL:** /api/genres

**Example:** http://localhost:6500/api/genres/20000

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The ID of a specific genre.  By default, will return all artists containing a genre matching this ID. | |
| type | Specifies the type of objects to retrieve, which match this genre ID.  Possible values are “folders”, “albums”, “songs”, and “artists” (default). | |
| s | The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| genres | An array of genre objects, which contain information about the genre(s) requested.  The structure of the genre object is discussed in the Data Structures segment. |
| folders | An array of folder objects, which contain information about the folder(s) requested.  The structure of the folder object is discussed in the Data Structures segment. |
| artists | An array of artist objects, which contain information about the artists matching the requested genre.  Empty array if ‘id’ is not specified, or no artists present in genre.  The structure of the artist object is discussed in the Data Structures segment. |
| albums | An array of album objects, which contain information about the albums matching the requested genre.  Empty array if ‘id’ is not specified, or no albums matching the requested genre.  The structure of the album object is discussed in the Data Structures segment. |
| songs | An array of song objects, which contain information about the songs matching the requested genre.  Empty array if ‘id’ is not specified, or no songs matching the requested genre.  The structure of the song object is discussed in the Data Structures segment. |

## Jukebox

> Used to manipulate the jukebox function of WaveBox.  This includes functions such as “play”, “pause”, “stop”, and other related actions.  Can potentially return an error, the status of the jukebox, and the jukebox’s current playlist.
>
> The “playlist”, “add”, “remove”, “move”, and “clear” actions manipulate the jukebox play queue.
> * Add takes only a song ID or a list of song IDs
> * Remove takes an index or a comma-delimited list of indexes in the jukebox playlist
> * Move takes a from index and a to index as a comma-delimited tuple. e.g. action=move&index=1,2 would move the song at index 1 to index 2
>
> “play”, “pause”, “stop”, “prev”, and “next” control the jukebox player.
> * Play takes an index of the jukebox playlist, or if no index and paused or stopped, plays the current index
>
> **Note: Songs must be added to the jukebox’s playlist before they can be played.**

**URL:** /api/jukebox

**Example:** http://localhost:6500/api/jukebox?action=add&id=125

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| action | The action to be performed, defaults to “status”. Possible values are: “play”, “pause”, “stop”, “prev”, “next”, “status”, “playlist”, “add”, “remove”, “move”, “clear” | |
| id | Only used (and required) with the “add” action. Contains a comma delimited list of song IDs to add to the playlist | (&#x2713;) |
| index | Only used (and required) with actions that modify the playlist such as “add”, “remove”, or “move”.  When using “remove”, may be a comma-delimited list of indexes to remove. When using “move”, this is a single comma-delimited pair of indexes with the first being the ‘from’ index and the second being the ‘to’ index. | (&#x2713;) |
| s | The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| jukeboxStatus | An object containing information on the jukebox player. The structure is discussed in the Data Structures segment. |
| jukeboxPlaylist | An object describing the jukebox play queue. The structure is discussed in the Data Structures segment. |

## Podcast
> Returns information about a podcast or an episode, or alternatively performs an action on a podcast or an episode.  If an action is not specified, information will be returned.

**URL:** /api/podcast

**Example:** http://localhost:6500/api/podcasts/125

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The ID of the podcast you are requesting | |
| episodeId | The ID of an episode of a specific podcast with specified podcastId | |
| action | Action you would like to perform.  Possible actions include “add”,  “delete”, or “edit”.  If not specified, information is returned instead of an action being performed. | |
| url | The (url encoded) url of the podcast to add. Used with the “add” action only. | |
| keepCap | The number of episodes to keep for the podcast.  Only used with the “add” and “edit” actions. | |
| The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| podcasts | An array of podcast objects.  The structure of the podcast object is discussed in the Data Structures segment. |
| episodes | An array of podcast episode objects.  The structure of the podcast episode object is discussed in the Data Structures segment. |

## Scrobble
> Used to submit Last.fm song plays and submissions.  Can be used to submit multiple offline events.  Since the last.fm API supports only 100 scrobbles per API call, WaveBox behaves the same way.  Only the first 100 scrobbles in a call will be acted upon.

**URL:** /api/scrobble

**Example:** http://localhost:6500/api/scrobble/125?action=nowPlaying

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| action | Defines what function will be performed for the call.  Valid values are  “auth”, “scrobble”, or “nowPlaying”.  If not specified, WaveBox assumes “auth”. Auth takes no parameters and returns whether or not last.fm is authenticated for the user account associated with the session ID.  If not, it returns a URL to direct the user to in order to do so. For “scrobble”, the event parameter is required. For “nowPlaying”, the id parameter is required. | |
| id | The id of the song being played. Only used with the “nowPlaying” action. | (&#x2713;) |
| event | A comma-delimited list of id/unix-timestamp tuples. All timestamps should be in UTC time zone. Only used with the “scrobble” action. Example: 10,213214621231,1231,213214821231 | (&#x2713;) |
| s | The sessionId of the active session you would like to use to access the API.  Scrobbles will be recorded for this user. | &#x2713; |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| authUrl | Returned when “auth” is called if the user is not authenticated.  Also returned when “scrobble” is called if the scrobble fails because the user is not authenticated. |

## Search
> Returns lists of objects matching the specified parameter ‘query’, optionally filtering types with a comma-separated list.  By default, searches using the item’s name, but can also search on other fields.  In addition, can either fuzzy-search or exact match if specified.

**URL:** /api/search

**Example:** http://localhost:6500/api/search?query=Taylor+Swift

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| query | A search string by which to query for in the database.  By default, the item’s name is queried, unless a ‘field’ parameter is specified. | &#x2713; |
| field | Determines which database field will be queried, when searching for items.  Can be any valid database column in any media database. (e.g. “song_id”, “video_id”, “song_file_name”, etc.) | |
| exact | Determines if WaveBox will only search for exact matches in the database.  If false, will attempt to match using SQLite LIKE operator.  Default is false. | |
| type | Comma-separated list of item types in which to query.  Defaults to all types.  Some examples could be “artists,songs”, “songs,videos”, “artists,albums,songs”, etc. | |
| s | The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| artists | An array of artist objects matching the search query results.   The structure of the artist object is discussed in the Data Structures segment. |
| albums | An array of album objects matching the search query results.   The structure of the album object is discussed in the Data Structures segment. |
| songs | An array of song objects matching the search query results.   The structure of the song object is discussed in the Data Structures segment. |
| videos | An array of video objects matching the search query results.   The structure of the video object is discussed in the Data Structures segment. |

## Settings
> Returns the settings information, or if a JSON object is specified, updates setting(s) by merging it with the current settings.

**URL:** /api/settings

**Example:** http://localhost:6500/api/settings?json={"port":6500}

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| json | JSON object which will overwrite any matching settings, and merge with the existing settings.  Parameter is sent URL encoded, and URL decoded on the server side. |
| s | The sessionId of the active session you would like to use to access the API. |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| settings | An object containing settings information.  The structure of the user object is discussed in the Data Structures segment. |

## Songs
> Returns a list of all songs known to WaveBox.  When a parameter ‘id’ is specified, returns the song with that matching song ID.  This response can be very large, so it is recommended to grab subsets using a combination of the ‘range’ and ‘limit’ parameters where applicable.

**URL:** /api/songs

**Example:** http://localhost:6500/api/songs/125

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The ID of a song in the WaveBox database, which returns a single song object matching this ID. |
| limit | Modifies the requested result set to only include a subset of results, just like SQL LIMIT x,y. |
| range | Fetches a list of all items within the comma-separated alphanumeric range. |
| s | The sessionId of the active session you would like to use to access the API. |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| songs | An array of song objects, which contain information about all songs, or the song matching parameter ‘id’.  The structure of the song object is discussed in the Data Structures segment. |

## Stats
> Currently only used to update server play counts.  In the future, additional statistics may be supported.  May be used to submit multiple offline actions.

**URL:** /api/stats

**Example:** http://localhost:6500/api/stats?event=10,213214621231&s=SESSIONID

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| event | A comma delimited list of id/event-type/unix-timestamp events. All timestamps should be UTC time zone. Example: 10,0,213214621231,1231,0,213214821231,122,0,213214381231,130,0,21321439231 | &#x2713; |
| s | The sessionId of the active session you would like to use to access the API. Stats will be recorded for this user. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. | |

## Status
> Returns the current server status information.  May be used to check if the server is online or for other purposes.

**URL:** /api/status

**Example:** http://localhost:6500/api/status?extended=true

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| extended | Determines if server will return extended status metrics, which involve database heavy operations.  These operations are cached on first run, however, and only need to be regenerated when a destructive operation occurs on the database.  Default: false | |
| s | The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| status | An object containing server status information.  The structure of the status object is discussed in the Data Structures segment. |

## Stream
> Used to retrieve a song or video stream.  Will always return the untouched original media file.

**URL:** /api/stream

**Example:** http://localhost:6500/api/stream/125

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The ID of the media item you would like to stream.  If null, information about all songs known by the server will be retrieved. | &#x2713; |
| s | The sessionId of the active session you would like to use to access the API. |

**Returns**

| Name | Description |
| :--- | :---------- |
| error (if unsuccessful) | An object containing information about any error that may have occurred.  NO JSON IS RETURNED ON SUCCESS, ONLY BINARY DATA.  The structure of the error object is discussed in the Data Structures segment. |

## Transcode
> Used to retrieve a song or video stream.  Returns a transcoded copy of the original file.

**URL:** /api/transcode

**Example:** http://localhost:6500/api/transcode/125?transType=opus&transQuality=medium

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The ID of the media item you would like to stream. | &#x2713; |
| transType | The transcoding format.  For audio, options include: “MP3” (defualt), “AAC”, “OGG”, and “OPUS”.  For video, options include: “MPEGTS”, and “X264” (default) | |
| transQuality | The transcoding quality. Defaults to “Medium”. Options include: “Low”, “Medium”, “High”, “Extreme”, or integer bitrate value.  Word values will produce VBR files while bitrate values will produce CBR files. | |
| isDirect | Sends stream directly from memory to the socket, instead of creating an intermediate file first.  Default: false | |
| offsetSeconds | Used to write a file beginning at a specified time offset, for connection inconsistencies | |
| lengthSeconds | Specify how much of a file to stream, but default to the entire file | |
| estimateContentLength | Determines whether or not WaveBox will attempt to estimate the size of the transcoded file.  Default: false | |
| width | Determines the output width of a transcoded video file | |
| height | Determines the output height of a transcoded video file | |
| maintainAspect | Determines if WaveBox will maintain the aspect ratio of an input video file.  Default: true | |
| s | The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| Transcoded file | A data stream of the transcoded input file, for playing in clients |
| error (if unsuccessful) | An object containing information about any error that may have occurred.  NO JSON IS RETURNED ON SUCCESS, ONLY BINARY DATA.  The structure of the error object is discussed in the Data Structures segment. |

## TranscodeHls
>

**URL:** /api/transcodehls

**Example:** http://localhost:6500/api/transcode/125

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The ID of the media item you would like to stream. | &#x2713; |
| width | Determines the output width of a transcoded video file | |
| height | Determines the output height of a transcoded video file | |
| maintainAspect | Determines if WaveBox will maintain the aspect ratio of an input video file.  Default: true | |
| s | The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| Transcoded file | A data stream of the transcoded input file, for playing in clients |
| error (if unsuccessful) | An object containing information about any error that may have occurred.  NO JSON IS RETURNED ON SUCCESS, ONLY BINARY DATA.  The structure of the error object is discussed in the Data Structures segment. |

## Users
> Used to retrieve or update user information.

**URL:** /api/users

**Example:** http://localhost:6500/api/users/125

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The ID of the user. | |
| action | Action you would like to perform.  Possible actions include “killSession”, which will remove a session with specified ‘rowId’. | |
| rowId | Used to identify a certain session, in conjunction with an ‘action’ parameter. | |
| testUser | Used to create a test user, with an optional expiration time of ‘durationSeconds’. | |
| durationSeconds | Used to set the duration for which a test account may remain active.  The account is deleted after this time. | |
| s | The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| user | An object containing user information.  The structure of the user object is discussed in the Data Structures segment. |

## Videos
> Returns a list of all videos known to WaveBox.  When a parameter ‘id’ is specified, returns the video with that matching video ID.

**URL:** /api/videos

**Example:** http://localhost:6500/api/videos/125

**Parameters**

| Name | Description | Required |
| :--- | :---------- | :------: |
| id | The ID of a video in the WaveBox database, which returns a single video object matching this ID. | |
| s | The sessionId of the active session you would like to use to access the API. | |

**Returns**

| Name | Description |
| :--- | :---------- |
| error | An object containing information about any error that may have occurred.  Null if the call is successful.  The structure of the error object is discussed in the Data Structures segment. |
| songs | An array of video objects, which contain information about all videos, or the video matching parameter ‘id’.  The structure of the video object is discussed in the Data Structures segment. |

