using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace AppGateway.Api.Templates;

public static class HomeTemplateProvider
{
    private static readonly Lazy<string> DefaultTemplate = new(LoadEmbeddedTemplate, isThreadSafe: true);

    public static string Load(IFileProvider? fileProvider, string fileName)
    {
        if (fileProvider is not null)
        {
            var fileInfo = fileProvider.GetFileInfo(fileName);
            if (fileInfo.Exists)
            {
                using var stream = fileInfo.CreateReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
                return reader.ReadToEnd();
            }
        }

        return DefaultTemplate.Value;
    }

    private static string LoadEmbeddedTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = typeof(HomeTemplateProvider).Namespace + ".DefaultHomeTemplate.html";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        return reader.ReadToEnd();
    }
}
