using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Text.Json.Serialization;
using WaveBox.Core.Model;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model {
    public class Folder : IItem, IGroupingItem {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public int? ItemId { get { return FolderId; } set { FolderId = ItemId; } }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public ItemType ItemType { get { return ItemType.Folder; } }

        [JsonPropertyName("itemTypeId"), IgnoreRead, IgnoreWrite]
        public int ItemTypeId { get { return (int)ItemType; } }

        [JsonPropertyName("folderId")]
        public int? FolderId { get; set; }

        [JsonPropertyName("folderName")]
        public string FolderName { get; set; }

        [JsonPropertyName("parentFolderId")]
        public int? ParentFolderId { get; set; }

        [JsonPropertyName("mediaFolderId")]
        public int? MediaFolderId { get; set; }

        [JsonPropertyName("folderPath")]
        public string FolderPath { get; set; }

        [JsonPropertyName("artId"), IgnoreRead, IgnoreWrite]
        public int? ArtId { get { return Injection.Get<IArtRepository>().ArtIdForItemId(FolderId); } }

        [JsonIgnore, IgnoreRead, IgnoreWrite]
        public string GroupingName { get { return FolderName; } }

        /// <summary>
        /// Constructors
        /// </summary>

        public Folder() {
        }

        public Folder ParentFolder() {
            return Injection.Get<IFolderRepository>().FolderForId((int)ParentFolderId);
        }

        public static void Scan() {
            // TO DO: scanning!  yay!
        }

        public IList<IMediaItem> ListOfMediaItems() {
            List<IMediaItem> mediaItems = new List<IMediaItem>();

            mediaItems.AddRange(ListOfSongs());
            mediaItems.AddRange(ListOfVideos());

            return mediaItems;
        }

        public IList<Song> ListOfSongs(bool recursive = false) {
            if (FolderId == null) {
                return new List<Song>();
            }

            return Injection.Get<IFolderRepository>().ListOfSongs((int)FolderId, recursive);
        }

        public IList<Video> ListOfVideos(bool recursive = false) {
            if (FolderId == null) {
                return new List<Video>();
            }

            return Injection.Get<IFolderRepository>().ListOfVideos((int)FolderId, recursive);
        }

        public IList<Folder> ListOfSubFolders() {
            if (FolderId == null) {
                return new List<Folder>();
            }

            return Injection.Get<IFolderRepository>().ListOfSubFolders((int)FolderId);
        }

        public bool IsMediaFolder() {
            Folder mFolder = MediaFolder();

            if (mFolder != null) {
                return true;
            }

            return false;
        }

        private Folder MediaFolder() {
            foreach (Folder mediaFolder in Injection.Get<IFolderRepository>().MediaFolders()) {
                if (FolderPath == mediaFolder.FolderPath) {
                    return mediaFolder;
                }
            }

            return null;
        }

        public bool InsertFolder(bool isMediaFolder) {
            int? itemId = Injection.Get<IItemRepository>().GenerateItemId(ItemType.Folder);
            if (itemId == null) {
                return false;
            }

            this.FolderId = itemId;
            if (!isMediaFolder) {
                this.ParentFolderId = Injection.Get<IFolderRepository>().GetParentFolderId(this.FolderPath);
            }

            return Injection.Get<IFolderRepository>().InsertFolder(this);
        }

        public override string ToString() {
            return String.Format("[Folder: ItemId={0}, FolderName={1}]", this.ItemId, this.FolderName);
        }

        public static int CompareFolderByName(Folder x, Folder y) {
            return StringComparer.OrdinalIgnoreCase.Compare(x.FolderName, y.FolderName);
        }
    }
}
