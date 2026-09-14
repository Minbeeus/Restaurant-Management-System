namespace Restaurant.Application.DTOs;

public class AreaDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int TableCount { get; set; }
}

public class CreateAreaRequest
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class UpdateAreaRequest : CreateAreaRequest
{
    public bool IsActive { get; set; } = true;
}

public class TableDto
{
    public int Id { get; set; }
    public int AreaId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int Status { get; set; }
    public string QrToken { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTableRequest
{
    public int AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; } = 4;
    public int SortOrder { get; set; }
}

public class UpdateTableRequest : CreateTableRequest
{
    public bool IsActive { get; set; } = true;
    public int Status { get; set; }
}
