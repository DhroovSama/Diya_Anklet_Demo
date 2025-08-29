public interface IDiyaLight
{
    bool IsLit { get; }
    bool IsCovered { get; }
    bool IsExtinguished { get; }
    float Intensity01 { get; } // 0..1 normalized
    float Heat01 { get; }      // 0..1 heat (snuffs at 1)
}
