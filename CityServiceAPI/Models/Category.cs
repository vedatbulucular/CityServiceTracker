using System;
using System.Collections.Generic;

namespace CityServiceAPI.Models;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string DepartmentName { get; set; } = null!;

    public int ExpectedResolutionHours { get; set; }

    public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
