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
using System.Net;
using System.Windows.Media.Imaging;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Concurrent;

public class ImageCacheManager
{
    private HttpClient httpClient;
    private static string tempPath;
    private static string cacheDirectory;
    private static string databaseFilePath;
    private static string cacheRootDirectory;
    private static readonly Dictionary<string, BitmapImage> memoryCache = new Dictionary<string, BitmapImage>();
    private static readonly ConcurrentQueue<ImageCacheItem> insertQueue = new ConcurrentQueue<ImageCacheItem>();
    private static readonly ConcurrentQueue<string> deleteQueue = new ConcurrentQueue<string>();
    private static readonly object lockObject = new object();

    public Dictionary<string, ImageCacheItem> ImageCache { get; private set; }
    public List<ImageInfo> ImageInfos { get; set; }
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
            databaseFilePath = Path.Combine(cacheDirectory, "cache_v2.db");
            InitializeCacheDirectory();
            InitializeDatabase();
            LoadMetadata();
            LoadCurrentPosition();
        }
        catch (Exception ex)
        {
            Logger.LogError("Error during ImageCacheManager initialization", ex);
            //DeleteCacheDirectory();
        }
    }

    public async Task LoadImagesAsync(bool allPage = true, int pageSizeNew = GlobalData.PageSize)
    {
        var funcMessage = "全量同步数据";
        Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
        if (!IsNetworkAvailable())
        {
            Console.WriteLine("No network connection available. Skipping API calls and cache updates.");
            return;
        }

        int pageIndex = GlobalData.PageIndex;
        int pageSize = pageSizeNew;
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
        int retryCount = 0;
        int maxRetries = 3;
        int delayMilliseconds = 2000;

        while (morePages)
        {
            try
            {
                Console.WriteLine($"{funcMessage}开始 请求第{pageIndex} 页：{DateTime.Now}");
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
                        //强制将webp改成jpg 开始
                        newImageInfo.Url = newImageInfo.Url.Replace("@!webp", "");
                        //强制将webp改成jpg 结束
                        newImageUrls.Add(newImageInfo.Url);

                        if (ImageCache.TryGetValue(newImageInfo.Url, out var cachedItem))
                        {
                            if (!ImageInfoEquals(cachedItem.Info, newImageInfo))
                            {
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
                    Console.WriteLine($"{funcMessage}结束 请求第{pageIndex} 页：{DateTime.Now}");
                    if (allPage)
                    {
                        pageIndex++;
                    }
                    else
                    {
                        morePages = false;
                    }
                    body["pageIndex"] = pageIndex;

                    await Task.Delay(delayMilliseconds);
                }
            }
            catch (HttpRequestException ex)
            {
                retryCount++;
                Console.WriteLine($"Server error encountered: {ex.Message}. Retry {retryCount}/{maxRetries}.");
                if (retryCount >= maxRetries)
                {
                    Console.WriteLine("Max retries reached. Exiting load process.");
                    morePages = false;
                }
                else
                {
                    await Task.Delay(delayMilliseconds);
                }
            }
        }

        ImageInfos = ImageInfos.OrderByDescending(i => i.CreateTime).ToList();
        await SyncCache(newImageUrls);
        Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
    }

    private List<ImageInfo> ParseImageInfo(string jsonResponse)
    {
        var funcMessage = "解析接口信息";
        Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
        var jsonDocument = JsonDocument.Parse(jsonResponse);
        var listElement = jsonDocument.RootElement.GetProperty("body").GetProperty("list");
        var imageInfos = new List<ImageInfo>();
        Console.WriteLine($"{funcMessage} 返回数量：{listElement.EnumerateArray().Count()}");
        try
        {
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
                    AuthorName = element.GetProperty("authorName").GetString(),
                    CreateTime = element.GetProperty("createtime").ToString(),
                    GroupId = element.GetProperty("groupId").GetInt32(),
                    AlbumId = element.GetProperty("albumId").GetInt32(),
                    AuthorId = element.GetProperty("authorId").GetInt32(),
                    AlbumName = element.GetProperty("albumName").GetString() // 解析 albumName 字段
                };
                imageInfos.Add(imageInfo);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"{funcMessage}", ex);
        }
        Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
        return imageInfos;
    }

    private async Task CacheImageAsync(ImageInfo imageInfo, bool updateExisting)
    {
        var funcMessage = "下载图片，保存元数据";
        Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
        //强制将webp改成jpg 开始
        imageInfo.Url = imageInfo.Url.Replace("@!webp", "");
        //强制将webp改成jpg 结束
        Uri uri = new Uri(imageInfo.Url);

        var filePath = Path.Combine(cacheDirectory, Path.GetFileNameWithoutExtension(uri.LocalPath) + Path.GetExtension(uri.LocalPath));

        if (updateExisting && File.Exists(filePath))
        {
            //暂不删除文件
            //File.Delete(filePath);
        }
        if (File.Exists(filePath))
        {
            Console.WriteLine($"{funcMessage} 缓存存在，不下载:{imageInfo.Url}");
            ImageCache[imageInfo.Url] = new ImageCacheItem { FilePath = filePath, Info = imageInfo };
            //SaveMetadata(imageInfo.Url, filePath, imageInfo.Description, imageInfo.Location, imageInfo.ShootTime, imageInfo.ShootAddr, imageInfo.AuthorUrl, imageInfo.AuthorName, imageInfo.CreateTime, imageInfo.GroupId, imageInfo.AlbumId, imageInfo.AuthorId);
            await SaveMetadataAsync(filePath, imageInfo);
        }
        else
        {
            bool downloadSuccess = await DownloadImageAsync(imageInfo.Url, filePath);

            if (downloadSuccess)
            {
                Console.WriteLine($"{funcMessage} 下载:{imageInfo.Url}");
                ImageCache[imageInfo.Url] = new ImageCacheItem { FilePath = filePath, Info = imageInfo };
                //SaveMetadata(imageInfo.Url, filePath, imageInfo.Description, imageInfo.Location, imageInfo.ShootTime, imageInfo.ShootAddr, imageInfo.AuthorUrl, imageInfo.AuthorName, imageInfo.CreateTime, imageInfo.GroupId, imageInfo.AlbumId, imageInfo.AuthorId);
                await SaveMetadataAsync(filePath, imageInfo);
            }
        }

        Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
    }

    private async Task<bool> DownloadImageAsync(string url, string filePath)
    {
        var funcMessage = "图片下载任务";
        Console.WriteLine($"{funcMessage}开始：{url} , {filePath}, {DateTime.Now}");
        int maxRetry = 3;
        for (int retry = 0; retry < maxRetry; retry++)
        {
            if (retry == 0)
                url = url.Replace("@!webp", "");
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
        Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
        return false;

    }

    private async Task SyncCache(HashSet<string> newImageUrls)
    {
        var funcMessage = "数据清理";
        Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
        var oldImageUrls = ImageCache.Keys.ToList();
        foreach (var url in oldImageUrls)
        {
            if (!newImageUrls.Contains(url))
            {
                try
                {
                    var filePath = ImageCache[url].FilePath;
                    if (File.Exists(filePath))
                    {
                        //File.Delete(filePath);
                        Console.WriteLine($"{funcMessage} 暂时不删除缓存图片文件 {filePath} {DateTime.Now}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"{funcMessage} 出现异常 .URL:{url}", ex);
                }
                ImageCache.Remove(url);
                ImageInfos.RemoveAll(info => info.Url == url);
                Console.WriteLine($"{funcMessage} 移除缓存 {url} {DateTime.Now}");
                //DeleteMetadata(url);
                await DeleteMetadataAsync(url);
            }
        }
        Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
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
        var funcMessage = "建立与检查缓存目录";
        Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
        if (!Directory.Exists(cacheRootDirectory))
        {
            Directory.CreateDirectory(cacheRootDirectory);
        }
        if (!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }
        Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
        //var funcMessage = "";
        //Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
        //Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
    }

    private void InitializeDatabase()
    {
        var funcMessage = "数据库初始化";
        //Console.WriteLine($"结束：{DateTime.Now}");
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
                                        AuthorName TEXT,
                                        CreateTime TEXT,
                                        GroupId INTEGER,
                                        AlbumId INTEGER,
                                        AuthorId INTEGER,
                                        AlbumName TEXT)"; // 添加 AlbumName 字段
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

        UpdateDatabaseSchema();
        Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
    }

    private void UpdateDatabaseSchema()
    {
        var funcMessage = "数据字段检查";
        Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");

        using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;"))
        {
            connection.Open();
            string checkColumnQuery = "PRAGMA table_info(ImageCache)";
            var columns = new List<string>();
            using (var command = new SQLiteCommand(checkColumnQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columns.Add(reader["name"].ToString());
                    }
                }
            }

            var requiredColumns = new List<string> { "CreateTime", "GroupId", "AlbumId", "AuthorId", "AlbumName" };
            foreach (var column in requiredColumns)
            {
                if (!columns.Contains(column))
                {
                    string addColumnQuery = $"ALTER TABLE ImageCache ADD COLUMN {column} {(column == "AlbumName" ? "TEXT" : "INTEGER")}";
                    using (var command = new SQLiteCommand(addColumnQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }

            connection.Close();
        }
        Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
    }

    private void LoadMetadata()
    {
        var funcMessage = "加载缓存数据";
        Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
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
                        var createTime = reader["CreateTime"].ToString();
                        var groupId = Convert.ToInt32(reader["GroupId"]);
                        var albumId = Convert.ToInt32(reader["AlbumId"]);
                        var authorId = Convert.ToInt32(reader["AuthorId"]);
                        var albumName = reader["AlbumName"].ToString(); // 加载 albumName 字段

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
                                AuthorName = authorName,
                                CreateTime = createTime,
                                GroupId = groupId,
                                AlbumId = albumId,
                                AuthorId = authorId,
                                AlbumName = albumName // 加载 albumName 字段
                            };
                            ImageCache[url] = new ImageCacheItem { FilePath = filePath, Info = imageInfo };
                            ImageInfos.Add(imageInfo);
                        }
                    }
                }
            }
        }

        ImageInfos = ImageInfos.OrderByDescending(i => i.CreateTime).ToList();
        Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
    }
    public static Task SaveMetadataAsync(string filePath, ImageInfo info)
    {
        insertQueue.Enqueue(new ImageCacheItem
        {
            FilePath = filePath,
            Info = info
        });

        return Task.Run(() => ProcessInsertQueue());
    }

    public static Task DeleteMetadataAsync(string url)
    {
        deleteQueue.Enqueue(url);
        return Task.Run(() => ProcessDeleteQueue());
    }

    private static void ProcessInsertQueue()
    {
        lock (lockObject)
        {
            using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;"))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    while (insertQueue.TryDequeue(out var item))
                    {
                        string insertQuery = @"REPLACE INTO ImageCache (Url, FilePath, Description, Location, ShootTime, ShootAddr, AuthorUrl, AuthorName, CreateTime, GroupId, AlbumId, AuthorId, AlbumName)
                                               VALUES (@url, @filePath, @description, @location, @shootTime, @shootAddr, @authorUrl, @authorName, @createTime, @groupId, @albumId, @authorId, @albumName)";
                        using (var command = new SQLiteCommand(insertQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@url", item.Info.Url);
                            command.Parameters.AddWithValue("@filePath", item.FilePath);
                            command.Parameters.AddWithValue("@description", item.Info.Description);
                            command.Parameters.AddWithValue("@location", item.Info.Location);
                            command.Parameters.AddWithValue("@shootTime", item.Info.ShootTime);
                            command.Parameters.AddWithValue("@shootAddr", item.Info.ShootAddr);
                            command.Parameters.AddWithValue("@authorUrl", item.Info.AuthorUrl);
                            command.Parameters.AddWithValue("@authorName", item.Info.AuthorName);
                            command.Parameters.AddWithValue("@createTime", item.Info.CreateTime);
                            command.Parameters.AddWithValue("@groupId", item.Info.GroupId);
                            command.Parameters.AddWithValue("@albumId", item.Info.AlbumId);
                            command.Parameters.AddWithValue("@authorId", item.Info.AuthorId);
                            command.Parameters.AddWithValue("@albumName", item.Info.AlbumName); // 保存 albumName 字段
                            command.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }
    }

    private static void ProcessDeleteQueue()
    {
        lock (lockObject)
        {
            using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;"))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    while (deleteQueue.TryDequeue(out var url))
                    {
                        string deleteQuery = "DELETE FROM ImageCache WHERE Url = @url";
                        using (var command = new SQLiteCommand(deleteQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@url", url);
                            command.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }
    }
    private void SaveMetadata(string url, string filePath, string description, string location, string shootTime, string shootAddr, string authorUrl, string authorName, string createTime, int groupId, int albumId, int authorId)
    {
        using (var connection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;"))
        {
            connection.Open();
            string insertQuery = @"REPLACE INTO ImageCache (Url, FilePath, Description, Location, ShootTime, ShootAddr, AuthorUrl, AuthorName, CreateTime, GroupId, AlbumId, AuthorId, AlbumName)
                                   VALUES (@url, @filePath, @description, @location, @shootTime, @shootAddr, @authorUrl, @authorName, @createTime, @groupId, @albumId, @authorId, @albumName)";
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
                command.Parameters.AddWithValue("@createTime", createTime);
                command.Parameters.AddWithValue("@groupId", groupId);
                command.Parameters.AddWithValue("@albumId", albumId);
                command.Parameters.AddWithValue("@authorId", authorId);
                command.Parameters.AddWithValue("@albumName", albumId);
                command.ExecuteNonQuery();
            }
        }
    }

    private void DeleteMetadata(string url)
    {
        var funcMessage = "删除元数据";
        Console.WriteLine($"{funcMessage}开始：{url} {DateTime.Now}");
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
        Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
    }

    public void SaveCurrentPosition(int currentIndex)
    {
        try
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
                connection.Close();
            }
        }
        catch
        {
            // Handle exception if necessary
        }
    }

    public void LoadCurrentPosition()
    {
        var funcMessage = "加载当前位置";
        Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
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
        Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
    }

    public void DeleteDatabaseFile()
    {
        if (File.Exists(databaseFilePath))
        {
            File.Delete(databaseFilePath);
            Console.WriteLine("数据库文件已删除。");
        }
    }

    public void DeleteCacheDirectory()
    {
        try
        {
            if (Directory.Exists(cacheDirectory))
            {
                Directory.Delete(cacheDirectory, true);
                Console.WriteLine("缓存目录已删除。");
            }
        }
        catch
        {
            return;
        }
    }

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
               info1.AuthorName == info2.AuthorName &&
               info1.CreateTime == info2.CreateTime &&
               info1.GroupId == info2.GroupId &&
               info1.AlbumId == info2.AlbumId &&
               info1.AuthorId == info2.AuthorId &&
               info1.AlbumName == info2.AlbumName; // 比较 albumName 字段

    }

    private bool IsNetworkAvailable()
    {
        try
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", InfoHelper.SoftwareInfo.NameEN);
                var response = client.GetAsync("https://cnapi.levect.com/social/hello").Result;
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine($"API service responded with status code: {response.StatusCode}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Network check failed: {ex.Message}");
            return false;
        }
    }


    public static BitmapImage GetImage(string url)
    {
        string fileName = GetSafeFileNameFromUrl(url);
        string filePath = Path.Combine(cacheDirectory, fileName);

        if (memoryCache.ContainsKey(filePath))
        {
            return memoryCache[filePath];
        }
        else if (File.Exists(filePath))
        {
            var image = new BitmapImage(new Uri(filePath));
            memoryCache[filePath] = image;
            return image;
        }
        else
        {
            try
            {
                DownloadImage(url, filePath);
                var image = new BitmapImage(new Uri(filePath));
                memoryCache[filePath] = image;
                return image;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading image: {ex.Message}");
                return null;
            }
        }
    }

    private static string GetSafeFileNameFromUrl(string url)
    {
        using (var md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            string extension = GetFileExtension(url);
            return sb.ToString() + extension;
        }
    }

    private static string GetFileExtension(string url)
    {
        try
        {
            Uri uri = new Uri(url);
            string path = uri.AbsolutePath;
            string extension = Path.GetExtension(path);

            // 如果 URL 包含查询参数，去除查询参数
            if (extension.Contains('?'))
            {
                extension = extension.Substring(0, extension.IndexOf('?'));
            }

            if (string.IsNullOrEmpty(extension))
            {
                return ".webp"; // 如果没有找到扩展名，默认使用 .webp
            }
            return extension;
        }
        catch
        {
            return ".webp"; // 如果 URL 无效，默认使用 .jpg
        }
    }

    private static void DownloadImage(string url, string filePath)
    {
        const int maxRetries = 3;
        int retries = 0;
        bool success = false;

        while (retries < maxRetries && !success)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] imageData = client.DownloadData(url);
                    File.WriteAllBytes(filePath, imageData);
                    success = true;
                }
            }
            catch (WebException ex)
            {
                retries++;
                Console.WriteLine($"Retry {retries}/{maxRetries} - Error downloading image: {ex.Message}");
                if (retries >= maxRetries)
                {
                    throw;
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
        public string CreateTime { get; set; }
        public int GroupId { get; set; }
        public int AlbumId { get; set; }
        public int AuthorId { get; set; }
        public string AlbumName { get; set; } // 新增字段
    }

    public class ImageCacheItem
    {
        public string FilePath { get; set; }
        public ImageInfo Info { get; set; }
    }
}
