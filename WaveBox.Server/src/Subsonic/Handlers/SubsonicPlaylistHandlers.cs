using System;
using System.Collections.Generic;
using System.Linq;
using WaveBox.Api;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Subsonic.Handlers {
    public static class SubsonicPlaylistHandlers {
        public static void GetPlaylists(SubsonicRequest req, HttpContextProcessor processor, User user) {
            List<SubsonicPlaylist> playlists = Injection.Get<IPlaylistRepository>().AllPlaylists()
                    .Where(p => p.PlaylistId != null)
                    .Select(p => SubsonicMapper.PlaylistFromPlaylist(p, user.UserName))
                    .ToList();

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Playlists = new SubsonicPlaylists { Playlist = playlists };
            SubsonicWriter.Write(req, processor, body);
        }

        public static void GetPlaylist(SubsonicRequest req, HttpContextProcessor processor, User user) {
            int? id = req.GetInt("id");
            if (id == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter id is missing");
                return;
            }

            Playlist playlist = Injection.Get<IPlaylistRepository>().PlaylistForId((int)id);
            if (playlist == null || playlist.PlaylistId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "Playlist not found");
                return;
            }

            WritePlaylistWithSongs(req, processor, user, playlist);
        }

        public static void CreatePlaylist(SubsonicRequest req, HttpContextProcessor processor, User user) {
            int? playlistId = req.GetInt("playlistId");
            string name = req.Get("name");
            IList<int> songIds = req.GetIntList("songId");

            IPlaylistRepository playlistRepository = Injection.Get<IPlaylistRepository>();
            Playlist playlist;

            if (playlistId != null) {
                // Updating an existing playlist: spec semantics are full replacement
                playlist = playlistRepository.PlaylistForId((int)playlistId);
                if (playlist == null || playlist.PlaylistId == null) {
                    SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "Playlist not found");
                    return;
                }
                playlist.ClearPlaylist();
            } else if (!String.IsNullOrEmpty(name)) {
                Playlist existing = playlistRepository.PlaylistForName(name);
                if (existing != null && existing.PlaylistId != null) {
                    SubsonicWriter.WriteError(req, processor, SubsonicError.Generic, "A playlist named " + name + " already exists");
                    return;
                }
                playlist = new Playlist { PlaylistName = name };
                playlist.CreatePlaylist();
            } else {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter name or playlistId is missing");
                return;
            }

            if (songIds.Count > 0) {
                playlist.AddMediaItems(songIds.ToList());
            }

            // Re-read so counts/duration reflect the mutation
            playlist = playlistRepository.PlaylistForId((int)playlist.PlaylistId);
            WritePlaylistWithSongs(req, processor, user, playlist);
        }

        public static void UpdatePlaylist(SubsonicRequest req, HttpContextProcessor processor, User user) {
            int? playlistId = req.GetInt("playlistId");
            if (playlistId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter playlistId is missing");
                return;
            }

            Playlist playlist = Injection.Get<IPlaylistRepository>().PlaylistForId((int)playlistId);
            if (playlist == null || playlist.PlaylistId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "Playlist not found");
                return;
            }

            string name = req.Get("name");
            if (!String.IsNullOrEmpty(name)) {
                playlist.PlaylistName = name;
                playlist.UpdateDatabase();
            }

            // Remove before add, positions are 0-based (matches WaveBox ItemPosition);
            // RemoveMediaItemAtIndexes re-packs positions itself
            IList<int> removeIndexes = req.GetIntList("songIndexToRemove");
            if (removeIndexes.Count > 0) {
                playlist.RemoveMediaItemAtIndexes(removeIndexes.ToList());
            }

            IList<int> addIds = req.GetIntList("songIdToAdd");
            if (addIds.Count > 0) {
                playlist.AddMediaItems(addIds.ToList());
            }

            SubsonicWriter.Write(req, processor, SubsonicWriter.Body());
        }

        public static void DeletePlaylist(SubsonicRequest req, HttpContextProcessor processor, User user) {
            int? id = req.GetInt("id");
            if (id == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.MissingParameter, "Required parameter id is missing");
                return;
            }

            Playlist playlist = Injection.Get<IPlaylistRepository>().PlaylistForId((int)id);
            if (playlist == null || playlist.PlaylistId == null) {
                SubsonicWriter.WriteError(req, processor, SubsonicError.NotFound, "Playlist not found");
                return;
            }

            playlist.DeletePlaylist();
            SubsonicWriter.Write(req, processor, SubsonicWriter.Body());
        }

        private static void WritePlaylistWithSongs(SubsonicRequest req, HttpContextProcessor processor, User user, Playlist playlist) {
            SubsonicPlaylistWithSongs dto = new SubsonicPlaylistWithSongs();
            SubsonicMapper.FillPlaylist(dto, playlist, user.UserName);
            dto.Entry = playlist.ListOfMediaItems()
                        .Select(SubsonicMapper.ChildFromMediaItem)
                        .Where(c => c != null)
                        .ToList();

            SubsonicResponseBody body = SubsonicWriter.Body();
            body.Playlist = dto;
            SubsonicWriter.Write(req, processor, body);
        }
    }
}
