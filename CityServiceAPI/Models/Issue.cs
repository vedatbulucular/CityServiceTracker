using System;
using System.Collections.Generic;

namespace CityServiceAPI.Models;

public partial class Issue
{
    public int Id { get; set; }

    public int CitizenId { get; set; }

    public int CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string LocationData { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? Status { get; set; }

    public DateTime? ReportedAt { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public virtual Category Category { get; set; } = null!;

    public virtual User Citizen { get; set; } = null!;
}
