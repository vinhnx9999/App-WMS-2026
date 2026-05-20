namespace WMS.Domain.Common;

public class NotFoundException(string name, object key) : Exception($"Entity \"{name}\" ({key}) was not found.")
{
}
