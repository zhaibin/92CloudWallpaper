using _92CloudWallpaper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

public class ImageCacheManager
{
    private HttpClient httpClient;
    private static string tempPath;
    private static string cacheDirectory;
    private static string databaseFilePath;

    public Dictionary<string, ImageCacheItem> ImageCache { get; private set; }
    public List<ImageInfo> ImageInfos { get; private set; }
    private readonly int screenWidth = Screen.PrimaryScreen.Bounds.Width;
    private readonly int screenHeight = Screen.PrimaryScreen.Bounds.Height;
    private readonly string appImageUrl = "https://cnapi.levect.com/v1/photoFrame/imageList";
    public int CurrentIndex { get; set; }

    public ImageCacheManager()
    {
        httpClient = new HttpClient();
        ImageCache = new Dictionary<string, ImageCacheItem>();
        ImageInfos = new List<ImageInfo>();
        tempPath = Path.GetTempPath();
        cacheDirectory = Path.Combine(tempPath, "CloudWallpaper", "U_" + GlobalData.UserId);
        databaseFilePath = Path.Combine(cacheDirectory, "cache.db");
        InitializeCacheDirectory();
        InitializeDatabase();
        LoadMetadata();
        LoadCurrentPosition();
    }

    public async Task LoadImagesAsync()
    {
        int pageIndex = GlobalData.PageIndex;
        int pageSize = 8;
        var apiHandler = new ApiRequestHandler();
        var body = new SortedDictionary<string, object>
        {
            { "userId", GlobalData.UserId },
            { "height", screenHeight },
            { "pageIndex", pageIndex },
            { "pageSize", pageSize },
            { "width", screenWidth }
        };

        bool morePages = true;
        HashSet<string> newImageUrls = new HashSet<string>();

        while (morePages)
        {
            var response = await apiHandler.SendApiRequestAsync(appImageUrl, body).ConfigureAwait(false);
            Console.WriteLine(response);
            var newImageInfos = ParseImageInfo(response);

            if (newImageInfos.Count == 0)
            {
                morePages = false;
            }
            else
            {
                foreach (var imageInfo in newImageInfos)
                {
                    newImageUrls.Add(imageInfo.Url);
                    if (!ImageCache.ContainsKey(imageInfo.Url))
                    {
                        ImageInfos.Add(imageInfo);
                        await CacheImageAsync(imageInfo);
                    }
                }
                pageIndex++;
                body["pageIndex"] = pageIndex;
            }
        }

        SyncCache(newImageUrls);
    }

    private List<ImageInfo> ParseImageInfo(string jsonResponse)
    {
        var jsonDocument = JsonDocument.Parse(jsonResponse);
        var listElement = jsonDocument.RootElement.GetProperty("body").GetProperty("list");
        var imageInfos = new List<ImageInfo>();

        foreach (var element in listElement.EnumerateArray())
        {
            var imageInfo = new ImageInfo
            {
                Url = element.GetProperty("url").GetString(),
                Description = element.GetProperty("content").GetString(),
                Location = element.GetProperty("addr").GetString(),
                ShootTime = element.GetProperty("shootTime").GetString(),
                ShootAddr = element.GetProperty("shootAddr").GetString(),
                AuthorUrl = element.GetProperty("authorUrl").GetString(),
                AuthorName = element.GetProperty("authorName").GetString()
            };
            imageInfos.Add(imageInfo);
        }

        return imageInfos;
    }

    private async Task CacheImageAsync(ImageInfo imageInfo)
    {
        if (!ImageCache.ContainsKey(imageInfo.Url))
        {
            var filePath = Path.Combine(cacheDirectory, Path.GetFileName(imageInfo.Url) + ".webp");

            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            {
                ImageCache[imageInfo.Url] = new ImageCacheItem { FilePath = filePath, Info = imageInfo };
                SaveMetadata(imageInfo.Url, filePath, imageInfo.Description, imageInfo.Location, imageInfo.ShootTime, imageInfo.ShootAddr, imageInfo.AuthorUrl, imageInfo.AuthorName);
                return;
            }

            bool downloadSuccess = await DownloadImageAsync(imageInfo.Url, filePath);

            if (downloadSuccess)
            {
                ImageCache[imageInfo.Url] = new ImageCacheItem { FilePath = filePath, Info = imageInfo };
                SaveMetadata(imageInfo.Url, filePath, imageInfo.Description, imageInfo.Location, imageInfo.ShootTime, imageInfo.ShootAddr, imageInfo.AuthorUrl, imageInfo.AuthorName);
            }
        }
    }

    private async Task<bool> DownloadImageAsync(string url, string filePath)
    {
        int maxRetry = 3;
        for (int retry = 0; retry < maxRetry; retry++)
        {
            try
            {
                var bytes = await httpClient.GetByteArrayAsync(url);
                await Task.Run(() => File.WriteAllBytes(filePath, bytes));
                return true;
            }
            catch
            {
                if (retry == maxRetry - 1)
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    return false;
                }
            }
        }
        return false;
    }

    private void SyncCache(HashSet<string> newImageUrls)
    {
        var oldImageUrls = ImageCache.Keys.ToList();
        foreach (var url in oldImageUrls)
        {
            if (!newImageUrls.Contains(url))
            {
                var filePath = ImageCache[url].FilePath;
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                ImageCache.Remove(url);
                ImageInfos.RemoveAll(info => info.Url == url);
                DeleteMetadata(url);
            }
        }
    }

    private void InitializeCacheDirectory()
    {
        if (!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }
    }

    private void InitializeDatabase()
    {
        if (!File.Exists(databaseFilePath))
        {
            SQLiteConnection.CreateFile(databaseFilePath);
        }

        using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;"))
        {
            connection.Open();
            string createTableQuery = @"CREATE TABLE IF NOT EXISTS ImageCache (
                                        Url TEXT PRIMARY KEY,
                                        FilePath TEXT,
                                        Description TEXT,
                                        Location TEXT,
                                        ShootTime TEXT,
                                        ShootAddr TEXT,
                                        AuthorUrl TEXT,
                                        AuthorName TEXT)";
            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            // Check and add missing columns
            var columnCheckQueries = new List<string>
            {
                "PRAGMA table_info(ImageCache)"
            };

            using (var command = new SQLiteCommand(columnCheckQueries[0], connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    var columns = new HashSet<string>();
                    while (reader.Read())
                    {
                        columns.Add(reader["name"].ToString());
                    }

                    if (!columns.Contains("AuthorUrl"))
                    {
                        string addColumnQuery = "ALTER TABLE ImageCache ADD COLUMN AuthorUrl TEXT";
                        using (var addColumnCommand = new SQLiteCommand(addColumnQuery, connection))
                        {
                            addColumnCommand.ExecuteNonQuery();
                        }
                    }

                    if (!columns.Contains("AuthorName"))
                    {
                        string addColumnQuery = "ALTER TABLE ImageCache ADD COLUMN AuthorName TEXT";
                        using (var addColumnCommand = new SQLiteCommand(addColumnQuery, connection))
                        {
                            addColumnCommand.ExecuteNonQuery();
                        }
                    }
                }
            }

            string createPositionTableQuery = @"CREATE TABLE IF NOT EXISTS CarouselPosition (
                                                Id INTEGER PRIMARY KEY,
                                                CurrentIndex INTEGER)";
            using (var command = new SQLiteCommand(createPositionTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    private void LoadMetadata()
    {
        if (!File.Exists(databaseFilePath))
        {
            return;
        }

        using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;"))
        {
            connection.Open();
            string selectQuery = "SELECT * FROM ImageCache";
            using (var command = new SQLiteCommand(selectQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var url = reader["Url"].ToString();
                        var filePath = reader["FilePath"].ToString();
                        var description = reader["Description"].ToString();
                        var location = reader["Location"].ToString();
                        var shootTime = reader["ShootTime"].ToString();
                        var shootAddr = reader["ShootAddr"].ToString();
                        var authorUrl = reader["AuthorUrl"].ToString();
                        var authorName = reader["AuthorName"].ToString();

                        if (File.Exists(filePath))
                        {
                            var imageInfo = new ImageInfo
                            {
                                Url = url,
                                Description = description,
                                Location = location,
                                ShootTime = shootTime,
                                ShootAddr = shootAddr,
                                AuthorUrl = authorUrl,
                                AuthorName = authorName
                            };
                            ImageCache[url] = new ImageCacheItem { FilePath = filePath, Info = imageInfo };
                            ImageInfos.Add(imageInfo);
                        }
                    }
                }
            }
        }
    }

    private void SaveMetadata(string url, string filePath, string description, string location, string shootTime, string shootAddr, string authorUrl, string authorName)
    {
        using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;"))
        {
            connection.Open();
            string insertQuery = @"REPLACE INTO ImageCache (Url, FilePath, Description, Location, ShootTime, ShootAddr, AuthorUrl, AuthorName)
                                   VALUES (@url, @filePath, @description, @location, @shootTime, @shootAddr, @authorUrl, @authorName)";
            using (var command = new SQLiteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@url", url);
                command.Parameters.AddWithValue("@filePath", filePath);
                command.Parameters.AddWithValue("@description", description);
                command.Parameters.AddWithValue("@location", location);
                command.Parameters.AddWithValue("@shootTime", shootTime);
                command.Parameters.AddWithValue("@shootAddr", shootAddr);
                command.Parameters.AddWithValue("@authorUrl", authorUrl);
                command.Parameters.AddWithValue("@authorName", authorName);
                command.ExecuteNonQuery();
            }
        }
    }

    private void DeleteMetadata(string url)
    {
        using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;"))
        {
            connection.Open();
            string deleteQuery = "DELETE FROM ImageCache WHERE Url = @url";
            using (var command = new SQLiteCommand(deleteQuery, connection))
            {
                command.Parameters.AddWithValue("@url", url);
                command.ExecuteNonQuery();
            }
        }
    }

    public void SaveCurrentPosition(int currentIndex)
    {
        using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;"))
        {
            connection.Open();
            string insertQuery = @"REPLACE INTO CarouselPosition (Id, CurrentIndex)
                                   VALUES (1, @currentIndex)";
            using (var command = new SQLiteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@currentIndex", currentIndex);
                command.ExecuteNonQuery();
            }
        }
    }

    public void LoadCurrentPosition()
    {
        using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;"))
        {
            connection.Open();
            string selectQuery = "SELECT CurrentIndex FROM CarouselPosition WHERE Id = 1";
            using (var command = new SQLiteCommand(selectQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        CurrentIndex = Convert.ToInt32(reader["CurrentIndex"]);
                    }
                }
            }
        }
    }

    public class ImageInfo
    {
        public string Url { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string ShootTime { get; set; }
        public string ShootAddr { get; set; }
        public string AuthorUrl { get; set; }
        public string AuthorName { get; set; }
    }

    public class ImageCacheItem
    {
        public string FilePath { get; set; }
        public ImageInfo Info { get; set; }
    }
}
