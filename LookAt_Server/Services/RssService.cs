using LookAt_Server.Models;
using MongoDB.Driver;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using System.Net;
using FuzzySharp;

namespace LookAt_Server.Services
{
    public class RssService
    {
        private readonly IMongoCollection<Category> _categories;
        private readonly IMongoCollection<Article> _articles;
        private const int SIMILARITY_THRESHOLD = 70; // Ngưỡng giống nhau 70%

        public RssService(IMongoDatabase db)
        {
            _categories = db.GetCollection<Category>("Categories");
            _articles = db.GetCollection<Article>("Articles");
        }

        // Crawl toàn bộ RSS từ các Category
        public async Task<FetchResult> FetchAllRssAsync()
        {
            var categories = await _categories.Find(_ => true).ToListAsync();
            int totalAdded = 0;
            int totalSkipped = 0;
            var details = new List<CategoryFetchDetail>();

            foreach (var cat in categories)
            {
                var result = await FetchRssFromCategory(cat);
                totalAdded += result.Added;
                totalSkipped += result.Skipped;
                details.Add(new CategoryFetchDetail
                {
                    CategoryName = cat.Name,
                    SourcesCount = cat.RssSources?.Count ?? 0,
                    ArticlesAdded = result.Added,
                    ArticlesSkipped = result.Skipped,
                    SourceDetails = result.SourceDetails
                });
            }

            return new FetchResult
            {
                TotalAdded = totalAdded,
                TotalSkipped = totalSkipped,
                Details = details
            };
        }

        // Crawl từ tất cả RSS sources của một Category
        private async Task<CategoryFetchResult> FetchRssFromCategory(Category category)
        {
            int totalCount = 0;
            int totalSkipped = 0;
            var sourceDetails = new List<SourceFetchDetail>();

            // Kiểm tra null hoặc empty
            if (category.RssSources == null || category.RssSources.Count == 0)
            {
                return new CategoryFetchResult
                {
                    Added = 0,
                    Skipped = 0,
                    SourceDetails = new List<SourceFetchDetail>
                    {
                        new SourceFetchDetail
                        {
                            SourceName = "N/A",
                            SourceUrl = "N/A",
                            ArticlesAdded = 0,
                            ArticlesSkipped = 0,
                            Success = false,
                            Error = "No RSS sources configured"
                        }
                    }
                };
            }

            // Lặp qua tất cả các RSS sources của category
            foreach (var rssSource in category.RssSources)
            {
                int sourceCount = 0;
                int sourceSkipped = 0;
                string error = null;
                bool success = true;

                try
                {
                    using var reader = XmlReader.Create(rssSource.Url);
                    var feed = SyndicationFeed.Load(reader);

                    foreach (var item in feed.Items)
                    {
                        string link = item.Links.FirstOrDefault()?.Uri.ToString() ?? "";
                        string title = item.Title?.Text ?? "";
                        string descriptionHtml = item.Summary?.Text ?? "";

                        // Kiểm tra trùng lặp theo link
                        var existsByLink = await _articles
                            .Find(a => a.Link == link)
                            .FirstOrDefaultAsync();

                        if (existsByLink != null)
                        {
                            sourceSkipped++;
                            totalSkipped++;
                            continue;
                        }

                        // Clean title: Remove CDATA và decode HTML entities
                        title = CleanText(title);

                        // Extract thumbnail từ HTML description
                        string? thumbnail = ExtractThumbnailFromHtml(descriptionHtml);

                        // Strip HTML tags để lấy text sạch và decode HTML entities
                        string cleanDescription = CleanText(StripHtml(descriptionHtml));

                        // ⭐ Kiểm tra trùng lặp theo nội dung (title + description)
                        var isDuplicate = await IsDuplicateArticle(title, cleanDescription, category.Name);
                        if (isDuplicate)
                        {
                            sourceSkipped++;
                            totalSkipped++;
                            continue;
                        }

                        var article = new Article
                        {
                            Title = title,
                            Description = cleanDescription,
                            Thumbnail = thumbnail,
                            Link = link,
                            Category = category.Name,
                            Source = rssSource.Name, // Sử dụng tên nguồn từ RssSource
                            PubDate = item.PublishDate.UtcDateTime,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _articles.InsertOneAsync(article);
                        sourceCount++;
                        totalCount++;
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    error = ex.Message;
                }

                sourceDetails.Add(new SourceFetchDetail
                {
                    SourceName = rssSource.Name,
                    SourceUrl = rssSource.Url,
                    ArticlesAdded = sourceCount,
                    ArticlesSkipped = sourceSkipped,
                    Success = success,
                    Error = error
                });
            }

            return new CategoryFetchResult
            {
                Added = totalCount,
                Skipped = totalSkipped,
                SourceDetails = sourceDetails
            };
        }

        // ⭐ Kiểm tra xem bài báo có trùng với bài nào đã lưu không (>= 70% giống nhau)
        private async Task<bool> IsDuplicateArticle(string title, string description, string category)
        {
            // Lấy các bài báo gần đây trong cùng category (24h gần nhất)
            var recentArticles = await _articles
                .Find(a => a.Category == category && a.CreatedAt >= DateTime.UtcNow.AddHours(-24))
                .Limit(100) // Giới hạn số lượng để tăng performance
                .ToListAsync();

            foreach (var article in recentArticles)
            {
                // So sánh title
                int titleSimilarity = Fuzz.Ratio(title, article.Title);

                // So sánh description (nếu có)
                int descSimilarity = 0;
                if (!string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(article.Description))
                {
                    descSimilarity = Fuzz.PartialRatio(description, article.Description);
                }

                // Nếu title giống >= 80% hoặc (title >= 60% và description >= 70%)
                if (titleSimilarity >= 80 || (titleSimilarity >= 60 && descSimilarity >= SIMILARITY_THRESHOLD))
                {
                    return true; // Là bài trùng
                }
            }

            return false;
        }

        // Clean text: Remove CDATA wrapper và decode HTML entities
        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove CDATA wrapper
            text = Regex.Replace(text, @"<!\[CDATA\[(.*?)\]\]>", "$1", RegexOptions.Singleline);

            // Decode HTML entities (&agrave; -> à, &aacute; -> á, etc.)
            text = WebUtility.HtmlDecode(text);

            return text.Trim();
        }

        // Extract URL ảnh đầu tiên từ HTML description
        private string? ExtractThumbnailFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            // Regex để tìm src trong thẻ img
            // Hỗ trợ cả dấu nháy đơn và nháy kép
            var match = Regex.Match(html, @"<img[^>]+src=[""']([^""']+)[""']", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        private string StripHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return Regex.Replace(input, "<.*?>", "").Trim();
        }
    }

    // Helper classes for detailed response
    public class FetchResult
    {
        public int TotalAdded { get; set; }
        public int TotalSkipped { get; set; }
        public List<CategoryFetchDetail> Details { get; set; }
    }

    public class CategoryFetchDetail
    {
        public string CategoryName { get; set; }
        public int SourcesCount { get; set; }
        public int ArticlesAdded { get; set; }
        public int ArticlesSkipped { get; set; }
        public List<SourceFetchDetail> SourceDetails { get; set; }
    }

    public class CategoryFetchResult
    {
        public int Added { get; set; }
        public int Skipped { get; set; }
        public List<SourceFetchDetail> SourceDetails { get; set; }
    }

    public class SourceFetchDetail
    {
        public string SourceName { get; set; }
        public string SourceUrl { get; set; }
        public int ArticlesAdded { get; set; }
        public int ArticlesSkipped { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}