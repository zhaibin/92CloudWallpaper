using _92CloudWallpaper;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.Linq;

public class ImageCacheManager
{
    private HttpClient httpClient;
    private static string tempPath;
    private static string cacheDirectory;
    private static string databaseFilePath;
    private static string cacheRootDirectory;

    public Dictionary<string, ImageCacheItem> ImageCache { get; private set; }
    public List<ImageInfo> ImageInfos { get; private set; }
    private readonly int screenWidth = Screen.PrimaryScreen.Bounds.Width;
    private readonly int screenHeight = Screen.PrimaryScreen.Bounds.Height;
    private readonly string appImageUrl = "https://cnapi.levect.com/v1/photoFrame/imageList";
    public int CurrentIndex { get; set; }

    public ImageCacheManager()
    {
        try
        {
            httpClient = new HttpClient();
            ImageCache = new Dictionary<string, ImageCacheItem>();
            ImageInfos = new List<ImageInfo>();
            tempPath = Path.GetTempPath();
            cacheRootDirectory = Path.Combine(tempPath, "CloudWallpaper");
            cacheDirectory = Path.Combine(cacheRootDirectory, "U_" + GlobalData.UserId);
            databaseFilePath = Path.Combine(cacheDirectory, "cache.db");
            InitializeCacheDirectory();
            InitializeDatabase();
            LoadMetadata();
            LoadCurrentPosition();
        }
        catch (Exception ex)
        {
            Logger.LogError("Error during ImageCacheManager initialization", ex);
        }
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
                foreach (var newImageInfo in newImageInfos)
                {
                    newImageUrls.Add(newImageInfo.Url);

                    if (ImageCache.TryGetValue(newImageInfo.Url, out var cachedItem))
                    {
                        if (!ImageInfoEquals(cachedItem.Info, newImageInfo))
                        {
                            // Image info has changed, update cache
                            ImageInfos.Remove(cachedItem.Info);
                            ImageInfos.Add(newImageInfo);
                            await CacheImageAsync(newImageInfo, true);
                        }
                    }
                    else
                    {
                        ImageInfos.Add(newImageInfo);
                        await CacheImageAsync(newImageInfo, false);
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

    private async Task CacheImageAsync(ImageInfo imageInfo, bool updateExisting)
    {
        var filePath = Path.Combine(cacheDirectory, Path.GetFileName(imageInfo.Url) + ".webp");

        if (updateExisting && File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        bool downloadSuccess = await DownloadImageAsync(imageInfo.Url, filePath);

        if (downloadSuccess)
        {
            ImageCache[imageInfo.Url] = new ImageCacheItem { FilePath = filePath, Info = imageInfo };
            SaveMetadata(imageInfo.Url, filePath, imageInfo.Description, imageInfo.Location, imageInfo.ShootTime, imageInfo.ShootAddr, imageInfo.AuthorUrl, imageInfo.AuthorName);
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

                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await fs.WriteAsync(bytes, 0, bytes.Length);
                }

                return true;
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"IOException caught: {ioEx.Message}");
                // Handle IO exception, e.g., by retrying
                if (retry == maxRetry - 1)
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught: {ex.Message}");
                // Handle other exceptions, e.g., by retrying
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

    public void SaveVersionInfo(string version, int userId)
    {
        var versionInfo = new
        {
            version = version,
            userId = userId
        };

        string jsonString = JsonSerializer.Serialize(versionInfo);
        string versionFilePath = Path.Combine(cacheRootDirectory, "info.json");
        Console.WriteLine(jsonString);
        File.WriteAllText(versionFilePath, jsonString);
    }

    private void InitializeCacheDirectory()
    {
        if (!Directory.Exists(cacheRootDirectory))
        {
            Directory.CreateDirectory(cacheRootDirectory);
        }
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


            string createPositionTableQuery = @"CREATE TABLE IF NOT EXISTS CarouselPosition (
                                                Id INTEGER PRIMARY KEY,
                                                CurrentIndex INTEGER)";
            using (var command = new SQLiteCommand(createPositionTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }
            connection.Close();
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
            connection.Close();
        }
    }

    public void SaveCurrentPosition(int currentIndex)
    {
        try { 
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
                connection.Close();
            }
        }
        catch
        {

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
            connection.Close();
        }
    }
    // 删除数据文件
    public void DeleteDatabaseFile()
    {
        if (File.Exists(databaseFilePath))
        {
            File.Delete(databaseFilePath);
            Console.WriteLine("数据库文件已删除。");
        }
    }

    // 删除缓存目录
    public void DeleteCacheDirectory()
    {
        if (Directory.Exists(cacheDirectory))
        {
            Directory.Delete(cacheDirectory, true);
            Console.WriteLine("缓存目录已删除。");
        }
    }

    // 删除所有图片
    public void DeleteAllCachedImages()
    {
        foreach (var cacheItem in ImageCache.Values)
        {
            if (File.Exists(cacheItem.FilePath))
            {
                File.Delete(cacheItem.FilePath);
                Console.WriteLine($"已删除图片：{cacheItem.FilePath}");
            }
        }
        ImageCache.Clear();
        ImageInfos.Clear();
        Console.WriteLine("所有缓存图片已删除。");
    }

    private bool ImageInfoEquals(ImageInfo info1, ImageInfo info2)
    {
        return info1.Description == info2.Description &&
               info1.Location == info2.Location &&
               info1.ShootTime == info2.ShootTime &&
               info1.ShootAddr == info2.ShootAddr &&
               info1.AuthorUrl == info2.AuthorUrl &&
               info1.AuthorName == info2.AuthorName;
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
