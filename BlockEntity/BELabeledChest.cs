﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent
{
    public class BlockEntityLabeledChest : BlockEntityGenericTypedContainer
    {
        string text = "";
        ChestLabelRenderer labelrenderer;
        int color;
        int tempColor;
        ItemStack tempStack;


        public override string DialogTitle {
            get
            {
                if (text == null || text.Length == 0) return Lang.Get("Chest Contents");
                else return text.Replace("\r", "").Replace("\n", " ").Substring(0, Math.Min(text.Length, 15));
            }
        }

        public BlockEntityLabeledChest()
        {
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (api is ICoreClientAPI)
            {
                labelrenderer = new ChestLabelRenderer(pos, api as ICoreClientAPI);
                labelrenderer.SetNewText(text, color);
            }
        }


        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (byPlayer?.Entity?.Controls?.Sneak == true)
            {
                ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                if (hotbarSlot?.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists == true)
                {
                    JsonObject jobj = hotbarSlot.Itemstack.ItemAttributes["pigment"]["color"];
                    int r = jobj["red"].AsInt();
                    int g = jobj["green"].AsInt();
                    int b = jobj["blue"].AsInt();

                    tempColor = ColorUtil.ToRgba(255, r, g, b);
                    tempStack = hotbarSlot.TakeOut(1);


                    if (api.World is IServerWorldAccessor)
                    {
                        byte[] data;

                        using (MemoryStream ms = new MemoryStream())
                        {
                            BinaryWriter writer = new BinaryWriter(ms);
                            writer.Write("BlockEntityTextInput");
                            writer.Write("Edit chest label text");
                            writer.Write(text);
                            data = ms.ToArray();
                        }

                        ((ICoreServerAPI)api).Network.SendBlockEntityPacket(
                            (IServerPlayer)byPlayer,
                            pos.X, pos.Y, pos.Z,
                            (int)EnumSignPacketId.OpenDialog,
                            data
                        );
                    }

                    return true;
                }
            }

            return base.OnPlayerRightClick(byPlayer, blockSel);
        }






        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid == (int)EnumSignPacketId.SaveText)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    text = reader.ReadString();
                    if (text == null) text = "";
                }

                color = tempColor;
                
                MarkDirty(true);

                // Tell server to save this chunk to disk again
                api.World.BlockAccessor.GetChunkAtBlockPos(pos.X, pos.Y, pos.Z).MarkModified();
            }

            if (packetid == (int)EnumSignPacketId.CancelEdit && tempStack != null)
            {
                player.InventoryManager.TryGiveItemstack(tempStack);
                tempStack = null;
            }

            base.OnReceivedClientPacket(player, packetid, data);
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == (int)EnumSignPacketId.OpenDialog)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);

                    string dialogClassName = reader.ReadString();
                    string dialogTitle = reader.ReadString();
                    text = reader.ReadString();
                    if (text == null) text = "";

                    IClientWorldAccessor clientWorld = (IClientWorldAccessor)api.World;

                    GuiDialogBlockEntityTextInput dlg = new GuiDialogBlockEntityTextInput(dialogTitle, pos, text, api as ICoreClientAPI, 3);
                    dlg.OnTextChanged = DidChangeTextClientSide;
                    dlg.OnCloseCancel = () =>
                    {
                        labelrenderer.SetNewText(text, color);
                        (api as ICoreClientAPI).Network.SendBlockEntityPacket(pos.X, pos.Y, pos.Z, (int)EnumSignPacketId.CancelEdit, null);
                    };
                    dlg.TryOpen();
                }
            }


            if (packetid == (int)EnumSignPacketId.NowText)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    text = reader.ReadString();
                    if (text == null) text = "";

                    if (labelrenderer != null)
                    {
                        labelrenderer.SetNewText(text, color);
                    }
                }
            }

            base.OnReceivedServerPacket(packetid, data);
        }


        private void DidChangeTextClientSide(string text)
        {
            labelrenderer?.SetNewText(text, tempColor);
        }



        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);

            color = tree.GetInt("color");
            text = tree.GetString("text");

            labelrenderer?.SetNewText(text, color);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetInt("color", color);
            tree.SetString("text", text);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (labelrenderer != null)
            {
                labelrenderer.Unregister();
                labelrenderer = null;
            }
        }

        public override void OnBlockBroken()
        {
            base.OnBlockBroken();
            labelrenderer?.Unregister();
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

            labelrenderer?.Unregister();
        }



    }
}