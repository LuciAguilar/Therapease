namespace TherapEase.UnitTests.Dobles;

internal sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;
}
