using System;
using System.Collections.Generic;

namespace medical_be.Models
{
    public class NotificationCampaign
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string DeliveryStatus { get; set; } = "paused"; // paused, on_going, delivered
        public DateTime? StartDate { get; set; }
        public string Type { get; set; } = "email"; // only email for now

        // Template
        public string? NotificationTitle { get; set; }
        public string? NotificationBody { get; set; }
        public long? MainCompanyId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<SingleNotification>? Notifications { get; set; }
    }
}