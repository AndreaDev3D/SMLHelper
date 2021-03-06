﻿namespace SMLHelper.V2.Patchers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using Assets;
    using HarmonyLib;
    using UnityEngine;
    using UWE;
    using Logger = Logger;
#if SUBNAUTICA
    using Sprite = Atlas.Sprite;
#elif BELOWZERO
    using Sprite = UnityEngine.Sprite;
#endif

    internal class SpritePatcher
    {

        internal static void PatchCheck(SpriteManager.Group group, string name)
        {
            if (string.IsNullOrEmpty(name) || !ModSprite.ModSprites.TryGetValue(group, out var groupDict) || !groupDict.TryGetValue(name, out _))
                return;
#if BELOWZERO
            if (!SpriteManager.hasInitialized)
                return;
#endif
            Dictionary<string, Sprite> atlas = GetSpriteAtlas(group);
            
            if (atlas != null && !atlas.TryGetValue(name, out _))
                PatchSprites();
        }

        internal static void Patch(Harmony harmony)
        {
#if SUBNAUTICA
            PatchSprites();
            MethodInfo methodInfo = AccessTools.Method(typeof(SpriteManager), nameof(SpriteManager.Get), new Type[] { typeof(SpriteManager.Group), typeof(string) });
#elif BELOWZERO
            CoroutineHost.StartCoroutine(PatchSpritesAsync());
            MethodInfo methodInfo = AccessTools.Method(typeof(SpriteManager), nameof(SpriteManager.Get), new Type[] { typeof(SpriteManager.Group), typeof(string), typeof(Sprite) });
#endif
            HarmonyMethod patchCheck = new HarmonyMethod(AccessTools.Method(typeof(SpritePatcher), nameof(SpritePatcher.PatchCheck)));
            harmony.Patch(methodInfo, prefix: patchCheck);
        }

#if BELOWZERO
        private static IEnumerator PatchSpritesAsync()
        {
            while (!SpriteManager.hasInitialized)
                yield return new WaitForSecondsRealtime(1);

            PatchSprites();
            yield break;
        }
#endif

        private static void PatchSprites()
        {
            foreach (var moddedSpriteGroup in ModSprite.ModSprites)
            {
                SpriteManager.Group moddedGroup = moddedSpriteGroup.Key;

                Dictionary<string, Sprite> spriteAtlas = GetSpriteAtlas(moddedGroup);
                if (spriteAtlas == null)
                    continue;

                Dictionary<string, Sprite> moddedSprites = moddedSpriteGroup.Value;
                foreach (var sprite in moddedSprites)
                {
                    if (spriteAtlas.ContainsKey(sprite.Key))
                    {
                        Logger.Debug($"Overwriting Sprite {sprite.Key} in {nameof(SpriteManager.Group)}.{moddedSpriteGroup.Key}");
                        spriteAtlas[sprite.Key] = sprite.Value;
                    }
                    else
                    {
                        Logger.Debug($"Adding Sprite {sprite.Key} to {nameof(SpriteManager.Group)}.{moddedSpriteGroup.Key}");
                        spriteAtlas.Add(sprite.Key, sprite.Value);
                    }
                }
            }
            Logger.Debug("SpritePatcher is done.");
        }

        private static Dictionary<string, Sprite> GetSpriteAtlas(SpriteManager.Group groupKey)
        {
            if (!SpriteManager.mapping.TryGetValue(groupKey, out string atlasName))
            {
                Logger.Error($"SpritePatcher was unable to find a sprite mapping for {nameof(SpriteManager.Group)}.{groupKey}");
                return null;
            }
#if SUBNAUTICA
            var atlas = Atlas.GetAtlas(atlasName);
            if (atlas?.nameToSprite != null)
                return atlas.nameToSprite;
#elif BELOWZERO
            if (SpriteManager.atlases.TryGetValue(atlasName, out var spriteGroup))
                    return spriteGroup;
#endif
            Logger.Error($"SpritePatcher was unable to find a sprite atlas for {nameof(SpriteManager.Group)}.{groupKey}");
            return null;
        }
    }
}
