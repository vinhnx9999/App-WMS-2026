namespace WMS.Application.Common.DynamicSearch;

/// <summary>
/// DynamicSearch Operators
/// </summary>
public enum Operators
{
    /// <summary>
    /// None
    /// </summary>
    None = 0,
    /// <summary>
    /// Equal =
    /// </summary>
    Equal = 1,
    /// <summary>
    /// GreaterThan 
    /// </summary>
    GreaterThan = 2,
    /// <summary>
    /// GreaterThanOrEqual 
    /// </summary>
    GreaterThanOrEqual = 3,
    /// <summary>
    /// LessThan
    /// </summary>
    LessThan = 4,
    /// <summary>
    /// LessThanOrEqual
    /// </summary>
    LessThanOrEqual = 5,
    /// <summary>
    /// Contains
    /// </summary>
    Contains = 6
}

/// <summary>
/// Condition
/// </summary>
public enum Condition
{
    /// <summary>
    /// OR
    /// </summary>
    OrElse = 1,
    /// <summary>
    /// AND
    /// </summary>
    AndAlso = 2
}

/// <summary>
/// select item combox
/// </summary>
public class ComboxItem
{
    /// <summary>
    /// value
    /// </summary>
    public string? Value { get; set; }
    /// <summary>
    /// text
    /// </summary>
    public string? Text { get; set; }
}

public class SearchObject
{
    /// <summary>
    /// sort
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// label
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// operator
    /// </summary>
    public Operators Operator { get; set; } = Operators.Equal;

    /// <summary>
    /// text
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// value
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// select item combox list
    /// </summary>
    public List<ComboxItem> ComboxItems { get; set; } = new List<ComboxItem>();
}
