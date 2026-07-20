using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernShop.Core.DTOs
{
    public class BlogCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
    }

    public class BlogPostListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Excerpt { get; set; }
        public string? FeaturedImageUrl { get; set; }
        public string CategoryName { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public int ReadTimeMinutes { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    public class BlogCommentDto
    {
        public int Id { get; set; }
        public string UserFullName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class BlogPostDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? FeaturedImageUrl { get; set; }
        public string CategoryName { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public string? AuthorBio { get; set; }
        public int ReadTimeMinutes { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int ViewCount { get; set; }

        public List<BlogCommentDto> Comments { get; set; } = new();
    }

    public class CreateCommentRequestDto
    {
        public int BlogPostId { get; set; }
        public string Content { get; set; } = null!;
    }
}
