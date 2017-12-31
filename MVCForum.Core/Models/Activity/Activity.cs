﻿namespace MvcForum.Core.Models.Activity
{
    using System;
    using Entities;
    using Utilities;

    public class Activity : Entity
    {
        public Activity()
        {
            Id = GuidComb.GenerateComb();
        }
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
