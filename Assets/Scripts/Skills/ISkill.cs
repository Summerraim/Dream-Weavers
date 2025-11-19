#if false
// Disabled legacy interface to avoid conflicts with new interfaces.
using DW.Runtime;
namespace DW.Skills
{
    public interface ISkill
    {
        string DisplayName { get; }
        int ManaCost { get; }
        int CooldownTurns { get; }
        void Execute(Sprite caster, Sprite target);
    }
}
#endif
