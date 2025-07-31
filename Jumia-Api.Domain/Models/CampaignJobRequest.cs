using Jumia_Api.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Models
{
    public class CampaignJobRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public JobType JobType { get; set; }

        [Required]
        [ForeignKey("Seller")]
        public int SellerId { get; set; }

        // Store any specific parameters for the job as JSON
        [Column(TypeName = "nvarchar(MAX)")]
        public string? PayloadJson { get; set; }

        [Required]
        public JobStatus Status { get; set; } = JobStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedProcessingAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string? ErrorMessage { get; set; }

        // Navigation property
        public Seller Seller { get; set; }
    }
}
