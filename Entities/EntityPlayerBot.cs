﻿using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent
{
    public class EntityPlayerBot : EntityAnimalBot
    {

        protected InventoryBase inv;
       

        public override bool StoreWithChunk
        {
            get { return true; }
        }
        

        public override IInventory GearInventory
        {
            get
            {
                return inv;
            }
        }

        public override ItemSlot RightHandItemSlot => inv[15];
        public override ItemSlot LeftHandItemSlot => inv[16];

        public EntityPlayerBot() : base()
        {
            inv = new InventoryGear(null, null);
        }

        public override void Initialize(EntityProperties properties, ICoreAPI api, long chunkindex3d)
        {
            base.Initialize(properties, api, chunkindex3d);

            inv.LateInitialize("gearinv-" + EntityId, api);

            Name = WatchedAttributes.GetTreeAttribute("nametag")?.GetString("name");
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();

            if (World.Side == EnumAppSide.Client)
            {
                (Properties.Client.Renderer as EntityShapeRenderer).DoRenderHeldItem = true;
            }
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);

            var curCommand = WatchedAttributes.GetString("currentCommand", "");
            if (curCommand != "")
            {
                AnimManager.StopAnimation("idle");
                AnimManager.StopAnimation("idle1");
            }


            HandleHandAnimations(dt);
        }




        protected string lastRunningHeldUseAnimation;
        protected string lastRunningRightHeldIdleAnimation;
        protected string lastRunningLeftHeldIdleAnimation;
        protected string lastRunningHeldHitAnimation;

        protected override void HandleHandAnimations(float dt)
        {
            ItemStack rightstack = RightHandItemSlot?.Itemstack;

            EnumHandInteract interact = servercontrols.HandUse;

            bool nowUseStack = (interact == EnumHandInteract.BlockInteract || interact == EnumHandInteract.HeldItemInteract) || (servercontrols.RightMouseDown && !servercontrols.LeftMouseDown);
            bool wasUseStack = lastRunningHeldUseAnimation != null && AnimManager.ActiveAnimationsByAnimCode.ContainsKey(lastRunningHeldUseAnimation);

            bool nowHitStack = interact == EnumHandInteract.HeldItemAttack || (servercontrols.LeftMouseDown);
            bool wasHitStack = lastRunningHeldHitAnimation != null && AnimManager.ActiveAnimationsByAnimCode.ContainsKey(lastRunningHeldHitAnimation);


            string nowHeldRightUseAnim = rightstack?.Collectible.GetHeldTpUseAnimation(RightHandItemSlot, this);
            string nowHeldRightHitAnim = rightstack?.Collectible.GetHeldTpHitAnimation(RightHandItemSlot, this);
            string nowHeldRightIdleAnim = rightstack?.Collectible.GetHeldTpIdleAnimation(RightHandItemSlot, this, EnumHand.Right);
            string nowHeldLeftIdleAnim = LeftHandItemSlot?.Itemstack?.Collectible.GetHeldTpIdleAnimation(LeftHandItemSlot, this, EnumHand.Left);

            bool nowRightIdleStack = nowHeldRightIdleAnim != null && !nowUseStack && !nowHitStack;
            bool wasRightIdleStack = lastRunningRightHeldIdleAnimation != null && AnimManager.ActiveAnimationsByAnimCode.ContainsKey(lastRunningRightHeldIdleAnimation);

            bool nowLeftIdleStack = nowHeldLeftIdleAnim != null;
            bool wasLeftIdleStack = lastRunningLeftHeldIdleAnimation != null && AnimManager.ActiveAnimationsByAnimCode.ContainsKey(lastRunningLeftHeldIdleAnimation);

            if (rightstack == null)
            {
                nowHeldRightHitAnim = "breakhand";
                nowHeldRightUseAnim = "interactstatic";
            }

            if (nowUseStack != wasUseStack || (lastRunningHeldUseAnimation != null && nowHeldRightUseAnim != lastRunningHeldUseAnimation))
            {
                AnimManager.StopAnimation(lastRunningHeldUseAnimation);
                lastRunningHeldUseAnimation = null;

                if (nowUseStack)
                {
                    AnimManager.StopAnimation(lastRunningRightHeldIdleAnimation);
                    AnimManager.StartAnimation(lastRunningHeldUseAnimation = nowHeldRightUseAnim);
                }
            }

            if (nowHitStack != wasHitStack || (lastRunningHeldHitAnimation != null && nowHeldRightHitAnim != lastRunningHeldHitAnimation))
            {
                AnimManager.StopAnimation(lastRunningHeldHitAnimation);
                lastRunningHeldHitAnimation = null;


                if (nowHitStack)
                {
                    AnimManager.StopAnimation(lastRunningLeftHeldIdleAnimation);
                    AnimManager.StopAnimation(lastRunningRightHeldIdleAnimation);
                    AnimManager.StartAnimation(lastRunningHeldHitAnimation = nowHeldRightHitAnim);
                }
            }

            if (nowRightIdleStack != wasRightIdleStack || (lastRunningRightHeldIdleAnimation != null && nowHeldRightIdleAnim != lastRunningRightHeldIdleAnimation))
            {
                AnimManager.StopAnimation(lastRunningRightHeldIdleAnimation);
                lastRunningRightHeldIdleAnimation = null;

                if (nowRightIdleStack)
                {
                    AnimManager.StartAnimation(lastRunningRightHeldIdleAnimation = nowHeldRightIdleAnim);
                }
            }

            if (nowLeftIdleStack != wasLeftIdleStack || (lastRunningLeftHeldIdleAnimation != null && nowHeldLeftIdleAnim != lastRunningLeftHeldIdleAnimation))
            {
                AnimManager.StopAnimation(lastRunningLeftHeldIdleAnimation);

                lastRunningLeftHeldIdleAnimation = null;

                if (nowLeftIdleStack)
                {
                    AnimManager.StartAnimation(lastRunningLeftHeldIdleAnimation = nowHeldLeftIdleAnim);
                }
            }
        }



        public override void ToBytes(BinaryWriter writer, bool forClient)
        {
            TreeAttribute tree;
            WatchedAttributes["gearInv"] = tree = new TreeAttribute();
            inv.ToTreeAttributes(tree);
            

            base.ToBytes(writer, forClient);
        }


        public override void FromBytes(BinaryReader reader, bool forClient)
        {
            base.FromBytes(reader, forClient);

            TreeAttribute tree = WatchedAttributes["gearInv"] as TreeAttribute;
            if (tree != null) inv.FromTreeAttributes(tree);
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
        {
            base.OnInteract(byEntity, slot, hitPosition, mode);

            if ((byEntity as EntityPlayer)?.Controls.Sneak == true && mode == EnumInteractMode.Interact && byEntity.World.Side == EnumAppSide.Server)
            {
                if (!LeftHandItemSlot.Empty || !RightHandItemSlot.Empty)
                {
                    LeftHandItemSlot.Itemstack = null;
                    RightHandItemSlot.Itemstack = null;
                }
                else
                {
                    inv.DiscardAll();
                }

                WatchedAttributes.MarkAllDirty();
            }
        }

    }




    public class InventoryGear : InventoryBase
    {
        ItemSlot[] slots;

        public InventoryGear(string className, string id, ICoreAPI api) : base(className, id, api)
        {
            slots = GenEmptySlots(19);
            baseWeight = 2.5f;
        }

        public InventoryGear(string inventoryId, ICoreAPI api) : base(inventoryId, api)
        {
            slots = GenEmptySlots(19);
            baseWeight = 2.5f;
        }

        public override void OnItemSlotModified(ItemSlot slot)
        {
            base.OnItemSlotModified(slot);
        }


        public override int Count
        {
            get { return slots.Length; }
        }

        public override ItemSlot this[int slotId] { get { return slots[slotId]; }  set { slots[slotId] = value; } }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            List<ItemSlot> modifiedSlots = new List<ItemSlot>();
            slots = SlotsFromTreeAttributes(tree, slots, modifiedSlots);
            for (int i = 0; i < modifiedSlots.Count; i++) DidModifyItemSlot(modifiedSlots[i]);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
            //ResolveBlocksOrItems();
        }

        Dictionary<EnumCharacterDressType, string> iconByDressType = new Dictionary<EnumCharacterDressType, string>()
        {
            { EnumCharacterDressType.Foot, "boots" },
            { EnumCharacterDressType.Hand, "gloves" },
            { EnumCharacterDressType.Shoulder, "cape" },
            { EnumCharacterDressType.Head, "hat" },
            { EnumCharacterDressType.LowerBody, "trousers" },
            { EnumCharacterDressType.UpperBody, "shirt" },
            { EnumCharacterDressType.UpperBodyOver, "pullover" },
            { EnumCharacterDressType.Neck, "necklace" },
            { EnumCharacterDressType.Arm, "bracers" },
            { EnumCharacterDressType.Waist, "belt" },
            { EnumCharacterDressType.Emblem, "medal" },
            { EnumCharacterDressType.Face, "face" },
        };


        protected override ItemSlot NewSlot(int slotId)
        {
            if (slotId == 15 || slotId == 16) return new ItemSlotSurvival(this);
            if (slotId > 16)
            {
                return new ItemSlotBackpack(this);
            }

            EnumCharacterDressType type = (EnumCharacterDressType)slotId;
            ItemSlotCharacter slot = new ItemSlotCharacter(type, this);
            iconByDressType.TryGetValue(type, out slot.BackgroundIcon);

            return slot;
        }

        public override void DiscardAll()
        {
            base.DiscardAll();
            for (int i = 0; i < Count; i++)
            {
                DidModifyItemSlot(this[i]);
            }
        }


        public override void OnOwningEntityDeath(Vec3d pos)
        {
            // Don't drop contents on death
        }
    }
}
