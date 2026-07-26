using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Model;

namespace WaveBox.Core {
    // Source-generated System.Text.Json context (required for NativeAOT).
    // Every type serialized or deserialized via STJ must be registered here — including every
    // runtime type placed into object-valued dictionaries like StatusResponse.Status.
    [JsonSourceGenerationOptions(WriteIndented = false)]
    // API responses
    [JsonSerializable(typeof(AlbumArtistsResponse))]
    [JsonSerializable(typeof(AlbumsResponse))]
    [JsonSerializable(typeof(ArtistsResponse))]
    [JsonSerializable(typeof(DatabaseResponse))]
    [JsonSerializable(typeof(ErrorResponse))]
    [JsonSerializable(typeof(ExternalArtResponse))]
    [JsonSerializable(typeof(FavoritesResponse))]
    [JsonSerializable(typeof(FoldersResponse))]
    [JsonSerializable(typeof(GenresResponse))]
    [JsonSerializable(typeof(JukeboxResponse))]
    [JsonSerializable(typeof(LoginResponse))]
    [JsonSerializable(typeof(LogoutResponse))]
    [JsonSerializable(typeof(NowPlayingResponse))]
    [JsonSerializable(typeof(PlaylistsResponse))]
    [JsonSerializable(typeof(ScrobbleResponse))]
    [JsonSerializable(typeof(SearchResponse))]
    [JsonSerializable(typeof(SettingsResponse))]
    [JsonSerializable(typeof(SongsResponse))]
    [JsonSerializable(typeof(StatsResponse))]
    [JsonSerializable(typeof(StatusResponse))]
    [JsonSerializable(typeof(StreamResponse))]
    [JsonSerializable(typeof(TranscodeHlsResponse))]
    [JsonSerializable(typeof(TranscodeResponse))]
    [JsonSerializable(typeof(UsersResponse))]
    [JsonSerializable(typeof(VideosResponse))]
    // Models embedded in responses
    [JsonSerializable(typeof(Album))]
    [JsonSerializable(typeof(AlbumArtist))]
    [JsonSerializable(typeof(Art))]
    [JsonSerializable(typeof(Artist))]
    [JsonSerializable(typeof(Favorite))]
    [JsonSerializable(typeof(Folder))]
    [JsonSerializable(typeof(Genre))]
    [JsonSerializable(typeof(JukeboxStatus))]
    [JsonSerializable(typeof(MediaItem))]
    [JsonSerializable(typeof(NowPlaying))]
    [JsonSerializable(typeof(Playlist))]
    [JsonSerializable(typeof(QueryLog))]
    [JsonSerializable(typeof(ServerSettingsData))]
    [JsonSerializable(typeof(Session))]
    [JsonSerializable(typeof(Song))]
    [JsonSerializable(typeof(Stat))]
    [JsonSerializable(typeof(User))]
    [JsonSerializable(typeof(Video))]
    // Collection shapes used by response DTOs
    [JsonSerializable(typeof(PairList<string, int>))]
    [JsonSerializable(typeof(Dictionary<string, int>))]
    [JsonSerializable(typeof(IDictionary<string, object>))]
    [JsonSerializable(typeof(IList<IItem>))]
    [JsonSerializable(typeof(IList<IMediaItem>))]
    // Runtime types placed in object-valued dictionaries (status/stats payloads)
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(IList<string>))]
    public partial class WaveBoxJsonContext : JsonSerializerContext {
    }
}
