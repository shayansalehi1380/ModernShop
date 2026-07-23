using System;

namespace ModernShop.Core.DTOs
{
    public class StaticPageDto
    {
        public string Slug { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    public class AdminStaticPageListItemDto
    {
        public int Id { get; set; }
        public string Slug { get; set; } = null!;
        public string Title { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateStaticPageRequestDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
    }
}
