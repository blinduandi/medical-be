using System;
using System.Collections.Generic;

namespace medical_be.Models
{
    public class SingleNotification
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Data { get; set; } // JSON

        public string Status { get; set; } = "waiting_for_sending";
        // waiting_for_campaign_start, waiting_for_sending, failed, sent, opened

        // Campaign reference
        public long? CampaignId { get; set; }
        public NotificationCampaign? Campaign { get; set; }

        // Email specific
        public string? ToEmail { get; set; }

        public string Type { get; set; } = "notification";
        public long? MainCompanyId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ScheduledAt { get; set; }
    }
}