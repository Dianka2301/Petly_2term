using System;
using System.ComponentModel.DataAnnotations;

namespace Petly.Models
{
    public class SearchHistory
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public ApplicationUser User { get; set; }

        [MaxLength(500)]
        public string SearchQuery { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
