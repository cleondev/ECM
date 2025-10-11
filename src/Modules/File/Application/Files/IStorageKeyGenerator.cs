namespace ECM.File.Application.Files;

public interface IStorageKeyGenerator
{
    string Generate(string fileName);
}
