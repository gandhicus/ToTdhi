namespace TitanCore.Core
{
    public struct DamageResult
    {
        public int damage;
        public HitResultType type;

        public DamageResult(int damage, HitResultType type)
        {
            this.damage = damage;
            this.type = type;
        }
    }
}
