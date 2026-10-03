namespace Invoice.AI.Data;

/// <summary>
/// ONE place that maps the AI queries to your real table / column names.
/// Change these constants if your database uses different names.
/// Assumes: Category.Id is an INT, and every table has a bit column called IsActive.
/// </summary>
public static class MasterTableMap
{
    public const string CategoryTable = "Category";
    public const string CategoryId = "Id";
    public const string CategoryName = "Name";
    public const string CategoryIsActive = "IsActive";

    public const string ItemTable = "ItemMaster";
    public const string ItemCategoryId = "CategoryId";   // FK -> Category.Id
    public const string ItemIsActive = "IsActive";

    public const string CustomerTable = "Customer";
    public const string CustomerCity = "City";
    public const string CustomerIsActive = "IsActive";

    public const string VendorTable = "Vendor";
    public const string VendorIsActive = "IsActive";

    public const string UserTable = "Users";
    public const string UserIsActive = "IsActive";
}
