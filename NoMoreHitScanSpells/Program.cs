using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;

namespace NoMoreHitScanSpells
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "NoHitScanSpells.esp")
                .Run(args);
        }

        // FLST - Formlist
        // RACE
        // NPC
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var skyrimModKey = new ModKey("Skyrim", type: ModType.Master);
            var spellFormIDs = new FormKey[]
            {
                // Flames
                new(skyrimModKey, 0x012FCD),
                // Flames Left
                new(skyrimModKey, 0x07D996),
                // Flames Right
                new(skyrimModKey, 0x0C969A),
                
                // Frostbite
                new(skyrimModKey, 0x02B96B),
                // Frostbite Left
                new(skyrimModKey, 0x034C58),
                // Frostbite Right
                new(skyrimModKey, 0x0C969D),
                
                // Sparks
                new(skyrimModKey, 0x02DD2A),
                // Sparks Left
                new(skyrimModKey, 0x040001),
                // Sparks Right
                new(skyrimModKey, 0x0C96A1),
                
                // Ice Storm
                new(skyrimModKey, 0x045F9C),
                // Ice Storm Left
                new(skyrimModKey, 0x0A1992),
                // Ice Storm Right
                new(skyrimModKey, 0x0BB96A),
                
                // Drain Life 0
                new(skyrimModKey, 0x107AA1),
                // Drain Life 1
                new(skyrimModKey, 0x0F5B58),
                // Drain Life 2
                new(skyrimModKey, 0x0F5B59),
                // Drain Life 3
                new(skyrimModKey, 0x0F5B5A),
                // Drain Life 4
                new(skyrimModKey, 0x0F5B5B),
                // Drain Life 5
                new(skyrimModKey, 0x08D5C3),
                // Drain Life 6
                new(skyrimModKey, 0x08D5C7)
            };
            
            var drainSpellIDs = new FormKey[]
            {
                // Drain Life 0
                new(skyrimModKey, 0x107AA1),
                // Drain Life 1
                new(skyrimModKey, 0x0F5B58),
                // Drain Life 2
                new(skyrimModKey, 0x0F5B59),
                // Drain Life 3
                new(skyrimModKey, 0x0F5B5A),
                // Drain Life 4
                new(skyrimModKey, 0x0F5B5B),
                // Drain Life 5
                new(skyrimModKey, 0x08D5C3),
                // Drain Life 6
                new(skyrimModKey, 0x08D5C7)
            };
            
            var resolvedSpells = spellFormIDs.Select(x =>
            {
                var recordLink = new FormLink<ISpellGetter>(x);
                return recordLink.Resolve(state.LinkCache);
            }).ToArray();

            var resolvedDrainSpells = drainSpellIDs.Select(x =>
            {
                var recordLink = new FormLink<ISpellGetter>(x);
                return recordLink.Resolve(state.LinkCache);
            }).ToArray();

            // Determine Replacement Spell
            var replacementSpellBase = new FormKey(skyrimModKey, 0x02B96C);
            var recordLink = new FormLink<ISpellGetter>(replacementSpellBase);
            var replacementSpell = recordLink.Resolve(state.LinkCache);

            // Determine Player
            var playerBase = new FormKey(skyrimModKey, 0x000007);
            var playerRecordLink = new FormLink<INpcGetter>(playerBase);
            var player = playerRecordLink.Resolve(state.LinkCache);

            foreach (var npcContext in state.LoadOrder.PriorityOrder.Npc().WinningContextOverrides())
            {
                var actorEffects = npcContext.Record.ActorEffect;
                if (actorEffects is null) continue;
                var effectsToRemove = actorEffects.Where(x =>
                {
                    return resolvedSpells.Any(y => x.FormKey.Equals(y.FormKey));
                }).ToArray();
                if (effectsToRemove.Length == 0) continue;

                var npc = npcContext.GetOrAddAsOverride(state.PatchMod);
                // Filter Player
                if (npc.FormKey.Equals(player.FormKey)) continue;

                var hasLifeDrain = false;
                npc.ActorEffect?.Remove(effectsToRemove.Select(x =>
                {
                    if (resolvedDrainSpells.Contains(x.TryResolve(state.LinkCache)))
                    {
                        hasLifeDrain = true;
                    }
                    return x.FormKey;
                }));
                
                if (!hasLifeDrain) continue;
                if (npc.ActorEffect is not null && !npc.ActorEffect.Contains(replacementSpell))
                {
                    npc.ActorEffect.Add(replacementSpell);
                }
            }
        }
    }
}