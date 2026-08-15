namespace TitanCore.Core
{
    public struct DamageResult
    {
        public int damage;
        public HitResultType type;
        public bool wasCritical;

        public DamageResult(int damage, HitResultType type, bool wasCritical = false)
        {
            this.damage = damage;
            this.type = type;
            this.wasCritical = wasCritical;
        }
    }
}
