using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Gallery.DataModel
{
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class ControlInfoDataItem
    {
        public ControlInfoDataItem(string uniqueId, string title, string subtitle, string imagePath, string imageIconPath, string badgeString, string description, string content, bool isNew, bool isUpdated, bool isPreview)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath.Replace("ms-appx://", string.Empty);
            this.ImageIconPath = imageIconPath.Replace("ms-appx://", string.Empty);
            this.BadgeString = badgeString;
            this.Content = content;
            this.IsNew = isNew;
            this.IsUpdated = isUpdated;
            this.IsPreview = isPreview;
            this.Docs = new ObservableCollection<ControlInfoDocLink>();
            this.RelatedControlsIds = new ObservableCollection<string>();
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public string ImageIconPath { get; private set; }
        public string BadgeString { get; private set; }
        public string Content { get; private set; }
        public bool IsNew { get; private set; }
        public bool IsUpdated { get; private set; }
        public bool IsPreview { get; private set; }
        public ObservableCollection<ControlInfoDocLink> Docs { get; private set; }
        public ObservableCollection<string> RelatedControlsIds { get; private set; }

        public IList<ControlInfoDataItem> RelatedControls
        {
            get
            {
                return ControlInfoDataSource.SelectMany(group => group.Items).Where(item => RelatedControlsIds.Contains(item.UniqueId)).ToList();
            }
        }

        public bool IncludedInBuild { get; set; }

        public override string ToString()
        {
            return this.Title;
        }

        public ControlInfoDataGroup Parent { get; set; }
    }

    public class ControlInfoDocLink
    {
        public ControlInfoDocLink(string title, string uri)
        {
            this.Title = title;
            this.Uri = uri;
        }
        public string Title { get; private set; }
        public string Uri { get; private set; }
    }


    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class ControlInfoDataGroup
    {
        public ControlInfoDataGroup(string uniqueId, string title, string subtitle, string imagePath, string imageIconPath, string description)
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath.Replace("ms-appx://", string.Empty);
            this.ImageIconPath = imageIconPath.Replace("ms-appx://", string.Empty);
            this.Items = new ObservableCollection<ControlInfoDataItem>();
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public string ImageIconPath { get; private set; }
        public ObservableCollection<ControlInfoDataItem> Items { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }

        public ControlInfoDataRealm Parent { get; set; }
    }

    /// <summary>
    /// The realm that contains all the data for the control info.
    /// Should be Windows, Community and Extended.
    /// </summary>
    public class ControlInfoDataRealm
    {
        public string UniqueId { get; set; }
        public string Title { get; set; }

        public bool IsVisible { get; set; } = true;

        public ObservableCollection<ControlInfoDataGroup> Groups { get; set; }

        public ControlInfoDataRealm()
        {
            Groups = new ObservableCollection<ControlInfoDataGroup>();
        }
    }

        /// <summary>
        /// Creates a collection of groups and items with content read from a static json file.
        ///
        /// ControlInfoSource initializes with data read from a static json file included in the
        /// project.  This provides sample data at both design-time and run-time.
        /// </summary>
        public sealed class ControlInfoDataSource
    {
        private static readonly object _lock = new object();

        #region Singleton

        private static ControlInfoDataSource _instance;

        public static ControlInfoDataSource Instance
        {
            get
            {
                return _instance;
            }
        }

        static ControlInfoDataSource()
        {
            _instance = new ControlInfoDataSource();
        }

        private ControlInfoDataSource() { }

        #endregion

        private IList<ControlInfoDataRealm> _realms = new List<ControlInfoDataRealm>();
        public IList<ControlInfoDataRealm> Realms
        {
            get { return this._realms; }
        }

        public IList<ControlInfoDataGroup> AllGroups
        {
            get
            {
                return this.Realms.SelectMany(r => r.Groups).ToList();
            }
        }

        public async Task<IEnumerable<ControlInfoDataRealm>> GetRealmsAsync()
        {
            await _instance.GetControlInfoDataAsync();

            return _instance.Realms;
        }

        public async Task<ControlInfoDataRealm> GetRealmAsync(string realmName)
        {
            await _instance.GetControlInfoDataAsync();
            var matches = _instance.Realms.Where((realm) => realm.UniqueId.Equals(realmName));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public async Task<ControlInfoDataGroup> GetGroupAsync(ControlInfoDataRealm realm, string uniqueId)
        {
            await _instance.GetControlInfoDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = realm.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public async Task<ControlInfoDataItem> GetItemAsync(ControlInfoDataRealm realm, string uniqueId)
        {
            await _instance.GetControlInfoDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = realm.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() > 0) return matches.First();
            return null;
        }

        public static IEnumerable<TResult> SelectMany<TResult>(Func<ControlInfoDataGroup, IEnumerable<TResult>> selector)
        {
            var result = new List<TResult>();
            foreach (var realm in _instance.Realms)
            {
                result.AddRange(realm.Groups.SelectMany(selector));
            }

            return result;
        }

        public async Task<ControlInfoDataGroup> GetGroupFromItemAsync(string uniqueId)
        {
            await _instance.GetControlInfoDataAsync();
            // var matches = _instance.Groups.Where((group) => group.Items.FirstOrDefault(item => item.UniqueId.Equals(uniqueId)) != null);
            var matches = new List<ControlInfoDataGroup>();

            foreach (var realm in _instance.Realms)
            {
                var match = realm.Groups.Where(group => group.Items.FirstOrDefault(item => item.UniqueId.Equals(uniqueId)) != null);
                matches.AddRange(match);
            }

            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task GetControlInfoDataAsync()
        {
            lock (_lock)
            {
                if (this.Realms.Count() != 0)
                {
                    return;
                }
            }

            var realmNames = new string[] { "Windows", "Community", "Extended", "Foundation" };

            foreach (var realmName in realmNames)
            {
                var dataUri = new Uri($"/DataModel/Data/Controls.{realmName}.json", UriKind.Relative);

                var file = Application.GetResourceStream(dataUri);
                string jsonText = string.Empty;

                using (var reader = new StreamReader(file.Stream))
                {
                    await Task.Run(() => jsonText = reader.ReadToEnd());
                }

                var realm = new ControlInfoDataRealm() { UniqueId = realmName, Title = realmName };

                JsonDocument jsonDocument = JsonDocument.Parse(jsonText, new JsonDocumentOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                JsonElement jsonArray = jsonDocument.RootElement.GetProperty("Groups");

                if (jsonDocument.RootElement.TryGetProperty("IsVisible", out JsonElement isVisibleElement))
                {
                    realm.IsVisible = isVisibleElement.GetBoolean();
                }

                // Set IsVisible property from JSON


                lock (_lock)
                {
                    string pageRoot = $"iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.{realmName}.";
                    foreach (JsonElement groupValue in jsonArray.EnumerateArray())
                    {
                        if (groupValue.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        JsonElement groupObject;
                        if (groupValue.TryGetProperty("UniqueId", out JsonElement uniqueIdElement) &&
                            groupValue.TryGetProperty("Title", out JsonElement titleElement) &&
                            groupValue.TryGetProperty("Subtitle", out JsonElement subtitleElement) &&
                            groupValue.TryGetProperty("ImagePath", out JsonElement imagePathElement) &&
                            groupValue.TryGetProperty("ImageIconPath", out JsonElement imageIconPathElement) &&
                            groupValue.TryGetProperty("Description", out JsonElement descriptionElement))
                        {
                            groupObject = groupValue;
                        }
                        else
                        {
                            continue;
                        }

                        ControlInfoDataGroup group = new ControlInfoDataGroup(uniqueIdElement.GetString(),
                            titleElement.GetString(), subtitleElement.GetString(),
                            imagePathElement.GetString(), imageIconPathElement.GetString(),
                            descriptionElement.GetString())
                        { Parent = realm };

                        if (groupObject.TryGetProperty("Items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement itemValue in itemsElement.EnumerateArray())
                            {
                                if (itemValue.ValueKind != JsonValueKind.Object)
                                {
                                    continue;
                                }

                                JsonElement itemObject;
                                if (itemValue.TryGetProperty("UniqueId", out JsonElement itemUniqueIdElement) &&
                                    itemValue.TryGetProperty("Title", out JsonElement itemTitleElement) &&
                                    itemValue.TryGetProperty("Subtitle", out JsonElement itemSubtitleElement) &&
                                    itemValue.TryGetProperty("ImagePath", out JsonElement itemImagePathElement) &&
                                    itemValue.TryGetProperty("ImageIconPath", out JsonElement itemImageIconPathElement) &&
                                    itemValue.TryGetProperty("Description", out JsonElement itemDescriptionElement))
                                {
                                    itemObject = itemValue;
                                }
                                else
                                {
                                    continue;
                                }

                                var itemContentElement = itemValue.TryGetProperty("Content");

                                string badgeString = null;

                                bool isNew = itemObject.TryGetProperty("IsNew", out JsonElement isNewElement) && isNewElement.GetBoolean();
                                bool isUpdated = itemObject.TryGetProperty("IsUpdated", out JsonElement isUpdatedElement) && isUpdatedElement.GetBoolean();
                                bool isPreview = itemObject.TryGetProperty("IsPreview", out JsonElement isPreviewElement) && isPreviewElement.GetBoolean();

                                if (isNew)
                                {
                                    badgeString = "New";
                                }
                                else if (isUpdated)
                                {
                                    badgeString = "Updated";
                                }
                                else if (isPreview)
                                {
                                    badgeString = "Preview";
                                }

                                var item = new ControlInfoDataItem(itemUniqueIdElement.GetString(),
                                    itemTitleElement.GetString(),
                                    itemSubtitleElement.GetString(),
                                    itemImagePathElement.GetString(),
                                    itemImageIconPathElement.GetString(),
                                    badgeString,
                                    itemDescriptionElement.GetString(),
                                    itemContentElement?.GetString() ?? "",
                                    isNew,
                                    isUpdated,
                                    isPreview)
                                { Parent = group };

                                {
                                    string pageString = pageRoot + item.UniqueId + "Page";
                                    Type pageType = Type.GetType(pageString);
                                    item.IncludedInBuild = pageType != null;
                                }

                                if (itemObject.TryGetProperty("Docs", out JsonElement docsElement) && docsElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (JsonElement docValue in docsElement.EnumerateArray())
                                    {
                                        if (docValue.ValueKind != JsonValueKind.Object)
                                        {
                                            continue;
                                        }

                                        if (docValue.TryGetProperty("Title", out JsonElement docTitleElement) &&
                                            docValue.TryGetProperty("Uri", out JsonElement docUriElement))
                                        {
                                            item.Docs.Add(new ControlInfoDocLink(docTitleElement.GetString(), docUriElement.GetString()));
                                        }
                                    }
                                }

                                if (itemObject.TryGetProperty("RelatedControls", out JsonElement relatedControlsElement) && relatedControlsElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (JsonElement relatedControlValue in relatedControlsElement.EnumerateArray())
                                    {
                                        if (relatedControlValue.ValueKind == JsonValueKind.String)
                                        {
                                            item.RelatedControlsIds.Add(relatedControlValue.GetString());
                                        }
                                    }
                                }

                                group.Items.Add(item);
                            }
                            if (!realm.Groups.Any(g => g.Title == group.Title))
                            {
                                realm.Groups.Add(group);
                            }
                        }
                    }
                }
            
                this.Realms.Add(realm);
            }
        }
    }
}
