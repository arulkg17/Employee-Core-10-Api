using System;
using System.Collections.Generic;
using System.Text;

namespace Invoice.Model.AI;

public class CategoryItemCountResult
{
    public string CategoryName { get; set; } = string.Empty;

    public bool CategoryIsActive { get; set; }

    public int ItemCount { get; set; }
}