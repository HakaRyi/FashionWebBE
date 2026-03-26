using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Repositories.Dto
{
    public class ItemDto
    {
    }

    public class SmartRecommendationDto
    {
        public string Prompt { get; set; } = string.Empty;

        public bool UseMyWardrobe { get; set; } = true;

        public bool UseSavedItems { get; set; } = true;

        public bool UseCommunityItems { get; set; } = false;

        public int Limit { get; set; } = 10;

        public string? ReferenceCategory { get; set; }
    }

    public class SearchIntent
    {
        // --- PHÂN LOẠI CƠ BẢN ---
        [JsonPropertyName("item")]
        public string Item { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("sub_category")]
        public string SubCategory { get; set; } = string.Empty;

        [JsonPropertyName("gender")]
        public string Gender { get; set; } = string.Empty;

        // --- THUỘC TÍNH CHI TIẾT CỦA MÓN ĐỒ (EXACT MATCH) ---
        [JsonPropertyName("main_color")]
        public string MainColor { get; set; } = string.Empty;

        [JsonPropertyName("sub_color")]
        public string SubColor { get; set; } = string.Empty;

        [JsonPropertyName("material")]
        public string Material { get; set; } = string.Empty;

        [JsonPropertyName("style")]
        public string Style { get; set; } = string.Empty;

        [JsonPropertyName("pattern")]
        public string Pattern { get; set; } = string.Empty;

        [JsonPropertyName("fit")]
        public string Fit { get; set; } = string.Empty;

        [JsonPropertyName("neckline")]
        public string Neckline { get; set; } = string.Empty;

        [JsonPropertyName("sleeve_length")]
        public string SleeveLength { get; set; } = string.Empty;

        [JsonPropertyName("length")]
        public string Length { get; set; } = string.Empty;

        [JsonPropertyName("item_type")]
        public string ItemType { get; set; } = string.Empty;

        // --- PHỤC VỤ VECTOR SEARCH (SEMANTIC) ---
        // Đây là câu prompt tiếng Anh chuẩn hóa để băm ra Vector (SigLIP)
        [JsonPropertyName("clean_prompt")]
        public string CleanPrompt { get; set; } = string.Empty;

        // --- RULE LỌC ĐẶC BIỆT ---
        [JsonPropertyName("must_have")]
        public List<string> MustHave { get; set; } = new List<string>();

        [JsonPropertyName("must_exclude")]
        public List<string> MustExclude { get; set; } = new List<string>();
    }
}
