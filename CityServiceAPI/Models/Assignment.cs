using System;
using System.Collections.Generic;

namespace CityServiceAPI.Models;

public partial class Assignment
{
    public int Id { get; set; }

    public int IssueId { get; set; }

    public int StaffId { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? StaffNotes { get; set; }

    public virtual Issue Issue { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;
}
