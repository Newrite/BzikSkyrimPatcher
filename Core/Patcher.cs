// ReSharper disable once CheckNamespace


namespace Bzikovich;

using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.FormKeys.SkyrimSE;

public class Patcher
{
    private static readonly FormLink<IKeywordGetter> SwordKey = Skyrim.Keyword.WeapTypeSword;
    private static readonly FormLink<IKeywordGetter> GreatSwordKey = Skyrim.Keyword.WeapTypeGreatsword;
    private static readonly FormLink<IKeywordGetter> DaggerKey = Skyrim.Keyword.WeapTypeDagger;

    private static readonly FormLink<IKeywordGetter> BattleStaffKey =
        new(FormKey.Factory("0E4581:Requiem for a Dream - Animated Armoury.esp"));

    private static readonly FormLink<IKeywordGetter> SpearKey =
        new(FormKey.Factory("0E457E:Requiem for a Dream - Animated Armoury.esp"));

    public enum ApiGameRelease
    {
        Skyrim,
        Enderal
    }

    public enum ApiPatcherMod
    {
        PatchGetAllArmor,
        PatchGetAllWeapon,
        PatchGetAllSpellStaffs,
        PatchBzikWeapon,
        PatchGetEnchantmentsAndSpellsWithReduceCosts
    }

    private readonly IEnumerable<IModListingGetter<ISkyrimModGetter>> _loadOrder;
    private readonly ILinkCache _cache;
    private readonly SkyrimMod _bethesdaMod;
    private readonly string _modName;


    private IEnumerable<IWeaponGetter> WeaponsFromLoadOrder()
    {
        return _loadOrder.Weapon().WinningOverrides()
            .Where(e => e != null)
            .Where(e => e.Template.IsNull);
    }

    private IEnumerable<IArmorGetter> ArmorsFromLoadOrder()
    {
        return _loadOrder.Armor().WinningOverrides()
            .Where(e => e != null)
            .Where(e => e.TemplateArmor.IsNull);
    }

    private IEnumerable<ISpellGetter> SpellsFromLoadOrder()
    {
        return _loadOrder.Spell().WinningOverrides()
            .Where(e => e != null);
    }

    private IEnumerable<IObjectEffectGetter> ObjectEffectsFromLoadOrder()
    {
        return _loadOrder.ObjectEffect().WinningOverrides()
            .Where(e => e != null);
    }

    private bool EffectContainActorValue(IEffectGetter effect, ActorValue av)
    {
        var magicEffect = effect.BaseEffect.TryResolve(_cache);

        if (magicEffect == null)
        {
            return false;
        }

        return magicEffect.Archetype.Type switch
        {
            MagicEffectArchetype.TypeEnum.ValueModifier => magicEffect.Archetype.ActorValue == av,
            MagicEffectArchetype.TypeEnum.PeakValueModifier => magicEffect.Archetype.ActorValue == av,
            MagicEffectArchetype.TypeEnum.DualValueModifier => magicEffect.Archetype.ActorValue == av ||
                                                               magicEffect.SecondActorValue == av,
            _ => false
        };
    }

    internal Patcher(ApiGameRelease gameRelease, ApiPatcherMod mod)
    {
        var skyrimRelease = gameRelease switch
        {
            ApiGameRelease.Skyrim => SkyrimRelease.SkyrimSE,
            ApiGameRelease.Enderal => SkyrimRelease.EnderalSE,
            _ => throw new ArgumentOutOfRangeException(nameof(gameRelease), gameRelease, null)
        };

        _modName = $"{gameRelease}{mod}{DateTime.Now.Ticks}.esp";
        _bethesdaMod = new SkyrimMod(ModKey.FromNameAndExtension(_modName.AsSpan()), skyrimRelease);
        var env = GameEnvironment.Typical.Skyrim(skyrimRelease);

        _loadOrder = env.LoadOrder.PriorityOrder.Where(e => e.Enabled);
        _cache = _loadOrder.ToImmutableLinkCache();
    }

    private string PatchGetAllArmor()
    {
        var armorCounter = 0;
        foreach (var armor in ArmorsFromLoadOrder())
        {
            _bethesdaMod.Armors.GetOrAddAsOverride(armor);
            Console.WriteLine($"Write armor {armor}");
            armorCounter += 1;
        }

        return $"Write to mod {armorCounter} armors";
    }

    private string PatchGetAllWeapon()
    {
        var weaponCounter = 0;
        foreach (var weapon in WeaponsFromLoadOrder())
        {
            _bethesdaMod.Weapons.GetOrAddAsOverride(weapon);
            Console.WriteLine($"Write weapon {weapon}");
            weaponCounter += 1;
        }

        return $"Write to mod {weaponCounter} weapons";
    }

    private string PatchGetAllSpellStaffs()
    {
        var weaponCounter = 0;
        foreach (var weapon in
                 WeaponsFromLoadOrder()
                     .Where(e => e.Data is {AnimationType: WeaponAnimationType.Staff}))
        {
            _bethesdaMod.Weapons.GetOrAddAsOverride(weapon);
            weaponCounter += 1;
            Console.WriteLine($"Write spell staff {weapon}");
        }

        return $"Write to mod {weaponCounter} spell staffs";
    }

    private void SaveMod()
    {
        _bethesdaMod.WriteToBinary(_modName);
    }

    private void DeleteModFile()
    {
        File.Delete(_modName);
    }

    private string PatchBzikWeapon()
    {
        var weaponCounter = 0;

        var weapons = WeaponsFromLoadOrder();

        var swords =
            weapons
                .Where(e => e.HasKeyword(SwordKey))
                .Where(e => e.Data != null);

        var greatSwords = weapons
            .Where(e => e.HasKeyword(GreatSwordKey))
            .Where(e => e.Data != null);

        var daggers =
            weapons
                .Where(e => e.HasKeyword(DaggerKey))
                .Where(e => e.Data is {Skill: { }});

        var spears =
            weapons
                .Where(e => e.HasKeyword(SpearKey))
                .Where(e => e.Data is {Skill: { }});

        var battleStaffs =
            weapons
                .Where(e => e.HasKeyword(BattleStaffKey))
                .Where(e => e.Data is {Skill: { }});

        Console.WriteLine($"BattleStaffs {battleStaffs}");

        foreach (var sword in swords)
        {
            var s = _bethesdaMod.Weapons.GetOrAddAsOverride(sword);
            Console.WriteLine($"Change speed sword {s.Data.Speed}");
            s.Data.Speed += 0.5f;
            weaponCounter += 1;
        }

        foreach (var greatSword in greatSwords)
        {
            var s = _bethesdaMod.Weapons.GetOrAddAsOverride(greatSword);
            Console.WriteLine($"Change speed great sword {s.Data.Speed}");
            s.Data.Speed += 0.7f;
            weaponCounter += 1;
        }

        foreach (var dagger in daggers)
        {
            var d = _bethesdaMod.Weapons.GetOrAddAsOverride(dagger);
            Console.WriteLine($"Change skill dagger {d.Data.Skill}");
            d.Data.Skill = Skill.Sneak;
            weaponCounter += 1;
        }

        foreach (var spear in spears)
        {
            var s = _bethesdaMod.Weapons.GetOrAddAsOverride(spear);
            Console.WriteLine($"Change skill spear {s.Data.Skill}");
            s.Data.Skill = Skill.Archery;
            weaponCounter += 1;
        }

        foreach (var battleStaff in battleStaffs)
        {
            var b = _bethesdaMod.Weapons.GetOrAddAsOverride(battleStaff);
            Console.WriteLine($"Change skill battleStaff {b.Data.Skill}");
            b.Data.Skill = Skill.Alteration;
            weaponCounter += 1;
        }

        return $"Write to mod {weaponCounter} weapons";
    }

    private int CheckEffectForReduceCost(IEffectGetter effect, ref bool result)
    {
        var counter = 0;
        if (EffectContainActorValue(effect, ActorValue.AlterationModifier))
        {
            _bethesdaMod.MagicEffects.GetOrAddAsOverride(effect.BaseEffect.TryResolve(_cache));
            result = true;
            Console.WriteLine("Write effect");
            counter += 1;
        }

        if (EffectContainActorValue(effect, ActorValue.DestructionModifier))
        {
            _bethesdaMod.MagicEffects.GetOrAddAsOverride(effect.BaseEffect.TryResolve(_cache));
            result = true;
            Console.WriteLine("Write effect");
            counter += 1;
        }

        if (EffectContainActorValue(effect, ActorValue.ConjurationModifier))
        {
            _bethesdaMod.MagicEffects.GetOrAddAsOverride(effect.BaseEffect.TryResolve(_cache));
            result = true;
            Console.WriteLine("Write effect");
            counter += 1;
        }

        if (EffectContainActorValue(effect, ActorValue.RestorationModifier))
        {
            _bethesdaMod.MagicEffects.GetOrAddAsOverride(effect.BaseEffect.TryResolve(_cache));
            result = true;
            Console.WriteLine("Write effect");
            counter += 1;
        }

        if (EffectContainActorValue(effect, ActorValue.IllusionModifier))
        {
            _bethesdaMod.MagicEffects.GetOrAddAsOverride(effect.BaseEffect.TryResolve(_cache));
            result = true;
            Console.WriteLine("Write effect");
            counter += 1;
        }

        return counter;
    }

    private (bool, int) CheckEffectsForReduceCost(IEnumerable<IEffectGetter> effects)
    {
        var result = false;
        var counter = effects.Sum(effect => CheckEffectForReduceCost(effect, ref result));

        return (result, counter);
    }

    private string PatchGetEnchantmentsAndSpellsWithReduceCosts()
    {
        var effectCounter = 0;
        var spellsCounter = 0;
        var enchesCounter = 0;
        foreach (var spell in SpellsFromLoadOrder())
        {
            var (result, counter) = CheckEffectsForReduceCost(spell.Effects);
            if (!result) continue;
            effectCounter += counter;
            spellsCounter += 1;
            _bethesdaMod.Spells.GetOrAddAsOverride(spell);
            Console.WriteLine("Write spell");
        }

        foreach (var enchantment in ObjectEffectsFromLoadOrder())
        {
            var (result, counter) = CheckEffectsForReduceCost(enchantment.Effects);
            if (!result) continue;
            effectCounter += counter;
            enchesCounter += 1;
            _bethesdaMod.ObjectEffects.GetOrAddAsOverride(enchantment);
            Console.WriteLine("Write object effect");
        }

        return $"Find {spellsCounter} spells, {enchesCounter} enchantments, {effectCounter} effects";
    }

    public static string Patch(ApiGameRelease gameRelease, ApiPatcherMod mod)
    {
        var patcher = new Patcher(gameRelease, mod);

        patcher.DeleteModFile();

        var resultString = mod switch
        {
            ApiPatcherMod.PatchGetAllArmor => patcher.PatchGetAllArmor(),
            ApiPatcherMod.PatchGetAllWeapon => patcher.PatchGetAllWeapon(),
            ApiPatcherMod.PatchGetAllSpellStaffs => patcher.PatchGetAllSpellStaffs(),
            ApiPatcherMod.PatchBzikWeapon => patcher.PatchBzikWeapon(),
            ApiPatcherMod.PatchGetEnchantmentsAndSpellsWithReduceCosts => patcher
                .PatchGetEnchantmentsAndSpellsWithReduceCosts(),
            _ => throw new ArgumentOutOfRangeException(nameof(mod), mod, null)
        };

        patcher.SaveMod();

        return resultString;
    }
}