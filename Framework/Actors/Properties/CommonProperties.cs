﻿using System;
using System.Text.RegularExpressions;
using Trinity.Combat.Abilities;
using Trinity.Framework.Actors.ActorTypes;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.SNO;
using Logger = Trinity.Technicals.Logger;

namespace Trinity.Framework.Actors.Properties
{
    public class CommonProperties
    {
        private static readonly Regex NameNumberTrimRegex = new Regex(@"-\d+$", RegexOptions.Compiled);

        internal static void Populate(TrinityActor actor)
        {
            var rActor = actor.RActor;
            var commonData = actor.CommonData;
            var actorInfo = actor.ActorInfo;

            actor.LastSeenTime = DateTime.UtcNow;
            actor.IsExcludedId = DataDictionary.ExcludedActorIds.Contains(actor.ActorSnoId) || DataDictionary.BlackListIds.Contains(actor.ActorSnoId);
            actor.IsExcludedType = DataDictionary.ExcludedActorTypes.Contains(actor.ActorType);
            actor.InternalNameLowerCase = actor.InternalName.ToLower();
            actor.IsAllowedClientEffect = DataDictionary.AllowedClientEffects.Contains(actor.ActorSnoId);
            actor.IsObstacle = DataDictionary.NavigationObstacleIds.Contains(actor.ActorSnoId) || DataDictionary.PathFindingObstacles.ContainsKey(actor.ActorSnoId);

            actor.Name = actor.InternalName; // todo get real name for everything (currently only items have this working)

            if (actor.IsRActorBased)
            {
                actor.GizmoType = actorInfo.GizmoType;
                actor.WorldSnoId = rActor.WorldSnoId;
                actor.Radius = rActor.CollisionSphere.Radius;
                var axialRadius = actorInfo.AxialCylinder.Ax1;
                actor.AxialRadius = axialRadius;
                actor.CollisionRadius = axialRadius * 0.6f;
            }

            var type = GetObjectType(
                actor.ActorType,
                actor.ActorSnoId,
                actor.GizmoType,
                actor.InternalName
                );

            actor.Type = type;

            actor.ObjectHash = actor.InternalName + actor.AcdId + actor.RActorId;
            actor.Distance = actor.Position.Distance(TrinityPlugin.Player.Position);
            actor.RadiusDistance = Math.Max(actor.Distance - actor.AxialRadius, 0f);

            if (actor.IsAcdBased && actor.IsAcdValid)
            {
                actor.Position = commonData.Position;
                actor.AnnId = commonData.AnnId;
                actor.AcdId = commonData.AcdId;
                actor.GameBalanceId = commonData.GameBalanceId;
                actor.GameBalanceType = commonData.GameBalanceType;
                actor.FastAttributeGroupId = commonData.FastAttributeGroupId;

                var animation = commonData.Animation;
                actor.Animation = animation;
                actor.AnimationNameLowerCase = DataDictionary.GetAnimationNameLowerCase(animation);
                actor.AnimationState = commonData.AnimationState;
                actor.InventorySlot = actor.CommonData.InventorySlot;
            }

            if (actor.IsRActorBased)
            {
                actor.Position = actor.RActor.Position;
            }

            actor.IsUnit = type == TrinityObjectType.Unit || actor.ActorType == ActorType.Monster || actor.ActorType == ActorType.Player;
            actor.IsItem = type == TrinityObjectType.Item || actor.ActorType == ActorType.Item;
            actor.IsPlayer = type == TrinityObjectType.Player || actor.ActorType == ActorType.Player;
            actor.IsGizmo = actor.ActorType == ActorType.Gizmo;
            actor.IsMonster = actor.ActorType == ActorType.Monster;
            actor.IsGroundItem = actor.IsItem && actor.InventorySlot == InventorySlot.None;

            actor.RequiredRange = GetRequiredRange(actor);

            if (actor.Attributes != null)
            {
                actor.IsBountyObjective = actor.Attributes.IsBountyObjective;
                actor.IsMinimapActive = actor.Attributes.IsMinimapActive;
            }
     
            UpdateLineOfSight(actor);
        }

        public static void UpdateLineOfSight(TrinityActor actor)
        {
            if(actor.ActorType == ActorType.Item && !actor.IsGroundItem)
                return;

            if (actor.Position != Vector3.Zero || Core.Avoidance.Grid.GridBounds == 0)
            {
                var inLineOfSight = Core.Avoidance.Grid.CanRayCast(TrinityPlugin.Player.Position, actor.Position);
                actor.IsInLineOfSight = inLineOfSight;
                if (!actor.HasBeenInLoS && inLineOfSight)
                    actor.HasBeenInLoS = true;

                if (actor.IsInLineOfSight)
                {
                    var isWalkable = Core.Avoidance.Grid.CanRayWalk(TrinityPlugin.Player.Position, actor.Position);
                    actor.IsWalkable = isWalkable;
                    if (!actor.HasBeenWalkable && isWalkable)
                        actor.HasBeenWalkable = true;
                }
                else
                {
                    actor.IsWalkable = false;
                }
            }
        }

        //public static void Load(TrinityActor target, DiaObject diaObject)
        //{
        //    if (diaObject == null || !diaObject.IsValid)
        //        return;

        //    var AcdId = diaObject.AcdId;
        //    var actorType = diaObject.ActorType;
        //    var actorSno = diaObject.ActorSnoId;
        //    var internalName = NameNumberTrimRegex.Replace(diaObject.Name, "");

        //    target.ActorType = actorType;
        //    target.AcdId = AcdId;
        //    target.ActorSnoId = actorSno;
        //    target.InternalName = internalName;

        //    if (AcdId == -1 && actorType != ActorType.ClientEffect)
        //        return;

        //    target.IsExcludedId = DataDictionary.ExcludedActorIds.Contains(actorSno) || DataDictionary.BlackListIds.Contains(actorSno);
        //    if (target.IsExcludedId)
        //        return;

        //    target.IsExcludedType = DataDictionary.ExcludedActorTypes.Contains(actorType);
        //    if (target.IsExcludedType)
        //        return;

        //    var rActorGuid = diaObject.RActorId;
        //    target.RActorId = rActorGuid;
        //    target.InternalNameLowerCase = internalName.ToLower();

        //    if (actorType == ActorType.ClientEffect)
        //    {
        //        target.IsAllowedClientEffect = DataDictionary.AllowedClientEffects.Contains(actorSno);
        //        if (!target.IsAllowedClientEffect)
        //            return;
        //    }

        //    target.GizmoType = diaObject.ActorInfo.GizmoType;
        //    target.IsObstacle = DataDictionary.NavigationObstacleIds.Contains(actorSno) || DataDictionary.PathFindingObstacles.ContainsKey(actorSno);
        //    target.WorldSnoId = TrinityPlugin.Player.WorldSnoId;
        //    target.Radius = diaObject.CollisionSphere.Radius;

        //    var axialRadius = diaObject.ActorInfo.AxialCylinder.Ax1;
        //    target.AxialRadius = axialRadius;
        //    target.CollisionRadius = axialRadius*0.6f;

        //    var position = diaObject.Position;
        //    target.Position = position;

        //    var commonData = target.CommonData;
        //    if (commonData == null || !commonData.IsValid || commonData.IsDisposed)
        //        return;

        //    var type = GetObjectType(
        //        target.ActorType,
        //        target.ActorSnoId,
        //        target.GizmoType,
        //        target.InternalName
        //        );

        //    target.Type = type;

        //    target.ObjectHash = HashGenerator.GenerateObjecthash(
        //        target.ActorSnoId,
        //        target.Position,
        //        target.InternalName,
        //        target.Type
        //        );

        //    target.GameBalanceID = commonData.GameBalanceId;
        //    target.GameBalanceType = commonData.GameBalanceType;
        //    target.IsMe = rActorGuid == TrinityPlugin.Player.RActorId;
        //    target.IsUnit = type == TrinityObjectType.Unit || actorType == ActorType.Monster || actorType == ActorType.Player;
        //    target.IsItem = type == TrinityObjectType.Item || actorType == ActorType.Item;
        //    target.IsPlayer = type == TrinityObjectType.Player || actorType == ActorType.Player;
        //    target.IsGizmo = actorType == ActorType.Gizmo;
        //    target.IsMonster = actorType == ActorType.Monster;

        //    target.Distance = TrinityPlugin.Player.Position.Distance(position);
        //    if (target.Distance > 100f)
        //        return;

        //    target.LastSeenTime = DateTime.UtcNow;
        //    target.AnnId = commonData.AnnId;

        //    var fagId = commonData.FastAttribGroupId;
        //    target.FastAttributeGroupId = fagId;

        //    var attributes = new ActorAttributes(fagId);
        //    target.ActorAttributes = attributes;

        //    var animation = commonData.CurrentAnimation;
        //    target.Animation = animation;
        //    target.AnimationNameLowerCase = DataDictionary.GetAnimationNameLowerCase(animation);
        //    target.AnimationState = commonData.AnimationState;

        //    if (target.ActorAttributes != null)
        //    {
        //        target.IsBountyObjective = attributes.IsBountyObjective;
        //        target.IsMinimapActive = attributes.IsMinimapActive;
        //    }

        //    var inLineOfSight = Core.Avoidance.Grid.CanRayCast(TrinityPlugin.Player.Position, position);
        //    target.IsInLineOfSight = inLineOfSight;
        //    if (!target.HasBeenInLoS && inLineOfSight)
        //        target.HasBeenInLoS = true;

        //    if (target.IsInLineOfSight)
        //    {
        //        var isWalkable = Core.Avoidance.Grid.CanRayWalk(TrinityPlugin.Player.Position, position);
        //        target.IsWalkable = isWalkable;
        //        if (!target.HasBeenWalkable && isWalkable)
        //            target.HasBeenWalkable = true;
        //    }
        //    else
        //    {
        //        target.IsWalkable = false;
        //    }
        //}

        public static TrinityObjectType GetObjectType(ActorType actorType, int actorSno, GizmoType gizmoType, string internalName)
        {
            if (DataDictionary.ObjectTypeOverrides.ContainsKey(actorSno))
                return DataDictionary.ObjectTypeOverrides[actorSno];

            if (DataDictionary.CursedChestSNO.Contains(actorSno))
                return TrinityObjectType.CursedChest;

            if (DataDictionary.CursedShrineSNO.Contains(actorSno))
                return TrinityObjectType.CursedShrine;

            if (DataDictionary.ShrineSNO.Contains(actorSno))
                return TrinityObjectType.Shrine;

            if (DataDictionary.HealthGlobeSNO.Contains(actorSno))
                return TrinityObjectType.HealthGlobe;

            if (DataDictionary.PowerGlobeSNO.Contains(actorSno))
                return TrinityObjectType.PowerGlobe;

            if (DataDictionary.ProgressionGlobeSNO.Contains(actorSno))
                return TrinityObjectType.ProgressionGlobe;

            if (DataDictionary.GoldSNO.Contains(actorSno))
                return TrinityObjectType.Gold;

            if (DataDictionary.BloodShardSNO.Contains(actorSno))
                return TrinityObjectType.BloodShard;

            if (actorType == ActorType.Item || DataDictionary.ForceToItemOverrideIds.Contains(actorSno))
                return TrinityObjectType.Item;

            if (DataDictionary.AvoidanceSNO.Contains(actorSno))
                return TrinityObjectType.Avoidance;

            if (DataDictionary.ForceTypeAsBarricade.Contains(actorSno))
                return TrinityObjectType.Barricade;

            if (actorType == ActorType.Monster)
                return TrinityObjectType.Unit;

            if (actorType == ActorType.Gizmo)
            {
                switch (gizmoType)
                {
                    case GizmoType.HealingWell:
                        return TrinityObjectType.HealthWell;

                    case GizmoType.Door:
                        return TrinityObjectType.Door;

                    case GizmoType.BreakableDoor:
                        return TrinityObjectType.Barricade;

                    case GizmoType.PoolOfReflection:
                    case GizmoType.PowerUp:
                        return TrinityObjectType.Shrine;

                    case GizmoType.Chest:
                        return TrinityObjectType.Container;

                    case GizmoType.DestroyableObject:
                    case GizmoType.BreakableChest:
                        return TrinityObjectType.Destructible;

                    case GizmoType.PlacedLoot:
                    case GizmoType.Switch:
                    case GizmoType.Headstone:
                        return TrinityObjectType.Interactable;

                    case GizmoType.Portal:
                        return TrinityObjectType.Portal;
                }
            }

            if (actorType == ActorType.Environment || actorType == ActorType.Critter || actorType == ActorType.ServerProp)
                return TrinityObjectType.Environment;

            if (actorType == ActorType.Projectile)
                return TrinityObjectType.Projectile;

            if (DataDictionary.BuffedLocationSno.Contains(actorSno))
                return TrinityObjectType.BuffedRegion;

            if (actorType == ActorType.ClientEffect)
                return TrinityObjectType.ClientEffect;

            if (actorType == ActorType.Player)
                return TrinityObjectType.Player;

            if (DataDictionary.PlayerBannerSNO.Contains(actorSno))
                return TrinityObjectType.Banner;

            if (internalName != null && internalName.StartsWith("Waypoint-"))
                return TrinityObjectType.Waypoint;

            return TrinityObjectType.Unknown;
        }

        public static TrinityObjectType GetObjectType(TrinityActor obj)
        {
            return GetObjectType(
                obj.ActorType,
                obj.ActorSnoId,
                obj.GizmoType,
                obj.InternalName
                );
        }

        public static float GetRequiredRange(TrinityActor actor)
        {
            var result = 2f;

            switch (actor.Type)
            {
                // * Unit, we need to pick an ability to use and get within range
                case TrinityObjectType.Unit:
                    {
                        if (actor.IsHidden)
                        {
                            result = actor.CollisionRadius;
                        }
                        else
                        {
                            result = CombatBase.CurrentPower.MinimumRange;
                        }
                        break;
                    }
                // * Item - need to get within 6 feet and then interact with it
                case TrinityObjectType.Item:
                    {
                        result = 2f;
                        break;
                    }
                // * Gold - need to get within pickup radius only
                case TrinityObjectType.Gold:
                    {
                        result = 2f;
                        break;
                    }
                // * Globes - need to get within pickup radius only
                case TrinityObjectType.PowerGlobe:
                case TrinityObjectType.HealthGlobe:
                case TrinityObjectType.ProgressionGlobe:
                    {
                        result = 2f;
                        break;
                    }
                // * Shrine & Container - need to get within 8 feet and interact
                case TrinityObjectType.HealthWell:
                    {
                        result = 4f;

                        float range;
                        if (DataDictionary.CustomObjectRadius.TryGetValue(actor.ActorSnoId, out range))
                        {
                            result = range;
                        }
                        break;
                    }
                case TrinityObjectType.Shrine:
                case TrinityObjectType.Container:
                    {
                        result = 6f;

                        float range;
                        if (DataDictionary.CustomObjectRadius.TryGetValue(actor.ActorSnoId, out range))
                        {
                            result = range;
                        }
                        break;
                    }
                case TrinityObjectType.Interactable:
                {
                        result = 5f;
                        float range;
                        if (DataDictionary.CustomObjectRadius.TryGetValue(actor.ActorSnoId, out range))
                        {
                            result = range;
                        }
                        if (result <= 0)
                            result = actor.Radius;
                        break;
                    }
                // * Destructible - need to pick an ability and attack it
                case TrinityObjectType.Destructible:
                    {
                        result = CombatBase.CurrentPower.MinimumRange;
                        actor.Radius = 1f;
                        break;
                    }
                case TrinityObjectType.Barricade:
                    {
                        result = CombatBase.CurrentPower.MinimumRange;
                        actor.Radius = 1f;
                        break;
                    }
                // * Avoidance - need to pick an avoid location and move there
                case TrinityObjectType.Avoidance:
                    {
                        result = 2f;
                        break;
                    }
                case TrinityObjectType.Door:
                    result = 2f;
                    break;
                default:
                    result = actor.Radius;
                    break;
            }
            return result;
        }

    }


}