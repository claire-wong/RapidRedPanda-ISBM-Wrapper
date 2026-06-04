namespace RapidRedPanda.Wrapper.Publication;

public sealed class WrapperFilterExpression
{
    public List<string> ApplicableMediaTypes { get; init; } = [];

    public string? Expression { get; init; }

    public string? Language { get; init; }

    public string? LanguageVersion { get; init; }

    public List<WrapperFilterNamespace> Namespaces { get; init; } = [];

    internal bool IsEmpty =>
        ApplicableMediaTypes.TrueForAll(string.IsNullOrWhiteSpace)
        && string.IsNullOrWhiteSpace(Expression)
        && string.IsNullOrWhiteSpace(Language)
        && string.IsNullOrWhiteSpace(LanguageVersion)
        && Namespaces.Count == 0;
}

public sealed class WrapperFilterNamespace
{
    public string? Prefix { get; init; }

    public string Name { get; init; } = "";
}
