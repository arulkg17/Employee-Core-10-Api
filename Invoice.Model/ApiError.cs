using System;
using System.Collections.Generic;
using System.Text;

namespace Invoice.Model;

public class ApiError
{
    public required string Code { get; set; }
    public required string Details { get; set; }
}
